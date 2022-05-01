﻿namespace Vernuntii.PluginSystem
{
    /// <summary>
    /// Represents a plugin registry.
    /// </summary>
    public interface IPluginRegistry : IPluginRegistrationProducer
    {
        /// <summary>
        /// Collection consisting of plugin registrations.
        /// </summary>
        IReadOnlyCollection<IPluginRegistration> PluginRegistrations { get; }

        /// <summary>
        /// Registers a plugin.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="plugin"></param>
        ValueTask<IPluginRegistration> RegisterAsync(Type serviceType, IPlugin plugin);

        /// <summary>
        /// The registered plugin.
        /// </summary>
        ILazyPlugin<T> First<T>() where T : IPlugin;
    }
}
