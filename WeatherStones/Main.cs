using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WeatherStones
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        public const string MODNAME = "WeatherStones";
        public const string AUTHOR = "GoldenJude";
        public const string GUID = AUTHOR + "_" + MODNAME;
        public const string VERSION = "0.1.3";

        public static ManualLogSource log;

        public static ConfigEntry<int> radius;
        public static ConfigEntry<string> station;
        public static ConfigEntry<string> requirements;

        public static ConfigSync configSync = new ConfigSync(GUID) { DisplayName = MODNAME, CurrentVersion = VERSION, IsLocked = true };

        void Awake()
        {
            log = Logger;

            new Harmony(GUID).PatchAll(Assembly.GetExecutingAssembly());

            radius = config("function", "radius", 100, "radius in which set weather applies");
            //station = Config.Bind("", "", "piece_workbench", "");
            //requirements = Config.Bind("", "", "Stone:30, GreydwarfEye:20, SurtlingCore:1", "");
        }

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
    static class AddEnvPatch
    {
        private static void Postfix(EnvMan __instance)
        {
            var env = __instance.m_environments.Find(x => x.m_name.Equals("Crypt", System.StringComparison.Ordinal));

            __instance.m_environments.Add(env);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class AddPrefabs
    {
        private static void Postfix(ZNetScene __instance)
        {
            var bundle = AssetBundle.LoadFromStream(new MemoryStream(Properties.Resources.weatherstone));

            var stone_big = bundle.LoadAsset<GameObject>("WS_stone_big");
            stone_big.FixReferences();

            var hammer = __instance.GetPrefab("Hammer");
            var hammerPieces = hammer.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces.m_pieces;

            if(!hammerPieces.Exists(p => p.name.Equals(stone_big.name, System.StringComparison.Ordinal))) hammerPieces.Add(stone_big);
            if (!__instance.m_prefabs.Exists(p => p.name.Equals(stone_big.name, System.StringComparison.Ordinal))) __instance.m_prefabs.Add(stone_big);
            if (!__instance.m_namedPrefabs.ContainsKey(stone_big.name.GetStableHashCode())) __instance.m_namedPrefabs.Add(stone_big.name.GetStableHashCode(), stone_big);

            //recipe
            var reqs = new List<Piece.Requirement>();
            reqs.Add(new Piece.Requirement()
            {
                m_resItem = __instance.GetPrefab("Stone").GetComponent<ItemDrop>(),
                m_amount = 20,
                m_recover = true
            });
            reqs.Add(new Piece.Requirement()
            {
                m_resItem = __instance.GetPrefab("GreydwarfEye").GetComponent<ItemDrop>(),
                m_amount = 10,
                m_recover = true
            });
            reqs.Add(new Piece.Requirement()
            {
                m_resItem = __instance.GetPrefab("SurtlingCore").GetComponent<ItemDrop>(),
                m_amount = 1,
                m_recover = true
            });
            stone_big.GetComponent<Piece>().m_resources = reqs.ToArray();
            stone_big.GetComponent<Piece>().m_craftingStation = __instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();

            bundle.Unload(false);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public static class AddLocalization
    {
        static void Postfix()
        {
            Localization.instance.AddWord("WS_stone_big", "Weather stone");
        }
    }
}
