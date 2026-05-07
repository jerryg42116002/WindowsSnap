using System.Windows;
using System.Windows.Interop;
using WindowSnapper.Core.Results;
using WindowSnapper.Hotkeys;
using WindowSnapper.Win32;

namespace WindowSnapper.App.Hotkeys;

internal sealed class WpfHotkeyRegistrar : IHotkeyRegistrar, IDisposable
{
    private readonly Window window;
    private readonly Win32HotkeyRegistrar nativeRegistrar;
    private readonly Dictionary<string, Registration> registrationsByChord = [];
    private readonly Dictionary<int, HotkeyDefinition> registrationsById = [];
    private HwndSource? source;
    private IntPtr windowHandle;
    private int nextId = 1;
    private bool disposed;

    public WpfHotkeyRegistrar(Window window, Win32HotkeyRegistrar nativeRegistrar)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        this.nativeRegistrar = nativeRegistrar ?? throw new ArgumentNullException(nameof(nativeRegistrar));
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public Result Register(HotkeyDefinition definition)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Command == HotkeyCommand.None)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey command is required.");
        }

        if (registrationsByChord.ContainsKey(definition.ChordText))
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, $"Hotkey conflict detected for '{definition.ChordText}'.");
        }

        var hook = EnsureMessageHook();
        if (hook.IsFailure)
        {
            return hook;
        }

        var nativeModifiers = MapModifiers(definition.Modifiers);
        if (nativeModifiers.IsFailure)
        {
            return Result.Failure(nativeModifiers.ErrorCode, nativeModifiers.ErrorMessage);
        }

        var virtualKey = MapVirtualKey(definition.Key);
        if (virtualKey.IsFailure)
        {
            return Result.Failure(virtualKey.ErrorCode, virtualKey.ErrorMessage);
        }

        var id = nextId++;
        var register = nativeRegistrar.RegisterHotkey(windowHandle, id, nativeModifiers.Value, virtualKey.Value);
        if (register.IsFailure)
        {
            return register;
        }

        registrationsByChord.Add(definition.ChordText, new Registration(id, definition));
        registrationsById.Add(id, definition);
        return Result.Success();
    }

    public Result Unregister(HotkeyDefinition definition)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(definition);

        if (!registrationsByChord.TryGetValue(definition.ChordText, out var registration))
        {
            return Result.Success();
        }

        var unregister = nativeRegistrar.UnregisterHotkey(windowHandle, registration.Id);
        if (unregister.IsFailure)
        {
            return unregister;
        }

        registrationsByChord.Remove(definition.ChordText);
        registrationsById.Remove(registration.Id);
        return Result.Success();
    }

    public Result UnregisterAll()
    {
        ThrowIfDisposed();

        Result? firstFailure = null;
        foreach (var registration in registrationsByChord.Values.ToArray())
        {
            var result = nativeRegistrar.UnregisterHotkey(windowHandle, registration.Id);
            if (result.IsFailure && firstFailure is null)
            {
                firstFailure = result;
            }
        }

        registrationsByChord.Clear();
        registrationsById.Clear();

        return firstFailure ?? Result.Success();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        _ = UnregisterAll();
        source?.RemoveHook(WndProc);
        source = null;
        disposed = true;
    }

    private Result EnsureMessageHook()
    {
        if (source is not null)
        {
            return Result.Success();
        }

        windowHandle = new WindowInteropHelper(window).EnsureHandle();
        if (windowHandle == IntPtr.Zero)
        {
            return Result.Failure(ResultErrorCode.NotFound, "WPF window handle is not available.");
        }

        source = HwndSource.FromHwnd(windowHandle);
        if (source is null)
        {
            return Result.Failure(ResultErrorCode.PlatformCallFailed, "WPF message source is not available.");
        }

        source.AddHook(WndProc);
        return Result.Success();
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == Win32HotkeyRegistrar.HotkeyMessageId &&
            registrationsById.TryGetValue(wParam.ToInt32(), out var definition))
        {
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(definition));
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static Result<uint> MapModifiers(HotkeyModifiers modifiers)
    {
        if (modifiers == HotkeyModifiers.None)
        {
            return Result<uint>.Failure(ResultErrorCode.InvalidArgument, "At least one hotkey modifier is required.");
        }

        uint nativeModifiers = Win32HotkeyRegistrar.NoRepeatModifier;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            nativeModifiers |= Win32HotkeyRegistrar.AltModifier;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            nativeModifiers |= Win32HotkeyRegistrar.ControlModifier;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            nativeModifiers |= Win32HotkeyRegistrar.ShiftModifier;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            nativeModifiers |= Win32HotkeyRegistrar.WindowsModifier;
        }

        return Result<uint>.Success(nativeModifiers);
    }

    private static Result<uint> MapVirtualKey(HotkeyKey key)
    {
        return key switch
        {
            HotkeyKey.Left => Result<uint>.Success(0x25u),
            HotkeyKey.Up => Result<uint>.Success(0x26u),
            HotkeyKey.Right => Result<uint>.Success(0x27u),
            HotkeyKey.Down => Result<uint>.Success(0x28u),
            HotkeyKey.D1 => Result<uint>.Success(0x31u),
            HotkeyKey.D2 => Result<uint>.Success(0x32u),
            HotkeyKey.D3 => Result<uint>.Success(0x33u),
            HotkeyKey.D4 => Result<uint>.Success(0x34u),
            HotkeyKey.Space => Result<uint>.Success(0x20u),
            _ => Result<uint>.Failure(ResultErrorCode.InvalidArgument, $"Hotkey key '{key}' is not supported.")
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private readonly record struct Registration(int Id, HotkeyDefinition Definition);
}
