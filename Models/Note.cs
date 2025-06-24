using System.ComponentModel.DataAnnotations;

namespace StickyNotesInator.Models;

/// <summary>
/// Represents a sticky note with all its properties and metadata.
/// This class is designed to be serializable and includes validation.
/// </summary>
public class Note
{
    /// <summary>
    /// Unique identifier for the note
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the note
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate of the note's position on screen
    /// </summary>
    [Range(0, 10000)]
    public int X { get; set; } = 100;

    /// <summary>
    /// Y coordinate of the note's position on screen
    /// </summary>
    [Range(0, 10000)]
    public int Y { get; set; } = 100;

    /// <summary>
    /// Width of the note in pixels
    /// </summary>
    [Range(100, 1000)]
    public int Width { get; set; } = 200;

    /// <summary>
    /// Height of the note in pixels
    /// </summary>
    [Range(100, 800)]
    public int Height { get; set; } = 150;

    /// <summary>
    /// When the note was created (ISO 8601 format)
    /// </summary>
    [Required]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// When the note was last modified (ISO 8601 format)
    /// </summary>
    [Required]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Whether the note is currently visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Creates a new note with default values and generated timestamps
    /// </summary>
    /// <param name="id">Unique identifier for the note</param>
    /// <returns>A new Note instance</returns>
    public static Note Create(string id)
    {
        var now = DateTime.UtcNow.ToString("O");
        return new Note
        {
            Id = id,
            Content = string.Empty,
            X = 100,
            Y = 100,
            Width = 200,
            Height = 150,
            CreatedAt = now,
            UpdatedAt = now,
            IsVisible = true
        };
    }

    /// <summary>
    /// Updates the note's modification timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow.ToString("O");
    }

    /// <summary>
    /// Validates the note data
    /// </summary>
    /// <returns>True if the note is valid, false otherwise</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Id))
            return false;

        if (string.IsNullOrWhiteSpace(CreatedAt) || string.IsNullOrWhiteSpace(UpdatedAt))
            return false;

        if (Width < 100 || Height < 100)
            return false;

        if (X < 0 || Y < 0)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a copy of this note with a new ID
    /// </summary>
    /// <param name="newId">The new ID for the copied note</param>
    /// <returns>A new Note instance with copied data</returns>
    public Note Clone(string newId)
    {
        var now = DateTime.UtcNow.ToString("O");
        return new Note
        {
            Id = newId,
            Content = this.Content,
            X = this.X + 30, // Offset slightly to avoid overlap
            Y = this.Y + 30,
            Width = this.Width,
            Height = this.Height,
            CreatedAt = now,
            UpdatedAt = now,
            IsVisible = this.IsVisible
        };
    }
} 