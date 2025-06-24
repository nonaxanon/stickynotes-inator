using StickyNotesInator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Timer = System.Windows.Forms.Timer;
using System.Drawing.Drawing2D;

namespace StickyNotesInator.Forms;

/// <summary>
/// Represents an individual sticky note form with drag functionality and auto-save.
/// This form features a modern material design theme with rounded corners and shadows.
/// </summary>
public partial class StickyNoteForm : Form
{
    private readonly Note _note;
    private readonly NoteStorage _storage;
    private readonly ILogger<StickyNoteForm> _logger;
    private readonly Timer _autoSaveTimer;
    private bool _isDragging = false;
    private Point _dragOffset;
    
    // Material Design Colors
    private static readonly Color[] MaterialColors = {
        Color.FromArgb(255, 87, 34),   // Deep Orange
        Color.FromArgb(156, 39, 176),  // Purple
        Color.FromArgb(63, 81, 181),   // Indigo
        Color.FromArgb(33, 150, 243),  // Blue
        Color.FromArgb(0, 150, 136),   // Teal
        Color.FromArgb(76, 175, 80),   // Green
        Color.FromArgb(255, 193, 7),   // Amber
        Color.FromArgb(255, 152, 0),   // Orange
        Color.FromArgb(233, 30, 99),   // Pink
        Color.FromArgb(121, 85, 72)    // Brown
    };
    
    private readonly Color _primaryColor;
    private readonly Color _primaryLightColor;
    private readonly Color _primaryDarkColor;
    private readonly Color _accentColor;
    private readonly Color _backgroundColor;
    private readonly Color _surfaceColor;
    private readonly Color _textPrimaryColor;
    private readonly Color _textSecondaryColor;

    /// <summary>
    /// Event raised when the note content changes
    /// </summary>
    public event EventHandler<Note>? NoteChanged;

    /// <summary>
    /// Event raised when the note is closed
    /// </summary>
    public event EventHandler<string>? NoteClosed;

    /// <summary>
    /// Initializes a new instance of the StickyNoteForm class
    /// </summary>
    /// <param name="note">The note data to display</param>
    /// <param name="storage">Storage service for persistence</param>
    /// <param name="logger">Logger instance for error tracking</param>
    public StickyNoteForm(Note note, NoteStorage storage, ILogger<StickyNoteForm>? logger = null)
    {
        _note = note ?? throw new ArgumentNullException(nameof(note));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? new NullLogger<StickyNoteForm>();

        // Select a color based on note ID for consistency
        var colorIndex = Math.Abs(_note.Id.GetHashCode()) % MaterialColors.Length;
        _primaryColor = MaterialColors[colorIndex];
        _primaryLightColor = LightenColor(_primaryColor, 0.3f);
        _primaryDarkColor = DarkenColor(_primaryColor, 0.3f);
        _accentColor = GetAccentColor(_primaryColor);
        _backgroundColor = Color.FromArgb(250, 250, 250);
        _surfaceColor = Color.White;
        _textPrimaryColor = Color.FromArgb(33, 33, 33);
        _textSecondaryColor = Color.FromArgb(117, 117, 117);

        // Initialize auto-save timer
        _autoSaveTimer = new Timer
        {
            Interval = 2000, // Save after 2 seconds of inactivity
            Enabled = false
        };
        _autoSaveTimer.Tick += OnAutoSaveTimer;

        SetupForm();
        LoadNoteData();

        _logger.LogInformation("Sticky note form initialized for note: {NoteId}", _note.Id);
    }

