using System;
using System.IO;
using System.Text.RegularExpressions;
using BaixALL.App.Infrastructure;

namespace BaixALL.App.Services;

public class LoggerService : ILoggerService
{
    private readonly string _logsDirectory;
    private readonly object _lock = new();

    public LoggerService(string? logsDirectory = null)
    {
        _logsDirectory = logsDirectory ?? AppConstants.LogsFolder;
        try
        {
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
            RotateOldLogs();
        }
        catch
        {
            // Evita crash caso haja erro na criação de diretório
        }
    }

    public void Info(string message) => Log("INFO", message);

    public void Warning(string message) => Log("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var full = exception != null ? $"{message} | Exceção: {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}" : message;
        Log("ERROR", full);
    }

    public void Debug(string message)
    {
#if DEBUG
        Log("DEBUG", message);
#endif
    }

    public string GetLogFolderPath() => _logsDirectory;

    private void Log(string level, string message)
    {
        try
        {
            // Remove quaisquer potenciais tokens, senhas ou cookies sensíveis
            var sanitizedMessage = SanitizeLogMessage(message);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] [{level}] {sanitizedMessage}{Environment.NewLine}";

            var fileName = $"baixall_{DateTime.Now:yyyyMMdd}.log";
            var filePath = Path.Combine(_logsDirectory, fileName);

            lock (_lock)
            {
                File.AppendAllText(filePath, line);
            }
        }
        catch
        {
            // Silencioso em caso de falha de IO para nunca travar a aplicação
        }
    }

    private static string SanitizeLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        // Limpa possíveis referências a senhas, cookies, auth tokens
        var sanitized = Regex.Replace(message, @"(?i)(password|passwd|pwd|token|cookie|secret|auth)\s*[:=]\s*([^\s,;]+)", "$1=[REDACTED]");
        return sanitized;
    }

    private void RotateOldLogs()
    {
        try
        {
            var files = Directory.GetFiles(_logsDirectory, "baixall_*.log");
            var cutoff = DateTime.Now.AddDays(-14);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
}
