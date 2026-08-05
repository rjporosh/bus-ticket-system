using System.IO.Compression;
using System.Text;

namespace BusTicketing.Infrastructure.Services;

public interface ICustomLogger
{
    Task LogBuildErrorAsync(string message, Exception? ex = null);
    Task LogRuntimeErrorAsync(string message, Exception? ex = null);
    Task LogQueryAsync(string controller, string method, string query, long executionTimeMs);
    void EnsureLogDirectoriesExist();
}

public class CustomLogger : ICustomLogger, IDisposable
{
    private readonly string _logsRootPath;
    private readonly string _buildErrorPath;
    private readonly string _runtimeErrorPath;
    private readonly string _queryLogsPath;
    private readonly object _lockObject = new();
    private bool _disposed;

    public CustomLogger(string logsRootPath = "logs")
    {
        _logsRootPath = Path.GetFullPath(logsRootPath);
        _buildErrorPath = Path.Combine(_logsRootPath, "build-error");
        _runtimeErrorPath = Path.Combine(_logsRootPath, "run-time-error");
        _queryLogsPath = Path.Combine(_logsRootPath, "query-logs");

        EnsureLogDirectoriesExist();
    }

    public void EnsureLogDirectoriesExist()
    {
        Directory.CreateDirectory(_buildErrorPath);
        Directory.CreateDirectory(_runtimeErrorPath);
        Directory.CreateDirectory(_queryLogsPath);
    }

    public async Task LogBuildErrorAsync(string message, Exception? ex = null)
    {
        var logEntry = FormatLogEntry("BUILD ERROR", message, ex);
        var filePath = GetDailyLogPath(_buildErrorPath, "build-error");
        await AppendToFileAsync(filePath, logEntry);
    }

    public async Task LogRuntimeErrorAsync(string message, Exception? ex = null)
    {
        var logEntry = FormatLogEntry("RUNTIME ERROR", message, ex);
        var filePath = GetDailyLogPath(_runtimeErrorPath, "run-time-error");
        await AppendToFileAsync(filePath, logEntry);
    }

    public async Task LogQueryAsync(string controller, string method, string query, long executionTimeMs)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] QUERY LOG\n" +
                       $"Controller: {controller}\n" +
                       $"Method: {method}\n" +
                       $"Query: {query}\n" +
                       $"Execution Time: {executionTimeMs}ms\n" +
                       new string('-', 80) + "\n";

        var filePath = GetDailyLogPath(_queryLogsPath, "query-logs");
        await AppendToFileAsync(filePath, logEntry);
    }

    private string FormatLogEntry(string type, string message, Exception? ex)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var sb = new StringBuilder();
        sb.AppendLine($"[{timestamp}] {type}");
        sb.AppendLine($"Message: {message}");

        if (ex != null)
        {
            sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
            sb.AppendLine($"Exception Message: {ex.Message}");
            sb.AppendLine($"StackTrace:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                sb.AppendLine($"Inner StackTrace:\n{ex.InnerException.StackTrace}");
            }
        }

        sb.AppendLine(new string('=', 80));
        return sb.ToString();
    }

    private string GetDailyLogPath(string directory, string prefix)
    {
        var date = DateTime.UtcNow.ToString("dd-MM-yy");
        var fileName = $"{prefix}-{date}.txt";
        return Path.Combine(directory, fileName);
    }

    private async Task AppendToFileAsync(string filePath, string content)
    {
        lock (_lockObject)
        {
            File.AppendAllText(filePath, content);
        }
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}