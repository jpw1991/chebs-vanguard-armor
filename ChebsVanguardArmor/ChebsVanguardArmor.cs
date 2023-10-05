using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary;
using ChebsVanguardArmor.Items;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Paths = BepInEx.Paths;

namespace ChebsVanguardArmor
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ChebsVanguardArmor : BaseUnityPlugin
    {
        public const string PluginGuid = "com.chebgonaz.chebsvanguardarmor";
        public const string PluginName = "ChebsVanguardArmor";
        public const string PluginVersion = "1.2.0";

        private const string ConfigFileName = PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        public readonly System.Version ChebsValheimLibraryVersion = new("2.4.0");

        private readonly Harmony harmony = new(PluginGuid);

        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static VanguardChestItem VanguardChest = new();
        public static VanguardLegsItem VanguardLegs = new();
        public static FireCapeItem FireCape = new();
        public static VanguardHelmItem VanguardHelm = new();

        private void Awake()
        {
            if (!Base.VersionCheck(ChebsValheimLibraryVersion, out string message))
            {
                Jotunn.Logger.LogWarning(message);
            }

            CreateConfigValues();
            LoadAssetBundle();
            harmony.PatchAll();

            SetupWatcher();

            PrefabManager.OnVanillaPrefabsAvailable += DoOnVanillaPrefabsAvailable;
        }

        private void DoOnVanillaPrefabsAvailable()
        {
            UpdateAllRecipes();
            PrefabManager.OnVanillaPrefabsAvailable -= DoOnVanillaPrefabsAvailable;
        }

        private void UpdateAllRecipes(bool updateItemsInScene = false)
        {
            VanguardChest.UpdateRecipe();
            VanguardLegs.UpdateRecipe();
            FireCape.UpdateRecipe();
            VanguardHelm.UpdateRecipe();
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            RadeonFriendly = Config.Bind($"{GetType().Name} (Client)", "RadeonFriendly",
                false, new ConfigDescription("ONLY set this to true if you have graphical issues with " +
                                             "the mod. It will disable all particle effects for the mod's prefabs " +
                                             "which seem to give users with Radeon cards trouble for unknown " +
                                             "reasons. If you have problems with lag it might also help to switch" +
                                             "this setting on."));
            VanguardChest.CreateConfigs(this);
            VanguardLegs.CreateConfigs(this);
            FireCape.CreateConfigs(this);
            VanguardHelm.CreateConfigs(this);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.Error += (sender, e) => Jotunn.Logger.LogError($"Error watching for config changes: {e}");
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Logger.LogInfo("Read updated config values");
                Config.Reload();
                UpdateAllRecipes(true);
            }
            catch (Exception exc)
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}: {exc}");
                Logger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private void LoadAssetBundle()
        {
            // order is important (I think): items, creatures, structures
            var assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "rs_vanguardarmor");
            var chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                {
                    var vanguardChestPrefab = Base.LoadPrefabFromBundle(VanguardChest.PrefabName, chebgonazAssetBundle,
                        RadeonFriendly.Value);
                    ItemManager.Instance.AddItem(VanguardChest.GetCustomItemFromPrefab(vanguardChestPrefab));
                }
                {
                    var vanguardLegsPrefab = Base.LoadPrefabFromBundle(VanguardLegs.PrefabName, chebgonazAssetBundle,
                        RadeonFriendly.Value);
                    ItemManager.Instance.AddItem(VanguardLegs.GetCustomItemFromPrefab(vanguardLegsPrefab));
                }
                {
                    var fireCapePrefab = Base.LoadPrefabFromBundle(FireCape.PrefabName, chebgonazAssetBundle,
                        RadeonFriendly.Value);
                    ItemManager.Instance.AddItem(FireCape.GetCustomItemFromPrefab(fireCapePrefab));
                }
                {
                    var vanguardHelmPrefab = Base.LoadPrefabFromBundle(VanguardHelm.PrefabName, chebgonazAssetBundle,
                        RadeonFriendly.Value);
                    ItemManager.Instance.AddItem(VanguardHelm.GetCustomItemFromPrefab(vanguardHelmPrefab));
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading assets: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }
    }
}