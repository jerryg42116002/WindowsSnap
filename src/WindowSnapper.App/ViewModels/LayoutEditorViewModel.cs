using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowSnapper.App.Commands;
using WindowSnapper.Core.Results;
using WindowSnapper.Layouts;

namespace WindowSnapper.App.ViewModels;

internal sealed class LayoutEditorViewModel : ViewModelBase
{
    private const int LayoutVersion = 1;
    private const double DefaultZoneWidth = 1.0 / 3.0;
    private const double DefaultZoneHeight = 0.5;
    private const double MinZoneSize = 0.02;

    private readonly Func<LayoutDefinition, Task<Result>> saveLayoutAsync;
    private readonly LayoutValidator validator = new();
    private string layoutId;
    private string layoutName;
    private int gap = 8;
    private int margin = 8;
    private EditableZoneViewModel? selectedZone;
    private string errorMessage = string.Empty;
    private string statusMessage = "创建或拖拽区域后保存为自定义布局。";
    private double canvasWidth;
    private double canvasHeight;

    public LayoutEditorViewModel(Func<LayoutDefinition, Task<Result>> saveLayoutAsync)
    {
        this.saveLayoutAsync = saveLayoutAsync ?? throw new ArgumentNullException(nameof(saveLayoutAsync));
        layoutId = CreateDefaultLayoutId();
        layoutName = "自定义布局";

        AddZoneCommand = new RelayCommand(AddZone);
        DeleteSelectedZoneCommand = new RelayCommand(DeleteSelectedZone, () => SelectedZone is not null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        AddZone();
    }

    public event EventHandler? CloseRequested;

    public ObservableCollection<EditableZoneViewModel> Zones { get; } = [];

    public string LayoutId
    {
        get => layoutId;
        set => SetProperty(ref layoutId, value);
    }

    public string LayoutName
    {
        get => layoutName;
        set => SetProperty(ref layoutName, value);
    }

    public int Gap
    {
        get => gap;
        set => SetProperty(ref gap, Math.Max(0, value));
    }

    public int Margin
    {
        get => margin;
        set => SetProperty(ref margin, Math.Max(0, value));
    }

    public EditableZoneViewModel? SelectedZone
    {
        get => selectedZone;
        private set
        {
            if (selectedZone is not null)
            {
                selectedZone.IsSelected = false;
            }

            if (SetProperty(ref selectedZone, value))
            {
                if (selectedZone is not null)
                {
                    selectedZone.IsSelected = true;
                }

                OnPropertyChanged(nameof(HasSelectedZone));
                RaiseCommandStates();
            }
        }
    }

    public bool HasSelectedZone => SelectedZone is not null;

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public ICommand AddZoneCommand { get; }

    public ICommand DeleteSelectedZoneCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public void SetCanvasSize(double width, double height)
    {
        canvasWidth = Math.Max(0, width);
        canvasHeight = Math.Max(0, height);
        foreach (var zone in Zones)
        {
            zone.SetCanvasSize(canvasWidth, canvasHeight);
        }
    }

    public void SelectZone(EditableZoneViewModel zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (Zones.Contains(zone))
        {
            SelectedZone = zone;
        }
    }

    public void MoveZoneByPixels(EditableZoneViewModel zone, double deltaX, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (!Zones.Contains(zone) || canvasWidth <= 0 || canvasHeight <= 0)
        {
            return;
        }

        SelectZone(zone);
        var nextX = Clamp(zone.X + deltaX / canvasWidth, 0, 1 - zone.Width);
        var nextY = Clamp(zone.Y + deltaY / canvasHeight, 0, 1 - zone.Height);
        zone.SetRect(Round(nextX), Round(nextY), zone.Width, zone.Height);
    }

    public void ResizeZoneByPixels(EditableZoneViewModel zone, double deltaWidth, double deltaHeight)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (!Zones.Contains(zone) || canvasWidth <= 0 || canvasHeight <= 0)
        {
            return;
        }

        SelectZone(zone);
        var maxWidth = Math.Max(MinZoneSize, 1 - zone.X);
        var maxHeight = Math.Max(MinZoneSize, 1 - zone.Y);
        var nextWidth = Clamp(zone.Width + deltaWidth / canvasWidth, MinZoneSize, maxWidth);
        var nextHeight = Clamp(zone.Height + deltaHeight / canvasHeight, MinZoneSize, maxHeight);
        zone.SetRect(zone.X, zone.Y, Round(nextWidth), Round(nextHeight));
    }

    public LayoutDefinition ToLayoutDefinition()
    {
        return new LayoutDefinition(
            LayoutId.Trim(),
            LayoutName.Trim(),
            LayoutVersion,
            Gap,
            Margin,
            Zones.Select(zone => zone.ToZoneDefinition()).ToArray());
    }

    public async Task<Result> SaveAsync()
    {
        ErrorMessage = string.Empty;
        var layout = ToLayoutDefinition();
        var validation = ValidateForSave(layout);
        if (validation.IsFailure)
        {
            ErrorMessage = validation.ErrorMessage;
            return validation;
        }

        var save = await saveLayoutAsync(layout);
        if (save.IsFailure)
        {
            ErrorMessage = save.ErrorMessage;
            return save;
        }

        StatusMessage = $"布局 '{layout.Name}' 已保存。";
        CloseRequested?.Invoke(this, EventArgs.Empty);
        return Result.Success();
    }

    private void AddZone()
    {
        var nextNumber = Zones.Count + 1;
        var column = (nextNumber - 1) % 3;
        var row = ((nextNumber - 1) / 3) % 2;
        var zone = new EditableZoneViewModel(
            $"zone-{nextNumber}",
            $"区域 {nextNumber}",
            column * DefaultZoneWidth,
            row * DefaultZoneHeight,
            DefaultZoneWidth,
            DefaultZoneHeight);
        zone.SetCanvasSize(canvasWidth, canvasHeight);
        Zones.Add(zone);
        SelectZone(zone);
        StatusMessage = $"已添加 {zone.Name}。";
    }

    private void DeleteSelectedZone()
    {
        if (SelectedZone is null)
        {
            return;
        }

        var removedIndex = Zones.IndexOf(SelectedZone);
        Zones.Remove(SelectedZone);
        SelectedZone = Zones.Count == 0
            ? null
            : Zones[Math.Clamp(removedIndex, 0, Zones.Count - 1)];
    }

    private Result ValidateForSave(LayoutDefinition layout)
    {
        if (!IsSafeLayoutId(layout.Id))
        {
            return Result.Failure(
                ResultErrorCode.InvalidArgument,
                "布局 id 只能包含英文字母、数字、短横线和下划线。");
        }

        if (BuiltinLayouts.FindById(layout.Id) is not null)
        {
            return Result.Failure(
                ResultErrorCode.InvalidArgument,
                $"布局 id '{layout.Id}' 是内置布局，不能覆盖。");
        }

        var validation = validator.Validate(layout);
        return validation.IsFailure
            ? Result.Failure(validation.ErrorCode, validation.ErrorMessage)
            : Result.Success();
    }

    private static bool IsSafeLayoutId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
            id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private void RaiseCommandStates()
    {
        if (DeleteSelectedZoneCommand is RelayCommand deleteCommand)
        {
            deleteCommand.RaiseCanExecuteChanged();
        }
    }

    private static string CreateDefaultLayoutId()
    {
        return $"custom-layout-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
