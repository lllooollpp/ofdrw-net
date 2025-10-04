using System;
using System.Diagnostics;

namespace OfdrwNet.Reader.Diagnostics;

/// <summary>
/// Lightweight reader logger used inside OfdrwNet.Reader project.
/// Respects global OfdrwConfiguration.EnableLogging and LogLevel.
/// Uses System.Diagnostics.Trace so Program's SerilogTraceListener captures output.
/// </summary>
internal static class ReaderLogger
{
    // Default: minimal logging (core only) disabled. Call SetEnabled(true) to enable core logs.
    private static bool _enabled = false;
    // Verbose logs must be explicitly enabled
    private static bool _enabledVerbose = false;

    public static void SetEnabled(bool enabled, bool verbose = false)
    {
        _enabled = enabled;
        _enabledVerbose = verbose;
    }

    public static bool IsEnabled => _enabled;

    public static void InfoCore(string message)
    {
        if (!_enabled) return;
        // core info should be visible at Information level or lower verbosity
        Trace.TraceInformation($"[Reader] {message}");
    }

    public static void InfoVerbose(string message)
    {
        if (!_enabled || !_enabledVerbose) return;
        // verbose info only when Debug/Trace enabled
        Trace.TraceInformation($"[Reader][VERBOSE] {message}");
    }

    public static void Warn(string message)
    {
        if (!_enabled) return;
        Trace.TraceWarning($"[Reader] {message}");
    }

    public static void Error(string message)
    {
        if (!_enabled) return;
        Trace.TraceError($"[Reader] {message}");
    }

    public static void Error(string message, Exception ex)
    {
        if (!_enabled) return;
        Trace.TraceError($"[Reader] {message} - {ex}");
    }
}
