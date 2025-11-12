using BepInEx;
using System.IO;
using System.Reflection;

namespace MoreBankTabs
{
    public static class Utilities
    {
        public static string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //old.
        //public static string EmotePath = Path.Combine(path,"..", "EmotePackages");
        public static string pluginsFolder = BepInEx.Paths.PluginPath;
    }
}