    /// <summary>
    /// Sets up the form properties and material design styling
    /// </summary>
    private void SetupForm()
    {
        // Form properties
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = _backgroundColor;
        TabStop = true;

        // Set size and position
        Size = new Size(_note.Width, _note.Height);
        Location = new Point(_note.X, _note.Y);

        // Enable double buffering
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

        // Create main container panel
        var mainPanel = new Panel
        {
            Name = "MainPanel",
            Dock = DockStyle.Fill,
            BackColor = _surfaceColor,
            Padding = new Padding(0)
        };

        // Create title bar panel with gradient background
        var titlePanel = new Panel
        {
            Name = "TitlePanel",
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = _primaryColor,
            Cursor = Cursors.SizeAll,
            Padding = new Padding(12, 8, 12, 8)
        };

        // Title label with modern typography
        var titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "📝 Note",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(12, 10),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // Modern close button with hover effects
        var closeButton = new Button
        {
            Name = "CloseButton",
            Text = "×",
            Size = new Size(28, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };

        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 255, 255);
        closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(100, 255, 255, 255);
        closeButton.Click += (s, e) => CloseNote();

        // Create modern text area
        var textBox = new TextBox
        {
            Name = "NoteTextBox",
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = _surfaceColor,
            ForeColor = _textPrimaryColor,
            Font = new Font("Segoe UI", 10),
            Text = _note.Content,
            AcceptsReturn = true,
            AcceptsTab = true,
            WordWrap = true,
            Margin = new Padding(12, 8, 12, 12),
            ReadOnly = false,
            Enabled = true,
            TabStop = true
        };

        // Add text changed event
        textBox.TextChanged += OnTextChanged;
        
        // Add click event to ensure focus
        textBox.Click += (s, e) => {
            textBox.Focus();
            _logger.LogDebug("Text box clicked and focused");
        };
        
        // Add got focus event for debugging
        textBox.GotFocus += (s, e) => {
            _logger.LogDebug("Text box gained focus");
        };
        
        // Add lost focus event for debugging
        textBox.LostFocus += (s, e) => {
            _logger.LogDebug("Text box lost focus");
        };

        // Add controls to panels
        titlePanel.Controls.Add(titleLabel);
        titlePanel.Controls.Add(closeButton);
        
        mainPanel.Controls.Add(titlePanel);
        mainPanel.Controls.Add(textBox);

        // Add main panel to form
        Controls.Add(mainPanel);

        // Ensure text box is properly accessible and can receive focus
        textBox.BringToFront();
        textBox.Focus();

        // Position close button
        closeButton.Location = new Point(titlePanel.Width - 40, 6);

        // Setup layout event to properly position close button
        titlePanel.Layout += (s, e) => {
            closeButton.Location = new Point(titlePanel.Width - 40, 6);
        };

        // Setup drag events
        titlePanel.MouseDown += OnTitleBarMouseDown;
        titlePanel.MouseMove += OnTitleBarMouseMove;
        titlePanel.MouseUp += OnTitleBarMouseUp;

        // Handle form events
        FormClosing += OnFormClosing;
        ResizeEnd += OnResizeEnd;
        Move += OnFormMove;
        
        // Add form click event to ensure text box gets focus
        Click += (s, e) => {
            var textBox = Controls.Find("NoteTextBox", true).FirstOrDefault() as TextBox;
            textBox?.Focus();
        };
        
        // Add form activated event
        Activated += (s, e) => {
            var textBox = Controls.Find("NoteTextBox", true).FirstOrDefault() as TextBox;
            textBox?.Focus();
        };
    }

