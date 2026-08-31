using System;

namespace Highbrow.Core
{
    /// <summary>
    /// Lifecycle interface for all Highbrow SDK modules.
    /// Provides decoupled initialization and shutdown capabilities.
    /// </summary>
    public interface IHighbrowModule
    {
        /// <summary>
        /// Unique name identifier for the module.
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Gets whether the module has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the module with the provided SDK configuration.
        /// </summary>
        /// <param name="config">Global SDK configuration.</param>
        void Initialize(HighbrowConfig config);

        /// <summary>
        /// Shuts down the module, releasing resources and persisting any pending state.
        /// </summary>
        void Shutdown();
    }
}
