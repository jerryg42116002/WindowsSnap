using WindowSnapper.Core.Results;

namespace WindowSnapper.Hotkeys;

/// <summary>
/// Registers and unregisters hotkeys with a platform-specific backend.
/// </summary>
public interface IHotkeyRegistrar
{
    /// <summary>
    /// Raised by the platform backend when a registered hotkey is pressed.
    /// </summary>
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    /// <summary>
    /// Registers a hotkey.
    /// </summary>
    Result Register(HotkeyDefinition definition);

    /// <summary>
    /// Unregisters a hotkey.
    /// </summary>
    Result Unregister(HotkeyDefinition definition);

    /// <summary>
    /// Unregisters every hotkey registered by this registrar.
    /// </summary>
    Result UnregisterAll();
}
