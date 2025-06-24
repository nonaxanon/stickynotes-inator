using StickyNotesInator.Models;
using StickyNotesInator.Services;
using Microsoft.Extensions.Logging;

namespace StickyNotesInator.Forms;

/// <summary>
/// Main application form that coordinates all sticky note components.
/// This form is hidden and serves as the application coordinator.
/// </summary>
public partial class MainForm : Form
{
    private readonly NoteStorage _storage = null!;
    private readonly TrayService _trayService = null!;
    private readonly ILogger<MainForm> _logger;
    private readonly Dictionary<string, StickyNoteForm> _noteForms;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the MainForm class
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating loggers</param>
    public MainForm(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<MainForm>();
        _noteForms = new Dictionary<string, StickyNoteForm>();

        try
        {
            // Initialize storage
            _storage = new NoteStorage("data", _loggerFactory.CreateLogger<NoteStorage>());

            // Initialize tray service
            _trayService = new TrayService(_loggerFactory.CreateLogger<TrayService>());

            // Setup form
            SetupForm();
            SetupEventHandlers();
            LoadSavedNotes();

            // Show tray icon
            if (TrayService.IsSystemTrayAvailable())
            {
                _trayService.Show();
                _trayService.ShowNotification("StickyNotes-inator", 
                    "Application started! Right-click the tray icon to create notes.", 3000);
                _logger.LogInformation("Application started successfully");
            }
            else
            {
                _logger.LogError("System tray not available");
                MessageBox.Show("System tray not available. Application cannot start.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize main form");
            MessageBox.Show($"Failed to initialize application: {ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    /// Sets up the main form properties
    /// </summary>
    private void SetupForm()
    {
        // Hide the main form - it's just a coordinator
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Visible = false;

        // Set application properties
        Text = "StickyNotes-inator";
    }

    /// <summary>
    /// Sets up event handlers for tray service and application events
    /// </summary>
    private void SetupEventHandlers()
    {
        // Tray service events
        _trayService.CreateNoteRequested += OnCreateNoteRequested;
        _trayService.ShowAllNotesRequested += OnShowAllNotesRequested;
        _trayService.HideAllNotesRequested += OnHideAllNotesRequested;
        _trayService.DeleteAllNotesRequested += OnDeleteAllNotesRequested;
        _trayService.ExitRequested += OnExitRequested;

        // Application events
        Application.ApplicationExit += OnApplicationExit;
    }

    /// <summary>
    /// Loads previously saved notes from storage
    /// </summary>
    private void LoadSavedNotes()
    {
        try
        {
            var savedNotes = _storage.LoadAllNotes();
            
            foreach (var kvp in savedNotes)
            {
                try
                {
                    var noteForm = CreateNoteForm(kvp.Value);
                    if (noteForm != null)
                    {
                        _noteForms[kvp.Key] = noteForm;
                        if (kvp.Value.IsVisible)
                        {
                            noteForm.Show();
                        }
                        _logger.LogInformation("Loaded saved note: {NoteId}", kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load note: {NoteId}", kvp.Key);
                }
            }

            _logger.LogInformation("Loaded {Count} saved notes", _noteForms.Count);
            UpdateTrayTooltip();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved notes");
        }
    }

    /// <summary>
    /// Creates a new note form from note data
    /// </summary>
    /// <param name="note">The note data</param>
    /// <returns>StickyNoteForm instance or null if creation failed</returns>
    private StickyNoteForm? CreateNoteForm(Note note)
    {
        try
        {
            var noteForm = new StickyNoteForm(note, _storage, 
                _loggerFactory.CreateLogger<StickyNoteForm>());

            // Wire up events
            noteForm.NoteChanged += OnNoteChanged;
            noteForm.NoteClosed += OnNoteClosed;

            return noteForm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note form for note: {NoteId}", note.Id);
            return null;
        }
    }

    /// <summary>
    /// Handles create note request from tray
    /// </summary>
    private void OnCreateNoteRequested(object? sender, EventArgs e)
    {
        try
        {
            var noteId = Guid.NewGuid().ToString();
            var note = Note.Create(noteId);

            // Calculate position to avoid overlap
            note.X = CalculateNotePosition().X;
            note.Y = CalculateNotePosition().Y;

            var noteForm = CreateNoteForm(note);
            if (noteForm != null)
            {
                _noteForms[noteId] = noteForm;
                noteForm.Show();

                // Save the new note
                _storage.SaveNote(note);
                UpdateTrayTooltip();

                _logger.LogInformation("Created new note: {NoteId}", noteId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new note");
            MessageBox.Show($"Failed to create new note: {ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Calculates a position for a new note to avoid overlapping
    /// </summary>
    /// <returns>Point for the new note position</returns>
    private Point CalculateNotePosition()
    {
        var baseX = 100;
        var baseY = 100;
        var offset = 30;

        var x = baseX;
        var y = baseY;
        var attempts = 0;
        var maxAttempts = 20;

        while (attempts < maxAttempts)
        {
            var positionOccupied = false;

            foreach (var noteForm in _noteForms.Values)
            {
                var noteX = noteForm.Location.X;
                var noteY = noteForm.Location.Y;
                if (Math.Abs(x - noteX) < 50 && Math.Abs(y - noteY) < 50)
                {
                    positionOccupied = true;
                    break;
                }
            }

            if (!positionOccupied)
                break;

            x += offset;
            y += offset;
            attempts++;

            // Reset to base position if we've moved too far
            if (x > 800 || y > 600)
            {
                x = baseX;
                y = baseY;
            }
        }

        return new Point(x, y);
    }

    /// <summary>
    /// Handles show all notes request from tray
    /// </summary>
    private void OnShowAllNotesRequested(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Show all notes requested. Current note forms: {Count}", _noteForms.Count);
            
            // First, update all existing note forms' visibility status and save
            foreach (var kvp in _noteForms)
            {
                var noteForm = kvp.Value;
                var noteId = kvp.Key;
                
                _logger.LogDebug("Processing note form: {NoteId}, Current form visible: {FormVisible}", 
                    noteId, noteForm.Visible);
                
                // Update the note's visibility status first
                noteForm.UpdateVisibility(true);
                
                // Save the updated note
                var note = noteForm.GetNoteData();
                _storage.SaveNote(note);
                
                _logger.LogDebug("Note {NoteId} - IsVisible: {IsVisible}, Form Visible: {FormVisible}", 
                    noteId, note.IsVisible, noteForm.Visible);
                
                // Then show the form
                noteForm.Show();
                
                _logger.LogDebug("Showed existing note: {NoteId}", noteId);
            }

            // Then, load any saved notes that aren't currently in the collection
            var savedNotes = _storage.LoadAllNotes();
            _logger.LogInformation("Found {SavedCount} saved notes in storage", savedNotes.Count);
            
            foreach (var kvp in savedNotes)
            {
                var noteId = kvp.Key;
                var note = kvp.Value;
                
                if (!_noteForms.ContainsKey(noteId))
                {
                    _logger.LogDebug("Creating new form for saved note: {NoteId}, IsVisible: {IsVisible}", 
                        noteId, note.IsVisible);
                    
                    // Mark as visible and save
                    note.IsVisible = true;
                    _storage.SaveNote(note);
                    
                    // Create new form
                    var noteForm = CreateNoteForm(note);
                    if (noteForm != null)
                    {
                        _noteForms[noteId] = noteForm;
                        noteForm.Show();
                        _logger.LogInformation("Created and showed saved note: {NoteId}", noteId);
                    }
                }
            }

            _logger.LogInformation("All notes shown. Total note forms: {Count}", _noteForms.Count);
            UpdateTrayTooltip();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show all notes");
        }
    }

    /// <summary>
    /// Handles hide all notes request from tray
    /// </summary>
    private void OnHideAllNotesRequested(object? sender, EventArgs e)
    {
        try
        {
            foreach (var kvp in _noteForms)
            {
                var noteForm = kvp.Value;
                
                // Hide the form first
                noteForm.Hide();
                
                // Update the note's visibility status
                noteForm.UpdateVisibility(false);
                
                // Save the updated note
                var note = noteForm.GetNoteData();
                _storage.SaveNote(note);
            }
            _logger.LogInformation("All notes hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide all notes");
        }
    }

    /// <summary>
    /// Handles delete all notes request from tray
    /// </summary>
    private void OnDeleteAllNotesRequested(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Delete all notes requested");
            _storage.ClearAllNotes();
            _noteForms.Clear();
            UpdateTrayTooltip();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all notes");
        }
    }

    /// <summary>
    /// Handles exit request from tray
    /// </summary>
    private void OnExitRequested(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Exit requested");
            Application.Exit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during exit");
        }
    }

    /// <summary>
    /// Handles note content changes
    /// </summary>
    private void OnNoteChanged(object? sender, Note note)
    {
        try
        {
            _storage.SaveNote(note);
            UpdateTrayTooltip();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle note change for note: {NoteId}", note.Id);
        }
    }

    /// <summary>
    /// Handles note closure
    /// </summary>
    private void OnNoteClosed(object? sender, string noteId)
    {
        try
        {
            if (_noteForms.TryGetValue(noteId, out var noteForm))
            {
                // Update the note's visibility status before removing the form
                noteForm.UpdateVisibility(false);
                
                // Save the updated note
                var note = noteForm.GetNoteData();
                _storage.SaveNote(note);
                
                // Remove the form from the dictionary since it's disposed
                _noteForms.Remove(noteId);
                
                UpdateTrayTooltip();
                _logger.LogInformation("Note closed and removed from dictionary: {NoteId}", noteId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle note closure for note: {NoteId}", noteId);
        }
    }

    /// <summary>
    /// Handles application exit
    /// </summary>
    private void OnApplicationExit(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Application shutting down");

            // Save all notes before exiting
            foreach (var kvp in _noteForms)
            {
                try
                {
                    var note = kvp.Value.GetNoteData();
                    _storage.SaveNote(note);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save note during shutdown: {NoteId}", kvp.Key);
                }
            }

            // Clean up tray service
            _trayService?.Dispose();

            _logger.LogInformation("Application shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application shutdown");
        }
    }

    /// <summary>
    /// Updates the tray icon tooltip with current note count
    /// </summary>
    private void UpdateTrayTooltip()
    {
        try
        {
            var count = _noteForms.Count;
            var visibleCount = _noteForms.Values.Count(nf => nf.Visible);
            var tooltip = count == 1 
                ? "StickyNotes-inator (1 note)" 
                : $"StickyNotes-inator ({count} notes, {visibleCount} visible)";
            _trayService.UpdateTooltip(tooltip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tray tooltip");
        }
    }

    /// <summary>
    /// Gets the current number of active notes
    /// </summary>
    /// <returns>Number of active notes</returns>
    public int GetNoteCount()
    {
        return _noteForms.Count;
    }

    /// <summary>
    /// Disposes of the main form and cleans up resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Dispose all note forms
                foreach (var noteForm in _noteForms.Values)
                {
                    noteForm?.Dispose();
                }
                _noteForms.Clear();

                // Dispose services
                _trayService?.Dispose();

                _logger.LogInformation("Main form disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing main form");
            }
        }
        base.Dispose(disposing);
    }
} 