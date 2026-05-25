module GrokWriter.Logging

open Serilog

/// Initialize Serilog: console + rolling file in logs/ directory
let init () =
    Log.Logger <- LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(
            outputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path = "logs/grok-writer-.log",
            rollingInterval = RollingInterval.Day,
            outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger()

    Log.Information("Grok Writer logging initialized")
    Log.Information("Log files: logs/grok-writer-{{Date}}.log")