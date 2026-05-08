using WindowSnapper.Layouts;

namespace WindowSnapper.App.ViewModels;

internal sealed class EditableZoneViewModel : ViewModelBase
{
    private string id;
    private string name;
    private double x;
    private double y;
    private double width;
    private double height;
    private double canvasWidth;
    private double canvasHeight;
    private bool isSelected;

    public EditableZoneViewModel(string id, string name, double x, double y, double width, double height)
    {
        this.id = id;
        this.name = name;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public double X
    {
        get => x;
        private set
        {
            if (SetProperty(ref x, value))
            {
                OnPropertyChanged(nameof(PixelX));
            }
        }
    }

    public double Y
    {
        get => y;
        private set
        {
            if (SetProperty(ref y, value))
            {
                OnPropertyChanged(nameof(PixelY));
            }
        }
    }

    public double Width
    {
        get => width;
        private set
        {
            if (SetProperty(ref width, value))
            {
                OnPropertyChanged(nameof(PixelWidth));
            }
        }
    }

    public double Height
    {
        get => height;
        private set
        {
            if (SetProperty(ref height, value))
            {
                OnPropertyChanged(nameof(PixelHeight));
            }
        }
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public double PixelX => canvasWidth * X;

    public double PixelY => canvasHeight * Y;

    public double PixelWidth => canvasWidth * Width;

    public double PixelHeight => canvasHeight * Height;

    public void SetCanvasSize(double width, double height)
    {
        canvasWidth = Math.Max(0, width);
        canvasHeight = Math.Max(0, height);
        OnPropertyChanged(nameof(PixelX));
        OnPropertyChanged(nameof(PixelY));
        OnPropertyChanged(nameof(PixelWidth));
        OnPropertyChanged(nameof(PixelHeight));
    }

    public void SetRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public ZoneDefinition ToZoneDefinition()
    {
        return new ZoneDefinition(
            Id.Trim(),
            Name.Trim(),
            Round(X),
            Round(Y),
            Round(Width),
            Round(Height));
    }

    private static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
