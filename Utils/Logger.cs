using MelonLoader;
using SimpleLabels.Settings;

namespace SimpleLabels.Utils
{
    public static class Logger
    {

        public static void Msg(string message)
        {
            if (ModSettings.ShowDebug.Value)
                MelonLogger.Msg(message);
        }

        public static void Warning(string message)
        {
                MelonLogger.Warning(message);
        }

        public static void Error(string message)
        {
            MelonLogger.Error(message); // Always show errors
        }
    }
}