using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Raid.Toolkit.Application.Core.Commands.Base;
using Raid.Toolkit.Application.Core.Commands.Matchers;
using Raid.Toolkit.Application.Core.DependencyInjection;
using Raid.Toolkit.Application.Core.Host;
using Raid.Toolkit.Common;
using Raid.Toolkit.DataModel;
using Raid.Toolkit.Extensibility;
using Raid.Toolkit.Extensibility.Host;

namespace Raid.Toolkit.Application.Core.Commands.Tasks
{
    internal class RunTask : ICommandTask
    {
        private readonly IProgramHost ProgramHost;
        private readonly IAppHostBuilder AppHostBuilder;
        private readonly RunOptions Options;

        public RunTask(IProgramHost programHost, IAppHostBuilder appHostBuilder, RunOptions options)
        {
            ProgramHost = programHost;
            AppHostBuilder = appHostBuilder;
            Options = options;
        }

        private static async Task<int?> TryActivateCurrentProcess()
        {
            try
            {
                RaidToolkitClientBase client = new();
                client.Connect();
                return await client.MakeApi<ActivationApi>().Activate(new Uri("rtk://default"), Array.Empty<string>());
            }
            catch
            {
                // No instance is actually listening — stale singleton handle; fall through to start normally
                return null;
            }
        }

        public async Task<int> Invoke()
        {
            if (!Options.Standalone)
            {
                if (Options.Wait.HasValue)
                {
                    await SingletonProcess.TryAcquireSingletonWithTimeout(Options.Wait.Value);
                }
                else
                {
                    // already running?
                    if (!SingletonProcess.TryAcquireSingleton())
                    {
                        int? result = await TryActivateCurrentProcess();
                        if (result.HasValue)
                            return result.Value;
                        // stale/broken instance — kill it and acquire the singleton
                        SingletonProcess.KillStaleInstances();
                        await Task.Delay(500);
                        SingletonProcess.TryAcquireSingleton();
                    }
                }
            }

            HostFeatures hostFeatures = HostFeatures.ProcessWatcher;
            if (!Options.Debug)
                hostFeatures |= HostFeatures.AutoUpdate;

            var builder = AppHostBuilder
                .AddExtensibility()
                .AddLogging()
                .AddAppServices(hostFeatures)
                .AddUI();
            if (!Options.NoWebService)
                builder = builder.AddWebSockets(AppHost.HandleMessage);

            _ = AppHostBuilder.ConfigureServices(services => ProgramHost.ConfigureServices(services));

            IHost host = AppHostBuilder.Build();
            ConfigureHost(host);

            AppHost.Start(host);

            // must allow AppUI to initialize any process hooks before
            // the synchronization context is requested

            INotificationManager? notificationManager = host.Services.GetService<INotificationManager>();
            notificationManager?.Initialize();

            await ProgramHost.Start(host, () =>
            {
                _ = Task.Run(() =>
                {
                    _ = host.StartAsync();
                });
            });

            return 0;
        }

        private void ConfigureHost(IHost host)
        {
            IModelLoader modelLoader = host.Services.GetRequiredService<IModelLoader>();

            if (Options.DebugPackage == ".")
            {
                Options.DebugPackage = Environment.GetEnvironmentVariable("DEBUG_PACKAGE_DIR") ?? ".";
            }
            PackageManager.DebugPackage = Options.DebugPackage;
            PackageManager.NoDefaultPackages = Options.NoDefaultPackages;
            if (!string.IsNullOrEmpty(PackageManager.DebugPackage))
            {
                Options.Debug = CommonOptions.Value.Debug = true;
                string debugInteropDirectory = Path.Combine(PackageManager.DebugPackage, @"temp~interop");
                modelLoader.OutputDirectory = debugInteropDirectory;
            }
            if (!string.IsNullOrEmpty(Options.InteropDirectory))
            {
                modelLoader.OutputDirectory = Options.InteropDirectory;
            }
        }
    }
}
