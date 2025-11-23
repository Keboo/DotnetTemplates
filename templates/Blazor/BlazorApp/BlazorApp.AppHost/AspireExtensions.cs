
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorApp.AppHost;

internal static class AspireExtensions
{
    public static ILogger GetResourceLogger<T>(this ExecuteCommandContext context, IResourceBuilder<T> resourceBuilder)
        where T : IResource
    {
        return GetResourceLogger(context.ServiceProvider, resourceBuilder.Resource);
    }

    public static ILogger GetResourceLogger(this IServiceProvider serviceProvider, IResource resource)
    {
        var loggerService = serviceProvider.GetRequiredService<ResourceLoggerService>();
        return loggerService.GetLogger(resource);
    }

    public static async Task<bool> ExecuteProcessAsync(this IResource resource, IServiceProvider serviceProvider, ProcessStartInfo processInfo, CancellationToken cancellationToken = default)
    {
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;

        if (Process.Start(processInfo) is { } process)
        {
            var logger = serviceProvider.GetResourceLogger(resource);
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

            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 && logger.IsEnabled(LogLevel.Error))
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

    public static Task<bool> ExecuteProcessAsync<T>(this IResourceBuilder<T> resourceBuilder, ExecuteCommandContext context, ProcessStartInfo processInfo)
        where T : IResource
    {
        return resourceBuilder.Resource.ExecuteProcessAsync(context.ServiceProvider, processInfo, context.CancellationToken);
    }

}
