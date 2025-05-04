using System;
using System.Diagnostics;
using Godot;

static class DurationLogger
{
    public record class DurationLoggerHelper(Action DisposeAction) : IDisposable
    {
        public static DurationLoggerHelper Empty { get; } = new DurationLoggerHelper(() => { });

        public void Dispose()
        {
            DisposeAction?.Invoke();
            GC.SuppressFinalize(this);
        }
    }

    public static IDisposable LogDuration(string actionName, bool onlyDuringDebug = false)
    {
#if !DEBUG
        if (onlyDuringDebug) return DurationLoggerHelper.Empty;
#endif

        var sw = Stopwatch.StartNew();
        GD.Print($"Starting {(char.IsUpper(actionName[0]) ? $"{char.ToLower(actionName[0])}{actionName.AsSpan(1)}" : actionName)}...");
        return new DurationLoggerHelper(() => 
            GD.Print($"{(char.IsLower(actionName[0]) ? $"{char.ToUpper(actionName[0])}{actionName.AsSpan(1)}" : actionName)} finished, took {sw.Elapsed.TotalSeconds}s"));
    }
}