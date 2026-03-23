namespace MonoGameEngine.Utils.Loggers;

public interface ILogger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    void Log(string message);
    void Log(string message, LogLevel logLevel);
}