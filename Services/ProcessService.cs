namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    /// <summary>Executa processos de forma observável, cancelável e com timeout.</summary>
    public sealed class ProcessService
    {
        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) =>
            RunAsync(fileName, SplitArguments(arguments), workingDirectory, timeout, cancellationToken);

        public async Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IEnumerable<string>? arguments = null,
            string? workingDirectory = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments ?? Array.Empty<string>())
                startInfo.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = startInfo };
            try
            {
                if (!process.Start())
                    return new ProcessExecutionResult { Exception = new InvalidOperationException("O processo não foi iniciado.") };

                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                using var timeoutSource = timeout.HasValue
                    ? new CancellationTokenSource(timeout.Value)
                    : new CancellationTokenSource();
                using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

                try
                {
                    await process.WaitForExitAsync(linkedSource.Token);
                }
                catch (OperationCanceledException)
                {
                    TryKill(process);
                    await process.WaitForExitAsync(CancellationToken.None);
                    return new ProcessExecutionResult
                    {
                        StandardOutput = await outputTask,
                        StandardError = await errorTask,
                        TimedOut = timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested,
                        WasCancelled = cancellationToken.IsCancellationRequested
                    };
                }

                return new ProcessExecutionResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = await outputTask,
                    StandardError = await errorTask
                };
            }
            catch (Exception ex)
            {
                TryKill(process);
                return new ProcessExecutionResult { Exception = ex };
            }
        }

        public Process? StartElevated(string fileName, IEnumerable<string>? arguments = null, string? workingDirectory = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory ?? string.Empty,
                Verb = "runas",
                UseShellExecute = true
            };
            foreach (var argument in arguments ?? Array.Empty<string>())
                startInfo.ArgumentList.Add(argument);
            return Process.Start(startInfo);
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }

        internal static IReadOnlyList<string> SplitArguments(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return Array.Empty<string>();

            var pointer = CommandLineToArgvW(commandLine, out var argumentCount);
            if (pointer == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                var arguments = new string[argumentCount];
                for (var index = 0; index < argumentCount; index++)
                {
                    var argumentPointer = Marshal.ReadIntPtr(pointer, index * IntPtr.Size);
                    arguments[index] = Marshal.PtrToStringUni(argumentPointer) ?? string.Empty;
                }
                return arguments;
            }
            finally
            {
                LocalFree(pointer);
            }
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
            out int argumentCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);
    }
}
