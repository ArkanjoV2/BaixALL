using System;

namespace BaixALL.App.Services;

public interface ILoggerService
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Debug(string message);
    string GetLogFolderPath();
}
