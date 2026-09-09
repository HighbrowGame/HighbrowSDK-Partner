using System;
using System.Collections.Generic;
using Highbrow.Core.Utils;
using UnityEngine;

namespace Highbrow.Core
{
    /// <summary>
    /// Unified main entry point for Highbrow SDK.
    /// Manages module lifecycle, global configuration, and registration.
    /// </summary>
    public static class HighbrowSDK
    {
        public const string SdkVersion = "1.3.5";

        private static readonly Dictionary<Type, IHighbrowModule> registeredModules = new Dictionary<Type, IHighbrowModule>();
        private static HighbrowConfig activeConfig;
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            registeredModules.Clear();
            activeConfig = null;
            isInitialized = false;
            OnInitialized = null;
        }

        /// <summary>
        /// Gets the current SDK package version.
        /// </summary>
        public static string Version => SdkVersion;

        /// <summary>
        /// Gets whether the Highbrow SDK core has been initialized.
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets the active global configuration.
        /// </summary>
        public static HighbrowConfig Config => activeConfig;

        /// <summary>
        /// Event fired when SDK initialization completes.
        /// </summary>
        public static event Action<bool> OnInitialized;

        /// <summary>
        /// Initializes Highbrow SDK using the HighbrowSettings ScriptableObject asset
        /// located in Resources/Highbrow/HighbrowSettings.
        /// If asset is missing, safely falls back to default configuration.
        /// </summary>
        public static void Initialize()
        {
            HighbrowSettings settings = HighbrowSettings.LoadSettings();
            if (settings == null)
            {
                HighbrowLogger.LogWarning("[HighbrowSDK] HighbrowSettings asset not found in Resources/Highbrow. Initializing with default HighbrowConfig fallback.");
                Initialize(new HighbrowConfig());
                return;
            }

            Initialize(settings.ToConfig());
        }

        /// <summary>
        /// Initializes Highbrow SDK with the given configuration.
        /// Automatically discovers and initializes active modules based on config.
        /// </summary>
        /// <param name="config">Configuration instance.</param>
        public static void Initialize(HighbrowConfig config)
        {
            try
            {
                if (config == null)
                {
                    HighbrowLogger.LogError("Cannot initialize HighbrowSDK with null configuration.");
                    OnInitialized?.Invoke(false);
                    return;
                }

                if (!config.Validate(out string validationError))
                {
                    HighbrowLogger.LogError($"[HighbrowSDK] Configuration validation failed: {validationError}");
                    OnInitialized?.Invoke(false);
                    return;
                }

                if (isInitialized)
                {
                    HighbrowLogger.LogWarning("HighbrowSDK is already initialized. Skipping duplicate call.");
                    OnInitialized?.Invoke(true);
                    return;
                }

                activeConfig = config;
                HighbrowLogger.DebugMode = config.DebugMode;

                // Ensure lifecycle dispatcher is active
                HighbrowDispatcher.EnsureCreated();

                // Pre-warm context metadata on main thread for cross-thread tracking safety
                HighbrowContext.PreWarm(activeConfig);

                HighbrowLogger.Log($"Initializing Highbrow SDK Core v{SdkVersion} (Sandbox: {config.UseSandbox}, Market: {config.Market}, AppKey: {MaskKey(config.AppKey)})");

                // Initialize registered modules
                foreach (var kvp in registeredModules)
                {
                    try
                    {
                        kvp.Value.Initialize(activeConfig);
                        HighbrowLogger.Log($"Module [{kvp.Value.ModuleName}] initialized successfully.");
                    }
                    catch (Exception ex)
                    {
                        HighbrowLogger.LogError($"Error initializing module [{kvp.Value.ModuleName}]: {ex.Message}");
                    }
                }

                isInitialized = true;
                OnInitialized?.Invoke(true);

                // Unconditional 1-line confirmation feedback log for developer assurance
                string marketStr = ((MarketType)HighbrowContext.GetMarketType(activeConfig.Market)).ToString();
                string countryStr = HighbrowContext.GetCountry(activeConfig.CustomCountry);
                Debug.Log($"[HighbrowSDK] Initialized v{SdkVersion} successfully. (Mode: {activeConfig.ServerMode}, Market: {marketStr}, Country: {(string.IsNullOrEmpty(countryStr) ? "Auto" : countryStr)})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HighbrowSDK] Fatal error during initialization: {ex.Message}");
                OnInitialized?.Invoke(false);
            }
        }

        /// <summary>
        /// Registers a module into the SDK module registry.
        /// If the SDK is already initialized, the module is initialized immediately.
        /// </summary>
        public static void RegisterModule<T>(T module) where T : class, IHighbrowModule
        {
            if (module == null) return;

            Type type = typeof(T);
            registeredModules[type] = module;

            if (isInitialized && !module.IsInitialized && activeConfig != null)
            {
                try
                {
                    module.Initialize(activeConfig);
                    HighbrowLogger.Log($"Dynamically registered and initialized module [{module.ModuleName}].");
                }
                catch (Exception ex)
                {
                    HighbrowLogger.LogError($"Error initializing dynamically registered module [{module.ModuleName}]: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Retrieves an initialized module instance of the specified type.
        /// </summary>
        public static T GetModule<T>() where T : class, IHighbrowModule
        {
            Type type = typeof(T);
            if (registeredModules.TryGetValue(type, out var module))
            {
                return module as T;
            }
            return null;
        }

        /// <summary>
        /// Shuts down all registered modules and resets the SDK state.
        /// </summary>
        public static void Shutdown()
        {
            if (!isInitialized) return;

            HighbrowLogger.Log("Shutting down Highbrow SDK.");

            foreach (var kvp in registeredModules)
            {
                try
                {
                    kvp.Value.Shutdown();
                }
                catch (Exception ex)
                {
                    HighbrowLogger.LogError($"Error shutting down module [{kvp.Value.ModuleName}]: {ex.Message}");
                }
            }

            registeredModules.Clear();
            activeConfig = null;
            isInitialized = false;
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "[EMPTY]";
            if (key.Length <= 6) return "***";
            return key.Substring(0, 3) + "***" + key.Substring(key.Length - 3);
        }
    }
}
