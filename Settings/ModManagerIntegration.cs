using System;
using System.Linq;
using System.Reflection;
using SimpleLabels.Utils;

namespace SimpleLabels.Settings
{
    public static class ModManagerIntegration
    {
        private static MethodInfo _requestUIRefreshMethod;

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