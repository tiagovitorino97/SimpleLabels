using MelonLoader;
using SimpleLabels.Settings;

namespace SimpleLabels.Utils
{
    public static class Logger
    {
        private static readonly bool ShowInConsole = ModSettings.ShowDebug.Value;

        public static void Msg(string message)
        {
            if (ShowInConsole)
                MelonLogger.Msg(message);
        }

        public static void Warning(string message)
        {
            if (ShowInConsole)
                MelonLogger.Warning(message);
        }

        public static void Error(string message)
        {
            MelonLogger.Error(message); // Always show errors
        }
    }
}