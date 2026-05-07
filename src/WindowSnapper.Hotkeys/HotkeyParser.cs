using WindowSnapper.Core.Results;

namespace WindowSnapper.Hotkeys;

/// <summary>
/// Parses user-facing hotkey chord strings.
/// </summary>
public static class HotkeyParser
{
    /// <summary>
    /// Parses a hotkey definition for the specified command.
    /// </summary>
    public static Result<HotkeyDefinition> Parse(string text, HotkeyCommand command)
    {
        if (command == HotkeyCommand.None)
        {
            return Result<HotkeyDefinition>.Failure(ResultErrorCode.InvalidArgument, "Hotkey command is required.");
        }

        var chord = ParseChord(text);
        return chord.IsFailure
            ? Result<HotkeyDefinition>.Failure(chord.ErrorCode, chord.ErrorMessage)
            : Result<HotkeyDefinition>.Success(new HotkeyDefinition(command, chord.Value.Modifiers, chord.Value.Key));
    }

    /// <summary>
    /// Parses a hotkey chord without assigning a command.
    /// </summary>
    public static Result<(HotkeyModifiers Modifiers, HotkeyKey Key)> ParseChord(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<(HotkeyModifiers, HotkeyKey)>.Failure(ResultErrorCode.InvalidArgument, "Hotkey text is required.");
        }

        var modifiers = HotkeyModifiers.None;
        HotkeyKey key = HotkeyKey.None;
        foreach (var token in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseModifier(token, out var modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (TryParseKey(token, out var parsedKey))
            {
                if (key != HotkeyKey.None)
                {
                    return Result<(HotkeyModifiers, HotkeyKey)>.Failure(
                        ResultErrorCode.InvalidArgument,
                        "Hotkey must contain exactly one non-modifier key.");
                }

                key = parsedKey;
                continue;
            }

            return Result<(HotkeyModifiers, HotkeyKey)>.Failure(
                ResultErrorCode.InvalidArgument,
                $"Hotkey token '{token}' is not supported.");
        }

        if (modifiers == HotkeyModifiers.None || key == HotkeyKey.None)
        {
            return Result<(HotkeyModifiers, HotkeyKey)>.Failure(
                ResultErrorCode.InvalidArgument,
                "Hotkey must contain at least one modifier and one non-modifier key.");
        }

        return Result<(HotkeyModifiers, HotkeyKey)>.Success((modifiers, key));
    }

    /// <summary>
    /// Formats a hotkey chord.
    /// </summary>
    public static string FormatChord(HotkeyModifiers modifiers, HotkeyKey key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(key));
        return string.Join("+", parts);
    }

    private static bool TryParseModifier(string token, out HotkeyModifiers modifier)
    {
        modifier = token.ToLowerInvariant() switch
        {
            "ctrl" or "control" => HotkeyModifiers.Control,
            "alt" => HotkeyModifiers.Alt,
            "shift" => HotkeyModifiers.Shift,
            "win" or "windows" or "meta" => HotkeyModifiers.Windows,
            _ => HotkeyModifiers.None
        };

        return modifier != HotkeyModifiers.None;
    }

    private static bool TryParseKey(string token, out HotkeyKey key)
    {
        key = token.ToLowerInvariant() switch
        {
            "left" => HotkeyKey.Left,
            "right" => HotkeyKey.Right,
            "up" => HotkeyKey.Up,
            "down" => HotkeyKey.Down,
            "1" or "d1" => HotkeyKey.D1,
            "2" or "d2" => HotkeyKey.D2,
            "3" or "d3" => HotkeyKey.D3,
            "4" or "d4" => HotkeyKey.D4,
            "space" => HotkeyKey.Space,
            _ => HotkeyKey.None
        };

        return key != HotkeyKey.None;
    }

    private static string FormatKey(HotkeyKey key)
    {
        return key switch
        {
            HotkeyKey.Left => "Left",
            HotkeyKey.Right => "Right",
            HotkeyKey.Up => "Up",
            HotkeyKey.Down => "Down",
            HotkeyKey.D1 => "1",
            HotkeyKey.D2 => "2",
            HotkeyKey.D3 => "3",
            HotkeyKey.D4 => "4",
            HotkeyKey.Space => "Space",
            _ => "None"
        };
    }
}
