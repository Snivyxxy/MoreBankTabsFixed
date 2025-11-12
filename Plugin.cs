
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MoreBankTabs;

namespace FixedBankTabs
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        Harmony patcher;
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Logger.LogInfo("Patching functions...");

            patcher = new Harmony("FixedMoreBankTabs");
            patcher.PatchAll(typeof(BankPatches));
            Logger.LogInfo("Done!");

            Logger.LogInfo("Registering Assets...");

            AssetHandler.GetAssetBundle("morebanktabs");


            Logger.LogInfo("Initializing Extra Data...");
            BankPatches.init();
            Logger.LogInfo("Setup Complete!");
        }
    }
}

