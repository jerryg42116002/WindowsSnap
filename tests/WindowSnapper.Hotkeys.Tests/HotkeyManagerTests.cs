using WindowSnapper.Core.Results;

namespace WindowSnapper.Hotkeys.Tests;

public sealed class HotkeyManagerTests
{
    [Fact]
    public void DuplicateHotkeysReturnConflictFailure()
    {
        var registrar = new FakeHotkeyRegistrar();
        using var manager = new HotkeyManager(registrar);
        var first = new HotkeyDefinition(HotkeyCommand.SnapLeftHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Left);
        var second = new HotkeyDefinition(HotkeyCommand.SnapRightHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Left);

        var result = manager.RegisterMany([first, second]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Contains("conflict", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(manager.RegisteredHotkeys);
    }

    [Fact]
    public void RegistrationFailureDoesNotThrow()
    {
        var registrar = new FakeHotkeyRegistrar
        {
            Failure = Result.Failure(ResultErrorCode.PlatformCallFailed, "RegisterHotKey failed.")
        };
        using var manager = new HotkeyManager(registrar);

        var exception = Record.Exception(() => manager.Register(DefaultHotkeys.All[0]));

        Assert.Null(exception);
        var result = manager.Register(DefaultHotkeys.All[0]);
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.PlatformCallFailed, result.ErrorCode);
        Assert.Empty(manager.RegisteredHotkeys);
    }

    [Fact]
    public void DisposeUnregistersRegisteredHotkeys()
    {
        var registrar = new FakeHotkeyRegistrar();
        var manager = new HotkeyManager(registrar);

        var result = manager.Register(DefaultHotkeys.All[0]);
        manager.Dispose();

        Assert.True(result.IsSuccess);
        Assert.True(registrar.UnregisterAllCalled);
    }

    [Fact]
    public void DispatchesRegisteredHotkeyPress()
    {
        var registrar = new FakeHotkeyRegistrar();
        using var manager = new HotkeyManager(registrar);
        var definition = DefaultHotkeys.All[0];
        HotkeyCommand? received = null;
        manager.HotkeyPressed += (_, args) => received = args.Command;
        _ = manager.Register(definition);

        registrar.Raise(definition);

        Assert.Equal(definition.Command, received);
    }

    private sealed class FakeHotkeyRegistrar : IHotkeyRegistrar
    {
        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        public Result? Failure { get; init; }

        public bool UnregisterAllCalled { get; private set; }

        public Result Register(HotkeyDefinition definition)
        {
            return Failure ?? Result.Success();
        }

        public Result Unregister(HotkeyDefinition definition)
        {
            return Result.Success();
        }

        public Result UnregisterAll()
        {
            UnregisterAllCalled = true;
            return Result.Success();
        }

        public void Raise(HotkeyDefinition definition)
        {
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(definition));
        }
    }
}
