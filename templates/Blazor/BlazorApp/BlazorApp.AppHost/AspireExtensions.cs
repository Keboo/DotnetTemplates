
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorApp.AppHost;

internal static class AspireExtensions
{
    public static ILogger GetResourceLogger<T>(this ExecuteCommandContext context, IResourceBuilder<T> resourceBuilder)
        where T : IResource
    {
        var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        return loggerService.GetLogger(resourceBuilder.Resource);
    }

    public static async Task<bool> ExecuteProcessAsync<T>(this ExecuteCommandContext context, IResourceBuilder<T> resourceBuilder, ProcessStartInfo processInfo)
        where T : IResource
    {
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;

        if (Process.Start(processInfo) is { } process)
        {
            var logger = context.GetResourceLogger(resourceBuilder);
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.LogInformation(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.LogError(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(context.CancellationToken);
            if (process.ExitCode != 0)
            {
                logger.LogError("{ProcessName} process exited with code {ExitCode}", Path.GetFileName(processInfo.FileName), process.ExitCode);
                return false;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}
