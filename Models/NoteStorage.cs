using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StickyNotesInator.Models;

/// <summary>
/// Handles persistent storage of sticky notes using JSON files.
/// Provides thread-safe operations and comprehensive error handling.
/// </summary>
public class NoteStorage
{
    private readonly string _dataDirectory;
    private readonly string _notesFilePath;
    private readonly object _lockObject = new object();
    private readonly ILogger<NoteStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the NoteStorage class
    /// </summary>
    /// <param name="dataDirectory">Directory to store note data files</param>
    /// <param name="logger">Logger instance for error tracking</param>
    public NoteStorage(string dataDirectory = "data", ILogger<NoteStorage>? logger = null)
    {
        _dataDirectory = dataDirectory;
        _notesFilePath = Path.Combine(_dataDirectory, "notes.json");
        _logger = logger ?? new NullLogger<NoteStorage>();
        
        EnsureDataDirectoryExists();
    }

    /// <summary>
    /// Creates the data directory if it doesn't exist
    /// </summary>
    private void EnsureDataDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
                _logger.LogInformation("Created data directory: {DataDirectory}", _dataDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data directory: {DataDirectory}", _dataDirectory);
            throw new InvalidOperationException($"Cannot create data directory: {_dataDirectory}", ex);
        }
    }

    /// <summary>
    /// Saves a note to storage
    /// </summary>
    /// <param name="note">The note to save</param>
    /// <returns>True if save was successful, false otherwise</returns>
    public bool SaveNote(Note note)
    {
        if (note == null)
        {
            _logger.LogWarning("Attempted to save null note");
            return false;
        }

        if (!note.IsValid())
        {
            _logger.LogWarning("Attempted to save invalid note: {NoteId}", note.Id);
            return false;
        }

        lock (_lockObject)
        {
            try
            {
                var notes = LoadAllNotesInternal();
                notes[note.Id] = note;

                var json = JsonConvert.SerializeObject(notes, Formatting.Indented);
                File.WriteAllText(_notesFilePath, json, Encoding.UTF8);

                _logger.LogInformation("Note saved successfully: {NoteId}", note.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save note: {NoteId}", note.Id);
                return false;
            }
        }
    }

    /// <summary>
    /// Loads all saved notes from storage
    /// </summary>
    /// <returns>Dictionary of note ID to Note object</returns>
    public Dictionary<string, Note> LoadAllNotes()
    {
        lock (_lockObject)
        {
            return LoadAllNotesInternal();
        }
    }

    /// <summary>
    /// Internal method to load notes from file (not thread-safe)
    /// </summary>
    /// <returns>Dictionary of note ID to Note object</returns>
    private Dictionary<string, Note> LoadAllNotesInternal()
    {
        if (!File.Exists(_notesFilePath))
        {
            _logger.LogInformation("No existing notes file found, starting fresh");
            return new Dictionary<string, Note>();
        }

        try
        {
            var json = File.ReadAllText(_notesFilePath, Encoding.UTF8);
            var notes = JsonConvert.DeserializeObject<Dictionary<string, Note>>(json) 
                       ?? new Dictionary<string, Note>();

            // Validate and filter notes
            var validNotes = new Dictionary<string, Note>();
            foreach (var kvp in notes)
            {
                if (kvp.Value?.IsValid() == true)
                {
                    validNotes[kvp.Key] = kvp.Value;
                }
                else
                {
                    _logger.LogWarning("Skipping invalid note data for: {NoteId}", kvp.Key);
                }
            }

            _logger.LogInformation("Loaded {Count} valid notes from storage", validNotes.Count);
            return validNotes;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in notes file");
            return new Dictionary<string, Note>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read notes file");
            return new Dictionary<string, Note>();
        }
    }

    /// <summary>
    /// Deletes a note from storage
    /// </summary>
    /// <param name="noteId">ID of the note to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public bool DeleteNote(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
        {
            _logger.LogWarning("Attempted to delete note with null or empty ID");
            return false;
        }

        lock (_lockObject)
        {
            try
            {
                var notes = LoadAllNotesInternal();
                if (notes.Remove(noteId))
                {
                    var json = JsonConvert.SerializeObject(notes, Formatting.Indented);
                    File.WriteAllText(_notesFilePath, json, Encoding.UTF8);

                    _logger.LogInformation("Note deleted successfully: {NoteId}", noteId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Note not found for deletion: {NoteId}", noteId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete note: {NoteId}", noteId);
                return false;
            }
        }
    }

    /// <summary>
    /// Clears all saved notes
    /// </summary>
    /// <returns>True if operation was successful, false otherwise</returns>
    public bool ClearAllNotes()
    {
        lock (_lockObject)
        {
            try
            {
                if (File.Exists(_notesFilePath))
                {
                    File.Delete(_notesFilePath);
                }
                _logger.LogInformation("All notes cleared successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear notes");
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the total number of saved notes
    /// </summary>
    /// <returns>Number of saved notes</returns>
    public int GetNoteCount()
    {
        lock (_lockObject)
        {
            return LoadAllNotesInternal().Count;
        }
    }

    /// <summary>
    /// Checks if a note exists in storage
    /// </summary>
    /// <param name="noteId">ID of the note to check</param>
    /// <returns>True if the note exists, false otherwise</returns>
    public bool NoteExists(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return false;

        lock (_lockObject)
        {
            var notes = LoadAllNotesInternal();
            return notes.ContainsKey(noteId);
        }
    }

    /// <summary>
    /// Creates a backup of the notes file
    /// </summary>
    /// <param name="backupPath">Path for the backup file</param>
    /// <returns>True if backup was successful, false otherwise</returns>
    public bool CreateBackup(string backupPath)
    {
        if (!File.Exists(_notesFilePath))
        {
            _logger.LogWarning("No notes file to backup");
            return false;
        }

        try
        {
            File.Copy(_notesFilePath, backupPath, true);
            _logger.LogInformation("Backup created successfully: {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup: {BackupPath}", backupPath);
            return false;
        }
    }
} 