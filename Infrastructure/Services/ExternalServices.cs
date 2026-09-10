using System.Diagnostics;

namespace Infrastructure.Services;

public static class ExternalServices
{
    public static bool IsInstalled(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where.exe" : "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process!.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Execute(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process!.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    $"Command {command} failed with exit code {process.ExitCode} and output {process.StandardError}");
            }
        }
        catch
        {
            throw new Exception($"Command {command} failed");
        }
    }
}