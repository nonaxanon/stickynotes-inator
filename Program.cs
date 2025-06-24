using StickyNotesInator.Forms;
using Microsoft.Extensions.Logging;

namespace StickyNotesInator;

/// <summary>
/// Main entry point for the StickyNotes-inator application.
/// Sets up logging, handles application lifecycle, and manages the main form.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            // Enable Windows Forms visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Setup logging
            var loggerFactory = CreateLoggerFactory();

            // Create and run the main form
            using var mainForm = new MainForm(loggerFactory);
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            // Log the error and show a message box
            var errorMessage = $"Fatal error: {ex.Message}";
            MessageBox.Show(errorMessage, "StickyNotes-inator Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            // Try to log the error if possible
            try
            {
                File.AppendAllText("error.log", 
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMessage}\n{ex}\n\n");
            }
            catch
            {
                // If we can't even log, just continue
            }
        }
    }

    /// <summary>
    /// Creates and configures the logger factory
    /// </summary>
    /// <returns>Configured ILoggerFactory instance</returns>
    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            // Add console logging
            builder.AddConsole();

            // Add file logging
            builder.AddProvider(new FileLoggerProvider("stickynotes.log"));

            // Set minimum log level
            builder.SetMinimumLevel(LogLevel.Information);

            // Filter out some noisy logs
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        });
    }
}

/// <summary>
/// Simple file logger provider for writing logs to a file
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_filePath);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Simple file logger implementation
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lockObject = new object();

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var message = formatter(state, exception);
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}";
            }

            lock (_lockObject)
            {
                File.AppendAllText(_filePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // If we can't log, just continue
        }
    }
} 