using WindowSnapper.Core.Results;

namespace WindowSnapper.Hotkeys;

/// <summary>
/// Coordinates hotkey registration, unregistration, conflict detection, and dispatch.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly IHotkeyRegistrar registrar;
    private readonly Dictionary<HotkeyChord, HotkeyDefinition> registeredHotkeys = [];
    private bool disposed;

    /// <summary>
    /// Raised when a registered hotkey is pressed.
    /// </summary>
    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HotkeyManager"/> class.
    /// </summary>
    public HotkeyManager(IHotkeyRegistrar registrar)
    {
        this.registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        this.registrar.HotkeyPressed += OnRegistrarHotkeyPressed;
    }

    /// <summary>
    /// Gets the currently registered hotkeys.
    /// </summary>
    public IReadOnlyList<HotkeyDefinition> RegisteredHotkeys => registeredHotkeys.Values.ToArray();

    /// <summary>
    /// Registers the default MVP hotkeys.
    /// </summary>
    public Result RegisterDefaultHotkeys()
    {
        return RegisterMany(DefaultHotkeys.All);
    }

    /// <summary>
    /// Registers a collection of hotkeys after checking for duplicate chords.
    /// </summary>
    public Result RegisterMany(IEnumerable<HotkeyDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var definitionsList = definitions.ToArray();
        var conflict = FindFirstConflict(definitionsList);
        if (conflict is not null)
        {
            return Result.Failure(
                ResultErrorCode.InvalidArgument,
                $"Hotkey conflict detected for '{conflict.ChordText}'.");
        }

        foreach (var definition in definitionsList)
        {
            var result = Register(definition);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Registers a single hotkey.
    /// </summary>
    public Result Register(HotkeyDefinition definition)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Command == HotkeyCommand.None)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey command is required.");
        }

        var chord = HotkeyChord.FromDefinition(definition);
        if (registeredHotkeys.ContainsKey(chord))
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, $"Hotkey conflict detected for '{definition.ChordText}'.");
        }

        var result = registrar.Register(definition);
        if (result.IsFailure)
        {
            return Result.Failure(result.ErrorCode, result.ErrorMessage);
        }

        registeredHotkeys.Add(chord, definition);
        return Result.Success();
    }

    /// <summary>
    /// Unregisters every registered hotkey.
    /// </summary>
    public Result UnregisterAll()
    {
        ThrowIfDisposed();

        var result = registrar.UnregisterAll();
        registeredHotkeys.Clear();

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        registrar.HotkeyPressed -= OnRegistrarHotkeyPressed;
        _ = registrar.UnregisterAll();
        registeredHotkeys.Clear();
        disposed = true;
    }

    private static HotkeyDefinition? FindFirstConflict(IReadOnlyList<HotkeyDefinition> definitions)
    {
        var seen = new HashSet<HotkeyChord>();
        foreach (var definition in definitions)
        {
            var chord = HotkeyChord.FromDefinition(definition);
            if (!seen.Add(chord))
            {
                return definition;
            }
        }

        return null;
    }

    private void OnRegistrarHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        if (registeredHotkeys.ContainsKey(HotkeyChord.FromDefinition(e.Definition)))
        {
            HotkeyPressed?.Invoke(this, e);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
