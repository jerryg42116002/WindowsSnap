namespace WindowSnapper.Core.Windows;

/// <summary>
/// Identifies a desktop window without exposing native APIs to higher layers.
/// </summary>
/// <param name="Value">The opaque platform handle value.</param>
public readonly record struct WindowHandle(nint Value)
{
    /// <summary>
    /// Represents the absence of a window handle.
    /// </summary>
    public static WindowHandle None { get; } = new(0);

    /// <summary>
    /// Gets whether this handle is empty.
    /// </summary>
    public bool IsNone => Value == 0;

    /// <summary>
    /// Gets whether this handle contains a non-zero value.
    /// </summary>
    public bool IsValid => !IsNone;

    /// <summary>
    /// Creates a window handle from an <see cref="IntPtr"/>.
    /// </summary>
    public static WindowHandle FromIntPtr(IntPtr value) => new(value);

    /// <summary>
    /// Converts this handle to an <see cref="IntPtr"/>.
    /// </summary>
    public IntPtr ToIntPtr() => Value;
}
