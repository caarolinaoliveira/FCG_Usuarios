using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace FCG.Presentation.Logging
{
    public sealed class CompactConsoleFormatter : ConsoleFormatter
    {
        public CompactConsoleFormatter() : base("compact") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
                return;

            message = FirstLine(message);
            var traceId = Activity.Current?.TraceId.ToHexString() ?? "-";
            var category = logEntry.Category[(logEntry.Category.LastIndexOf('.') + 1)..];

            textWriter.Write($"{DateTime.UtcNow:HH:mm:ss} {Level(logEntry.LogLevel)} [{traceId}] {category}: {message}");
            if (logEntry.Exception is not null)
                textWriter.Write($" | {logEntry.Exception.GetType().Name}: {logEntry.Exception.Message}");
            textWriter.Write(Environment.NewLine);
        }

        private static string FirstLine(string message)
        {
            var newlineIndex = message.IndexOf('\n');
            return newlineIndex < 0 ? message : message[..newlineIndex].TrimEnd('\r') + " […]";
        }

        private static string Level(LogLevel level) => level switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none"
        };
    }
}
