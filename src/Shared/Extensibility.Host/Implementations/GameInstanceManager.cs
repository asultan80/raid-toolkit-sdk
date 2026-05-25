using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Raid.Toolkit.Extensibility.Host
{
    public class GameInstanceManager : IGameInstanceManager
    {
        private readonly ConcurrentDictionary<int, IGameInstance> _Instances = new();
        private readonly ConcurrentDictionary<int, IGameInstance> _RawInstances = new();
        private readonly IServiceProvider ServiceProvider;
        private readonly ILogger<GameInstanceManager> Logger;
        private bool HasCheckedStaticData;

        public IReadOnlyList<IGameInstance> Instances => _Instances.Values.ToList();
        public event EventHandler<IGameInstanceManager.GameInstancesUpdatedEventArgs> OnAdded;
        public event EventHandler<IGameInstanceManager.GameInstancesUpdatedEventArgs> OnRemoved;

        public GameInstanceManager(
            IServiceProvider serviceProvider,
            IHostApplicationLifetime lifetime,
            ILogger<GameInstanceManager> logger)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            _ = lifetime.ApplicationStopped.Register(() =>
            {
                int[] instanceKeys = _RawInstances.Keys.ToArray();
                foreach (int key in instanceKeys)
                {
                    _ = _RawInstances.Remove(key, out IGameInstance instance);
                    _ = _Instances.Remove(key, out _);
                    instance.Dispose();
                }
            });
        }

        public IGameInstance GetById(string id)
        {
            return Instances.FirstOrDefault(instance => instance.Id == id);
        }

        public bool TryGetById(string id, out IGameInstance? instance)
        {
            instance = Instances.FirstOrDefault(instance => instance.Id == id);
            return instance != null;
        }


        public void AddInstance(Process process)
        {
            Logger.LogInformation("GameInstanceManager.AddInstance: pid={pid}", process.Id);
            IGameInstance instance = _RawInstances.GetOrAdd(process.Id, (token) => ActivatorUtilities.CreateInstance<GameInstance>(ServiceProvider, process));
            try
            {
                instance.InitializeOrThrow(process);
                Logger.LogInformation("GameInstanceManager.AddInstance: InitializeOrThrow succeeded, id={id}", instance.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GameInstanceManager.AddInstance: InitializeOrThrow failed for pid={pid}", process.Id);
                throw;
            }

            _ = _Instances.TryAdd(instance.Token, instance);
            Logger.LogInformation("GameInstanceManager.AddInstance: firing OnAdded for id={id}", instance.Id);
            try
            {
                OnAdded?.Raise(this, new(instance));
                Logger.LogInformation("GameInstanceManager.AddInstance: OnAdded completed for id={id}", instance.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GameInstanceManager.AddInstance: OnAdded threw for id={id}", instance.Id);
            }
        }

        public void RemoveInstance(int token)
        {
            if (_RawInstances.TryRemove(token, out IGameInstance instance))
            {
                _ = _Instances.TryRemove(token, out _);
                OnRemoved?.Raise(this, new(instance));
                instance.Dispose();
            }
        }
    }
}
