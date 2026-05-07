using System.Drawing;
using System.Windows.Forms;

namespace WindowSnapper.Tray;

/// <summary>
/// Windows Forms NotifyIcon implementation for the system tray entry.
/// </summary>
public sealed class NotifyIconTrayIcon : ITrayIcon
{
    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip menu;
    private readonly ToolStripMenuItem openMainWindowItem;
    private readonly ToolStripMenuItem openSettingsItem;
    private readonly ToolStripMenuItem layoutsItem;
    private readonly ToolStripMenuItem toggleHotkeysItem;
    private readonly ToolStripMenuItem exitItem;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyIconTrayIcon"/> class.
    /// </summary>
    public NotifyIconTrayIcon()
    {
        openMainWindowItem = CreateMenuItem("打开主窗口", TrayMenuCommand.OpenMainWindow);
        openSettingsItem = CreateMenuItem("设置", TrayMenuCommand.OpenSettings);
        layoutsItem = new ToolStripMenuItem("布局");
        toggleHotkeysItem = CreateMenuItem("暂停快捷键", TrayMenuCommand.ToggleHotkeysPaused);
        exitItem = CreateMenuItem("退出", TrayMenuCommand.Exit);

        menu = new ContextMenuStrip();
        menu.Items.Add(openMainWindowItem);
        menu.Items.Add(openSettingsItem);
        menu.Items.Add(layoutsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(toggleHotkeysItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application,
            Text = "WindowSnapper",
            Visible = false
        };
        notifyIcon.DoubleClick += (_, _) => RaiseCommand(TrayMenuCommand.OpenMainWindow);
    }

    /// <inheritdoc />
    public event EventHandler<TrayMenuCommandEventArgs>? CommandRequested;

    /// <inheritdoc />
    public void Show(TrayMenuState state)
    {
        ThrowIfDisposed();

        UpdateState(state);
        notifyIcon.Visible = true;
    }

    /// <inheritdoc />
    public void UpdateState(TrayMenuState state)
    {
        ThrowIfDisposed();

        toggleHotkeysItem.Text = state.HotkeysPaused ? "恢复快捷键" : "暂停快捷键";
        RebuildLayoutsMenu(state.Layouts ?? []);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        disposed = true;
    }

    private ToolStripMenuItem CreateMenuItem(string text, TrayMenuCommand command)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => RaiseCommand(command);
        return item;
    }

    private void RaiseCommand(TrayMenuCommand command)
    {
        CommandRequested?.Invoke(this, new TrayMenuCommandEventArgs(command));
    }

    private void RaiseLayoutCommand(string layoutId, string zoneId)
    {
        CommandRequested?.Invoke(
            this,
            new TrayMenuCommandEventArgs(TrayMenuCommand.SnapLayoutZone, layoutId, zoneId));
    }

    private void RebuildLayoutsMenu(IReadOnlyList<TrayLayoutMenuItem> layouts)
    {
        layoutsItem.DropDownItems.Clear();

        if (layouts.Count == 0)
        {
            var emptyItem = new ToolStripMenuItem("无可用布局")
            {
                Enabled = false
            };
            layoutsItem.DropDownItems.Add(emptyItem);
            return;
        }

        foreach (var layout in layouts)
        {
            if (layout.Zones.Count == 1)
            {
                var zone = layout.Zones[0];
                var item = new ToolStripMenuItem(layout.Name);
                item.Click += (_, _) => RaiseLayoutCommand(layout.Id, zone.Id);
                layoutsItem.DropDownItems.Add(item);
                continue;
            }

            var layoutItem = new ToolStripMenuItem(layout.Name);
            foreach (var zone in layout.Zones)
            {
                var zoneItem = new ToolStripMenuItem(zone.Name);
                zoneItem.Click += (_, _) => RaiseLayoutCommand(layout.Id, zone.Id);
                layoutItem.DropDownItems.Add(zoneItem);
            }

            layoutsItem.DropDownItems.Add(layoutItem);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
