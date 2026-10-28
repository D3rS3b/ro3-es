using System;
using System.IO;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json.Linq;

[assembly: MelonInfo(typeof(Ro3Es.Melon.Ro3Mod), "RO3 Spanish", "0.1.0", "ro3-es")]
[assembly: MelonGame(null, null)]

namespace Ro3Es.Melon
{
    public class Ro3Mod : MelonMod
    {
        internal static MelonTranslationStore Store;
        internal static bool Enabled = true;
        internal static bool Capture = true;
        internal static bool WithContext = true;

        public override void OnInitializeMelon()
        {
            string modsDir = Path.GetDirectoryName(typeof(Ro3Mod).Assembly.Location);
            string dataDir = Path.Combine(modsDir, "ro3-es");
            Directory.CreateDirectory(dataDir);

            LoadConfig(Path.Combine(dataDir, "config.json"));

            string dictPath = Path.Combine(dataDir, "es.jsonl");
            string capturePath = Path.Combine(dataDir, "capture.jsonl");

            Store = new MelonTranslationStore(
                dictPath,
                capturePath,
                () => Enabled,
                () => Capture,
                () => WithContext);

            var harmony = new HarmonyLib.Harmony("community.ro3es.spanish");
            MelonTextHooks.Apply(harmony);

            MelonLogger.Msg($"RO3 Spanish cargado: {Store.Count} traducciones");
            MelonLogger.Msg($"Diccionario: {dictPath}");
            MelonLogger.Msg($"Captura:     {capturePath}");
        }

        private static void LoadConfig(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path,
                        "{\n  \"enabled\": true,\n  \"capture\": true,\n  \"context\": true\n}\n");
                    return;
                }

                var json = JObject.Parse(File.ReadAllText(path));
                Enabled = json.Value<bool?>("enabled") ?? true;
                Capture = json.Value<bool?>("capture") ?? true;
                WithContext = json.Value<bool?>("context") ?? true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"config.json inválido, usando valores por defecto: {ex.Message}");
            }
        }

        public override void OnApplicationQuit()
        {
            Store?.Dispose();
        }
    }
}
