namespace MonoGameEngine.Loggers;

public class DebugLogger : ILogger
{
    public void Log(string message)
    {
        Log(message, ILogger.LogLevel.Debug);
    }

    public void Log(string message, ILogger.LogLevel logLevel = ILogger.LogLevel.Debug)
    {
        switch (logLevel)
        {
            case ILogger.LogLevel.Debug:
            case ILogger.LogLevel.Info:
            case ILogger.LogLevel.Warning:
            case ILogger.LogLevel.Error:
            default:
                System.Diagnostics.Debug.WriteLine(message);
                break;
        }
    }
}
