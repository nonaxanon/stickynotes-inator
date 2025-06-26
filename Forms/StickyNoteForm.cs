using StickyNotesInator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Timer = System.Windows.Forms.Timer;
using System.Drawing.Drawing2D;

namespace StickyNotesInator.Forms;

/// <summary>
/// Represents the direction of resizing for a form
/// </summary>
public enum ResizeDirection
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

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
    private bool _isResizing = false;
    private Point _dragOffset;
    private Point _resizeStartPoint;
    private Size _resizeStartSize;
    private ResizeDirection _resizeDirection = ResizeDirection.None;
    
    // Size constraints for resizable notes
    private const int MIN_WIDTH = 50;
    private const int MIN_HEIGHT = 50;
    private const int MAX_WIDTH = 9999;
    private const int MAX_HEIGHT = 9999;
    private const int RESIZE_HANDLE_SIZE = 12;
    
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
    /// Gets or sets the minimum width for the note
    /// </summary>
    public int MinWidth { get; set; } = MIN_WIDTH;

    /// <summary>
    /// Gets or sets the minimum height for the note
    /// </summary>
    public int MinHeight { get; set; } = MIN_HEIGHT;

    /// <summary>
    /// Gets or sets the maximum width for the note
    /// </summary>
    public int MaxWidth { get; set; } = MAX_WIDTH;

    /// <summary>
    /// Gets or sets the maximum height for the note
    /// </summary>
    public int MaxHeight { get; set; } = MAX_HEIGHT;

    /// <summary>
    /// Initializes a new instance of the StickyNoteForm class
    /// </summary>
    /// <param name="note">The note data to display</param>
    /// <param name="storage">Storage service for persistence</param>
    /// <param name="logger">Logger instance for error tracking</param>
    /// <param name="minWidth">Optional minimum width (default: 200)</param>
    /// <param name="minHeight">Optional minimum height (default: 150)</param>
    /// <param name="maxWidth">Optional maximum width (default: 800)</param>
    /// <param name="maxHeight">Optional maximum height (default: 600)</param>
    public StickyNoteForm(Note note, NoteStorage storage, ILogger<StickyNoteForm>? logger = null, 
        int? minWidth = null, int? minHeight = null, int? maxWidth = null, int? maxHeight = null)
    {
        _note = note ?? throw new ArgumentNullException(nameof(note));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? new NullLogger<StickyNoteForm>();

        // Set custom size constraints if provided
        if (minWidth.HasValue) MinWidth = minWidth.Value;
        if (minHeight.HasValue) MinHeight = minHeight.Value;
        if (maxWidth.HasValue) MaxWidth = maxWidth.Value;
        if (maxHeight.HasValue) MaxHeight = maxHeight.Value;

        // Validate size constraints
        if (MinWidth >= MaxWidth || MinHeight >= MaxHeight)
        {
            throw new ArgumentException("Minimum dimensions must be less than maximum dimensions");
        }

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

        // Set size and position with constraints
        var constrainedSize = ConstrainSize(new Size(_note.Width, _note.Height));
        Size = constrainedSize;
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
            Font = new Font("Calibri", 11, FontStyle.Regular),
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

        // Setup resize events for the entire form
        MouseDown += OnFormMouseDown;
        MouseMove += OnFormMouseMove;
        MouseUp += OnFormMouseUp;

        // Handle form events
        FormClosing += OnFormClosing;
        ResizeEnd += OnResizeEnd;
        Resize += OnFormResize;
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
        
        // Add form shown event to ensure handles are drawn
        Shown += (s, e) => {
            // Handles are now created as controls, no need to force repaint
        };
        
        // Create visible resize handles
        CreateResizeHandles();
    }

    /// <summary>
    /// Creates visible resize handle controls on the form edges and corners
    /// </summary>
    private void CreateResizeHandles()
    {
        var handleSize = RESIZE_HANDLE_SIZE;
        
        // Create only bottom right corner handle
        CreateResizeHandle("BottomRight", Width - handleSize, Height - handleSize, handleSize, handleSize, ResizeDirection.BottomRight);
    }

    /// <summary>
    /// Creates a single resize handle control
    /// </summary>
    private void CreateResizeHandle(string name, int x, int y, int width, int height, ResizeDirection direction)
    {
        var handle = new Panel
        {
            Name = $"ResizeHandle_{name}",
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.Transparent,
            BorderStyle = BorderStyle.None,
            Cursor = GetCursorForDirection(direction),
            Tag = direction
        };
        
        // Make handles truly transparent by overriding the paint method
        handle.Paint += (s, e) => {
            // Do absolutely nothing - don't paint anything
            // This prevents any background from being drawn
        };
        
        // Add mouse events to handles to ensure they receive events
        handle.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                _isResizing = true;
                _resizeDirection = direction;
                _resizeStartPoint = PointToScreen(e.Location); // Capture initial screen position
                _resizeStartSize = Size;
                Capture = true;
            }
        };
        
        handle.MouseMove += (s, e) => {
            if (_isResizing)
            {
                // Calculate delta from initial screen position to current screen position
                var currentScreenPoint = PointToScreen(e.Location);
                var deltaX = currentScreenPoint.X - _resizeStartPoint.X;
                var deltaY = currentScreenPoint.Y - _resizeStartPoint.Y;
                
                var newSize = _resizeStartSize;
                var newLocation = Location;

                // Apply resize based on direction
                switch (_resizeDirection)
                {
                    case ResizeDirection.TopLeft:
                        newSize.Width -= deltaX;
                        newSize.Height -= deltaY;
                        newLocation.X += deltaX;
                        newLocation.Y += deltaY;
                        break;
                    case ResizeDirection.TopRight:
                        newSize.Width += deltaX;
                        newSize.Height -= deltaY;
                        newLocation.Y += deltaY;
                        break;
                    case ResizeDirection.BottomRight:
                        newSize.Width += deltaX;
                        newSize.Height += deltaY;
                        break;
                    case ResizeDirection.BottomLeft:
                        newSize.Width -= deltaX;
                        newSize.Height += deltaY;
                        newLocation.X += deltaX;
                        break;
                    case ResizeDirection.Left:
                        newSize.Width -= deltaX;
                        newLocation.X += deltaX;
                        break;
                    case ResizeDirection.Right:
                        newSize.Width += deltaX;
                        break;
                    case ResizeDirection.Top:
                        newSize.Height -= deltaY;
                        newLocation.Y += deltaY;
                        break;
                    case ResizeDirection.Bottom:
                        newSize.Height += deltaY;
                        break;
                }

                // Apply size constraints
                var constrainedSize = ConstrainSize(newSize);
                
                // Adjust location if size was constrained
                if (constrainedSize.Width != newSize.Width)
                {
                    if (_resizeDirection == ResizeDirection.Left || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.BottomLeft)
                    {
                        newLocation.X = Location.X + (newSize.Width - constrainedSize.Width);
                    }
                }
                if (constrainedSize.Height != newSize.Height)
                {
                    if (_resizeDirection == ResizeDirection.Top || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.TopRight)
                    {
                        newLocation.Y = Location.Y + (newSize.Height - constrainedSize.Height);
                    }
                }

                Size = constrainedSize;
                Location = newLocation;
                
                // Update handle positions immediately during resize
                UpdateResizeHandlePositions();
            }
        };
        
        handle.MouseUp += (s, e) => {
            if (_isResizing)
            {
                _isResizing = false;
                _resizeDirection = ResizeDirection.None;
                Capture = false;
                SaveNote();
            }
        };
        
        Controls.Add(handle);
        handle.BringToFront(); // Ensure handles are on top
    }

    /// <summary>
    /// Gets the appropriate cursor for a resize direction
    /// </summary>
    private Cursor GetCursorForDirection(ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.TopLeft => Cursors.SizeNWSE,
            ResizeDirection.TopRight => Cursors.SizeNESW,
            ResizeDirection.BottomRight => Cursors.SizeNWSE,
            ResizeDirection.BottomLeft => Cursors.SizeNESW,
            ResizeDirection.Left => Cursors.SizeWE,
            ResizeDirection.Right => Cursors.SizeWE,
            ResizeDirection.Top => Cursors.SizeNS,
            ResizeDirection.Bottom => Cursors.SizeNS,
            _ => Cursors.Default
        };
    }

    /// <summary>
    /// Constrains a size to the minimum and maximum limits
    /// </summary>
    private Size ConstrainSize(Size size)
    {
        return new Size(
            Math.Max(MinWidth, Math.Min(MaxWidth, size.Width)),
            Math.Max(MinHeight, Math.Min(MaxHeight, size.Height))
        );
    }

    /// <summary>
    /// Determines the resize direction based on mouse position
    /// </summary>
    private ResizeDirection GetResizeDirection(Point mousePos)
    {
        var clientPos = PointToClient(mousePos);
        
        // Check only bottom right corner
        if (clientPos.X >= Width - RESIZE_HANDLE_SIZE && clientPos.Y >= Height - RESIZE_HANDLE_SIZE)
            return ResizeDirection.BottomRight;
        
        return ResizeDirection.None;
    }

    /// <summary>
    /// Handles form mouse down for resizing
    /// </summary>
    private void OnFormMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var direction = GetResizeDirection(e.Location);
            if (direction != ResizeDirection.None)
            {
                _isResizing = true;
                _resizeDirection = direction;
                _resizeStartPoint = PointToScreen(e.Location); // Use screen coordinates
                _resizeStartSize = Size;
                Capture = true;
            }
        }
    }

    /// <summary>
    /// Handles form mouse move for resizing
    /// </summary>
    private void OnFormMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isResizing)
        {
            // Use screen coordinates for consistent calculations
            var currentScreenPoint = PointToScreen(e.Location);
            var deltaX = currentScreenPoint.X - _resizeStartPoint.X;
            var deltaY = currentScreenPoint.Y - _resizeStartPoint.Y;
            
            var newSize = _resizeStartSize;
            var newLocation = Location;

            // Apply resize based on direction
            switch (_resizeDirection)
            {
                case ResizeDirection.TopLeft:
                    newSize.Width -= deltaX;
                    newSize.Height -= deltaY;
                    newLocation.X += deltaX;
                    newLocation.Y += deltaY;
                    break;
                case ResizeDirection.TopRight:
                    newSize.Width += deltaX;
                    newSize.Height -= deltaY;
                    newLocation.Y += deltaY;
                    break;
                case ResizeDirection.BottomRight:
                    newSize.Width += deltaX;
                    newSize.Height += deltaY;
                    break;
                case ResizeDirection.BottomLeft:
                    newSize.Width -= deltaX;
                    newSize.Height += deltaY;
                    newLocation.X += deltaX;
                    break;
                case ResizeDirection.Left:
                    newSize.Width -= deltaX;
                    newLocation.X += deltaX;
                    break;
                case ResizeDirection.Right:
                    newSize.Width += deltaX;
                    break;
                case ResizeDirection.Top:
                    newSize.Height -= deltaY;
                    newLocation.Y += deltaY;
                    break;
                case ResizeDirection.Bottom:
                    newSize.Height += deltaY;
                    break;
            }

            // Apply size constraints
            var constrainedSize = ConstrainSize(newSize);
            
            // Adjust location if size was constrained
            if (constrainedSize.Width != newSize.Width)
            {
                if (_resizeDirection == ResizeDirection.Left || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.BottomLeft)
                {
                    newLocation.X = Location.X + (newSize.Width - constrainedSize.Width);
                }
            }
            if (constrainedSize.Height != newSize.Height)
            {
                if (_resizeDirection == ResizeDirection.Top || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.TopRight)
                {
                    newLocation.Y = Location.Y + (newSize.Height - constrainedSize.Height);
                }
            }

            Size = constrainedSize;
            Location = newLocation;
            
            // Update handle positions immediately during resize
            UpdateResizeHandlePositions();
        }
        else
        {
            // Update cursor for resize handles
            var direction = GetResizeDirection(e.Location);
            Cursor = GetCursorForDirection(direction);
        }
    }

    /// <summary>
    /// Handles form mouse up for resizing
    /// </summary>
    private void OnFormMouseUp(object? sender, MouseEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            _resizeDirection = ResizeDirection.None;
            Capture = false;
            Cursor = Cursors.Default;
            
            // Save the new size
            SaveNote();
        }
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
        UpdateResizeHandlePositions();
    }

    /// <summary>
    /// Handles form resize to update handle positions during resize
    /// </summary>
    private void OnFormResize(object? sender, EventArgs e)
    {
        // Update handle positions during resize to keep them in sync
        UpdateResizeHandlePositions();
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
    /// Updates the positions of all resize handles after form resize
    /// </summary>
    private void UpdateResizeHandlePositions()
    {
        var handleSize = RESIZE_HANDLE_SIZE;
        
        // Update only bottom right corner handle
        UpdateHandlePosition("BottomRight", Width - handleSize, Height - handleSize, handleSize, handleSize);
    }

    /// <summary>
    /// Updates the position and size of a specific resize handle
    /// </summary>
    private void UpdateHandlePosition(string handleName, int x, int y, int width, int height)
    {
        var handle = Controls.Find($"ResizeHandle_{handleName}", false).FirstOrDefault() as Panel;
        if (handle != null)
        {
            handle.Location = new Point(x, y);
            handle.Size = new Size(width, height);
        }
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

    /// <summary>
    /// Resizes the note to the specified dimensions while respecting size constraints
    /// </summary>
    /// <param name="width">Desired width</param>
    /// <param name="height">Desired height</param>
    public void ResizeNote(int width, int height)
    {
        var constrainedSize = ConstrainSize(new Size(width, height));
        Size = constrainedSize;
        SaveNote();
        _logger.LogDebug("Note resized to: {Width}x{Height}", constrainedSize.Width, constrainedSize.Height);
    }

    /// <summary>
    /// Gets the current size constraints
    /// </summary>
    /// <returns>Tuple containing (minWidth, minHeight, maxWidth, maxHeight)</returns>
    public (int minWidth, int minHeight, int maxWidth, int maxHeight) GetSizeConstraints()
    {
        return (MinWidth, MinHeight, MaxWidth, MaxHeight);
    }

    /// <summary>
    /// Sets new size constraints for the note
    /// </summary>
    /// <param name="minWidth">Minimum width</param>
    /// <param name="minHeight">Minimum height</param>
    /// <param name="maxWidth">Maximum width</param>
    /// <param name="maxHeight">Maximum height</param>
    public void SetSizeConstraints(int minWidth, int minHeight, int maxWidth, int maxHeight)
    {
        if (minWidth >= maxWidth || minHeight >= maxHeight)
        {
            throw new ArgumentException("Minimum dimensions must be less than maximum dimensions");
        }

        MinWidth = minWidth;
        MinHeight = minHeight;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;

        // Apply constraints to current size
        var constrainedSize = ConstrainSize(Size);
        if (constrainedSize != Size)
        {
            Size = constrainedSize;
            SaveNote();
        }

        _logger.LogDebug("Size constraints updated: {MinWidth}x{MinHeight} to {MaxWidth}x{MaxHeight}", 
            minWidth, minHeight, maxWidth, maxHeight);
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