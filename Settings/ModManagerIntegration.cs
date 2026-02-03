using System;
using System.Linq;
using System.Reflection;
using SimpleLabels.Utils;

namespace SimpleLabels.Settings
{
    /// <summary>
    /// Optional integration with "Mod Manager &amp; Phone App". Requests UI refresh when label settings change.
    /// </summary>
    /// <remarks>
    /// Initialize reflects ModManagerPhoneApp.ModSettingsEvents.RequestUIRefresh. RequestUIRefresh
    /// invokes it so the Mod Manager UI updates when colors etc. change. No-op if Mod Manager is not loaded.
    /// </remarks>
    public static class ModManagerIntegration
    {
        private static MethodInfo _requestUIRefreshMethod;

        /// <summary>
        /// Resolves ModSettingsEvents.RequestUIRefresh via reflection. Safe to call if Mod Manager is absent.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                var modManagerAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetType("ModManagerPhoneApp.ModSettingsEvents") != null);

                if (modManagerAssembly == null) return;
                var eventsType = modManagerAssembly.GetType("ModManagerPhoneApp.ModSettingsEvents");
                _requestUIRefreshMethod = eventsType?.GetMethod("RequestUIRefresh");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize Mod Manager integration: {ex.Message}");
            }
        }

        /// <summary>
        /// Invokes the Mod Manager's RequestUIRefresh so its UI reflects current label settings.
        /// </summary>
        public static void RequestUIRefresh()
        {
            try
            {
                _requestUIRefreshMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to request UI refresh: {ex.Message}");
            }
        }
    }
}