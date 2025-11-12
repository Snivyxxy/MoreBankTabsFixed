using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FixedBankTabs;


namespace MoreBankTabs
{
    public static class AssetHandler
    {
        public static Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

        
        public static void GetAssetBundle(string bundlename)
        {
            Plugin.Logger.LogInfo("Loading bundle: " + bundlename);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Utilities.path, bundlename));
            bundles.Add(bundlename, assetBundle);
            Plugin.Logger.LogInfo("Bundle " + bundlename + " Loaded!");
        }
        public static T FetchFromBundle<T>(string bundle, string key) where T : Object
        {
            return bundles[bundle].LoadAsset<T>(key);
        }
    }
}
