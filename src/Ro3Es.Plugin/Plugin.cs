using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace Ro3Es
{
    [BepInPlugin(Guid, PluginName, Version)]
    public sealed class Ro3EsPlugin : BaseUnityPlugin
    {
        public const string Guid = "community.ro3es.spanish";
        public const string PluginName = "RO3 Spanish";
        public const string Version = "0.1.0";

        internal static Ro3EsPlugin Instance;
        internal static ManualLogSource Log;
        internal static TranslationStore Store;

        internal static ConfigEntry<bool> CfgEnabled;
        internal static ConfigEntry<bool> CfgCapture;
        internal static ConfigEntry<bool> CfgContext;
        internal static ConfigEntry<bool> CfgHotReload;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            CfgEnabled = Config.Bind("General", "Enabled", true,
                "Activa la sustitución de texto por español.");
            CfgCapture = Config.Bind("Capture", "Enabled", true,
                "Guarda en capture.jsonl las cadenas que aún no tienen traducción.");
            CfgContext = Config.Bind("Capture", "Context", true,
                "Añade nombre de objeto y escena a cada cadena capturada.");
            CfgHotReload = Config.Bind("General", "HotReload", true,
                "Recarga es.jsonl automáticamente al guardarlo.");

            string dir = Path.Combine(Paths.PluginPath, "ro3-es");
            Directory.CreateDirectory(dir);

            string dictPath = Path.Combine(dir, "es.jsonl");
            string capturePath = Path.Combine(dir, "capture.jsonl");

            Store = new TranslationStore(
                dictPath,
                capturePath,
                () => CfgEnabled.Value,
                () => CfgCapture.Value,
                () => CfgContext.Value);

            _harmony = new Harmony(Guid);
            TextHooks.Apply(_harmony);

            if (CfgHotReload.Value)
            {
                Store.Watch();
            }

            Logger.LogInfo($"{PluginName} {Version} cargado: {Store.Count} traducciones desde {dictPath}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Store?.Dispose();
        }
    }
}