    /// <summary>
    /// Loads the note data into the form
    /// </summary>
    private void LoadNoteData()
    {
        try
        {
            var textBox = Controls.Find("NoteTextBox", true).FirstOrDefault() as TextBox;
            if (textBox != null)
            {
                textBox.Text = _note.Content;
                textBox.Focus(); // Give focus to the text box
                _logger.LogDebug("Note data loaded: {ContentLength} characters", _note.Content.Length);
            }
            else
            {
                _logger.LogWarning("NoteTextBox control not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading note data");
        }
    }

    /// <summary>
    /// Handles text changes with auto-save functionality
    /// </summary>
    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _note.Content = textBox.Text;
            _note.UpdateTimestamp();

            // Reset auto-save timer
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();

            // Notify listeners
            NoteChanged?.Invoke(this, _note);
        }
    }

    /// <summary>
    /// Handles auto-save timer tick
    /// </summary>
    private void OnAutoSaveTimer(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        SaveNote();
    }

    /// <summary>
    /// Saves the current note state
    /// </summary>
    private void SaveNote()
    {
        try
        {
            // Update note position and size
            _note.X = Location.X;
            _note.Y = Location.Y;
            _note.Width = Width;
            _note.Height = Height;
            _note.UpdateTimestamp();

            var success = _storage.SaveNote(_note);
            if (success)
            {
                _logger.LogDebug("Note auto-saved: {NoteId}", _note.Id);
            }
            else
            {
                _logger.LogWarning("Failed to auto-save note: {NoteId}", _note.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-save for note: {NoteId}", _note.Id);
        }
    }

    /// <summary>
    /// Handles title bar mouse down for dragging
    /// </summary>
    private void OnTitleBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragOffset = e.Location;
            Cursor = Cursors.SizeAll;
        }
    }

    /// <summary>
    /// Handles title bar mouse move for dragging
    /// </summary>
    private void OnTitleBarMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var newLocation = PointToScreen(e.Location);
            newLocation.Offset(-_dragOffset.X, -_dragOffset.Y);
            Location = newLocation;
        }
    }

    /// <summary>
    /// Handles title bar mouse up for dragging
    /// </summary>
    private void OnTitleBarMouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// Handles form closing
    /// </summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            // Save before closing
            SaveNote();

            // Notify listeners
            NoteClosed?.Invoke(this, _note.Id);

            // Clean up timer
            _autoSaveTimer?.Dispose();

            _logger.LogInformation("Note form closed: {NoteId}", _note.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing note form: {NoteId}", _note.Id);
        }
    }

    /// <summary>
    /// Handles form resize end
    /// </summary>
    private void OnResizeEnd(object? sender, EventArgs e)
    {
        SaveNote();
    }

    /// <summary>
    /// Handles form move
    /// </summary>
    private void OnFormMove(object? sender, EventArgs e)
    {
        // Debounce move events to avoid excessive saves
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    /// <summary>
    /// Closes the note and triggers cleanup
    /// </summary>
    public void CloseNote()
    {
        Close();
    }

    /// <summary>
    /// Updates the note content programmatically
    /// </summary>
    /// <param name="content">New content text</param>
    public void UpdateContent(string content)
    {
        var textBox = Controls.Find("NoteTextBox", true).FirstOrDefault() as TextBox;
        if (textBox != null)
        {
            textBox.Text = content;
            _note.Content = content;
            _note.UpdateTimestamp();
        }
    }

    /// <summary>
    /// Updates the note's visibility state
    /// </summary>
    /// <param name="isVisible">Whether the note should be visible</param>
    public void UpdateVisibility(bool isVisible)
    {
        _note.IsVisible = isVisible;
        _note.UpdateTimestamp();
    }

    /// <summary>
    /// Gets the current note data
    /// </summary>
    /// <returns>The current note instance</returns>
    public Note GetNoteData()
    {
        // Update the note with current form state
        _note.X = Location.X;
        _note.Y = Location.Y;
        _note.Width = Width;
        _note.Height = Height;
        // Don't override IsVisible here - it should be managed explicitly
        _note.UpdateTimestamp();
        
        return _note;
    }

    /// <summary>
    /// Shows the note and brings it to front
    /// </summary>
    public new void Show()
    {
        base.Show();
        BringToFront();
        Activate();
        
        // Ensure text box gets focus
        var textBox = Controls.Find("NoteTextBox", true).FirstOrDefault() as TextBox;
        if (textBox != null)
        {
            textBox.Focus();
            _logger.LogDebug("Note shown and text box focused");
        }
    }

    /// <summary>
    /// Hides the note
    /// </summary>
    public new void Hide()
    {
        base.Hide();
    }

    #region Helper Methods for Material Design

    /// <summary>
    /// Lightens a color by the specified factor
    /// </summary>
    private static Color LightenColor(Color color, float factor)
    {
        return Color.FromArgb(
            color.A,
            Math.Min(255, (int)(color.R + (255 - color.R) * factor)),
            Math.Min(255, (int)(color.G + (255 - color.G) * factor)),
            Math.Min(255, (int)(color.B + (255 - color.B) * factor))
        );
    }

    /// <summary>
    /// Darkens a color by the specified factor
    /// </summary>
    private static Color DarkenColor(Color color, float factor)
    {
        return Color.FromArgb(
            color.A,
            Math.Max(0, (int)(color.R * (1 - factor))),
            Math.Max(0, (int)(color.G * (1 - factor))),
            Math.Max(0, (int)(color.B * (1 - factor)))
        );
    }

    /// <summary>
    /// Gets an accent color that complements the primary color
    /// </summary>
    private static Color GetAccentColor(Color primaryColor)
    {
        // Calculate complementary color
        return Color.FromArgb(
            primaryColor.A,
            255 - primaryColor.R,
            255 - primaryColor.G,
            255 - primaryColor.B
        );
    }

    #endregion
}

/// <summary>
/// Extension methods for drawing rounded rectangles
/// </summary>
public static class GraphicsExtensions
{
    /// <summary>
    /// Fills a rounded rectangle
    /// </summary>
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius, bool topOnly = false)
    {
        using var path = CreateRoundedRectanglePath(rect, radius, topOnly);
        g.FillPath(brush, path);
    }

    /// <summary>
    /// Draws a rounded rectangle
    /// </summary>
    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using var path = CreateRoundedRectanglePath(rect, radius);
        g.DrawPath(pen, path);
    }

    /// <summary>
    /// Creates a rounded rectangle path
    /// </summary>
    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius, bool topOnly = false)
    {
        var path = new GraphicsPath();
        
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        var size = new Size(diameter, diameter);
        var arc = new Rectangle(rect.Location, size);

        // Top left corner
        path.AddArc(arc, 180, 90);

        // Top right corner
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        if (!topOnly)
        {
            // Bottom right corner
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left corner
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
        }

        path.CloseFigure();
        return path;
    }
} 