using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StickyNotesInator.Services;

/// <summary>
/// Manages the system tray icon and context menu for the sticky notes application.
/// Provides a clean interface for system tray operations including creating new notes,
/// showing/hiding notes, and application exit.
/// </summary>
public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ILogger<TrayService> _logger;
    private bool _disposed = false;

    /// <summary>
    /// Event raised when a new note is requested
    /// </summary>
    public event EventHandler? CreateNoteRequested;

    /// <summary>
    /// Event raised when showing all notes is requested
    /// </summary>
    public event EventHandler? ShowAllNotesRequested;

    /// <summary>
    /// Event raised when hiding all notes is requested
    /// </summary>
    public event EventHandler? HideAllNotesRequested;

    /// <summary>
    /// Event raised when application exit is requested
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Event raised when deleting all notes is requested
    /// </summary>
    public event EventHandler? DeleteAllNotesRequested;

    /// <summary>
    /// Initializes a new instance of the TrayService class
    /// </summary>
    /// <param name="logger">Logger instance for error tracking</param>
    public TrayService(ILogger<TrayService>? logger = null)
    {
        _logger = logger ?? new NullLogger<TrayService>();

        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                Text = "StickyNotes-inator",
                Visible = false
            };

            SetupContextMenu();
            SetupEventHandlers();

            _logger.LogInformation("Tray service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tray service");
            throw;
        }
    }

    /// <summary>
    /// Creates a simple sticky note icon for the tray
    /// </summary>
    /// <returns>Icon for the system tray</returns>
    private Icon CreateTrayIcon()
    {
        try
        {
            // Create a simple 16x16 icon
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);

            // Clear background
            graphics.Clear(Color.Transparent);

            // Draw sticky note shape
            using var brush = new SolidBrush(Color.FromArgb(254, 249, 231)); // Light yellow
            using var pen = new Pen(Color.FromArgb(243, 156, 18), 1); // Orange border

            // Main note body
            graphics.FillRectangle(brush, 2, 2, 12, 12);
            graphics.DrawRectangle(pen, 2, 2, 12, 12);

            // Folded corner effect
            var points = new Point[]
            {
                new Point(10, 2),
                new Point(14, 6),
                new Point(10, 10),
                new Point(10, 2)
            };
            using var cornerBrush = new SolidBrush(Color.FromArgb(243, 156, 18));
            graphics.FillPolygon(cornerBrush, points);

            // Text lines
            using var textPen = new Pen(Color.FromArgb(44, 62, 80), 1);
            graphics.DrawLine(textPen, 4, 6, 10, 6);
            graphics.DrawLine(textPen, 4, 8, 12, 8);
            graphics.DrawLine(textPen, 4, 10, 11, 10);

            return Icon.FromHandle(bitmap.GetHicon());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tray icon");
            // Return a default icon if creation fails
            return SystemIcons.Application;
        }
    }

    /// <summary>
    /// Sets up the context menu for the tray icon
    /// </summary>
    private void SetupContextMenu()
    {
        try
        {
            var contextMenu = new ContextMenuStrip();

            // New Note menu item
            var newNoteItem = new ToolStripMenuItem("📝 New Note", null, OnNewNoteClicked);
            contextMenu.Items.Add(newNoteItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Show All Notes menu item
            var showNotesItem = new ToolStripMenuItem("👁️ Show All Notes", null, OnShowAllNotesClicked);
            contextMenu.Items.Add(showNotesItem);

            // Hide All Notes menu item
            var hideNotesItem = new ToolStripMenuItem("🙈 Hide All Notes", null, OnHideAllNotesClicked);
            contextMenu.Items.Add(hideNotesItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Delete All Notes menu item
            var deleteAllNotesItem = new ToolStripMenuItem("🗑️ Delete All Notes", null, OnDeleteAllNotesClicked);
            contextMenu.Items.Add(deleteAllNotesItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit menu item
            var exitItem = new ToolStripMenuItem("❌ Exit", null, OnExitClicked);
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            _logger.LogInformation("Context menu setup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup context menu");
            throw;
        }
    }

    /// <summary>
    /// Sets up event handlers for the tray icon
    /// </summary>
    private void SetupEventHandlers()
    {
        _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
    }

    /// <summary>
    /// Handles new note menu item click
    /// </summary>
    private void OnNewNoteClicked(object? sender, EventArgs e)
    {
        try
        {
            CreateNoteRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("New note requested via tray menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling new note request");
        }
    }

    /// <summary>
    /// Handles show all notes menu item click
    /// </summary>
    private void OnShowAllNotesClicked(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Show all notes clicked in tray menu");
            ShowAllNotesRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Show all notes requested via tray menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling show all notes request");
        }
    }

    /// <summary>
    /// Handles hide all notes menu item click
    /// </summary>
    private void OnHideAllNotesClicked(object? sender, EventArgs e)
    {
        try
        {
            HideAllNotesRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Hide all notes requested via tray menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling hide all notes request");
        }
    }

    /// <summary>
    /// Handles delete all notes menu item click
    /// </summary>
    private void OnDeleteAllNotesClicked(object? sender, EventArgs e)
    {
        try
        {
            DeleteAllNotesRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Delete all notes requested via tray menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete all notes request");
        }
    }

    /// <summary>
    /// Handles exit menu item click
    /// </summary>
    private void OnExitClicked(object? sender, EventArgs e)
    {
        try
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Exit requested via tray menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling exit request");
        }
    }

    /// <summary>
    /// Handles tray icon double-click
    /// </summary>
    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        try
        {
            ShowAllNotesRequested?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Show all notes requested via tray icon double-click");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tray icon double-click");
        }
    }

    /// <summary>
    /// Shows the tray icon
    /// </summary>
    public void Show()
    {
        try
        {
            if (!_disposed)
            {
                _notifyIcon.Visible = true;
                _logger.LogInformation("Tray icon shown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show tray icon");
        }
    }

    /// <summary>
    /// Hides the tray icon
    /// </summary>
    public void Hide()
    {
        try
        {
            if (!_disposed)
            {
                _notifyIcon.Visible = false;
                _logger.LogInformation("Tray icon hidden");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide tray icon");
        }
    }

    /// <summary>
    /// Shows a notification message
    /// </summary>
    /// <param name="title">Message title</param>
    /// <param name="message">Message content</param>
    /// <param name="duration">Display duration in milliseconds</param>
    public void ShowNotification(string title, string message, int duration = 3000)
    {
        try
        {
            if (!_disposed)
            {
                _notifyIcon.ShowBalloonTip(duration, title, message, ToolTipIcon.Info);
                _logger.LogDebug("Tray notification shown: {Title} - {Message}", title, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show tray notification");
        }
    }

    /// <summary>
    /// Updates the tray icon tooltip text
    /// </summary>
    /// <param name="text">New tooltip text</param>
    public void UpdateTooltip(string text)
    {
        try
        {
            if (!_disposed)
            {
                _notifyIcon.Text = text;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tray tooltip");
        }
    }

    /// <summary>
    /// Checks if the system tray is available
    /// </summary>
    /// <returns>True if system tray is available, false otherwise</returns>
    public static bool IsSystemTrayAvailable()
    {
        try
        {
            // Check if we can create a NotifyIcon
            using var testIcon = new NotifyIcon();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Disposes of the tray service and cleans up resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the tray service and cleans up resources
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _notifyIcon?.Dispose();
                _logger.LogInformation("Tray service disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing tray service");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
} 