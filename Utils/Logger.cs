using MelonLoader;
using SimpleLabels.Settings;

namespace SimpleLabels.Utils
{
    /// <summary>
    /// Thin wrapper over MelonLogger. Msg is gated by ModSettings.ShowDebug; Warning and Error always log.
    /// </summary>
    /// <remarks>
    /// Use Msg for debug/info; it only outputs when ShowDebug is enabled. Warning and Error always
    /// call MelonLogger so failures and warnings are visible regardless of debug toggle.
    /// </remarks>
    public static class Logger
    {
        /// <summary>
        /// Logs a message only when ModSettings.ShowDebug is true.
        /// </summary>
        public static void Msg(string message)
        {
            if (ModSettings.ShowDebug != null && ModSettings.ShowDebug.Value)
                MelonLogger.Msg(message);
        }

        /// <summary>
        /// Logs a warning (always shown, regardless of ShowDebug).
        /// </summary>
        public static void Warning(string message)
        {
                MelonLogger.Warning(message);
        }

        /// <summary>
        /// Logs an error (always shown, regardless of ShowDebug).
        /// </summary>
        public static void Error(string message)
        {
            MelonLogger.Error(message); // Always show errors
        }
    }
}