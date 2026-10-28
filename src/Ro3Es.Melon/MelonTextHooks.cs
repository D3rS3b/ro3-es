using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ro3Es.Melon
{
    [HarmonyPatch]
    internal static class MelonTextHooks
    {
        private static readonly string[] Targets =
        {
            "TMPro.TMP_Text",
            "TMPro.TextMeshProUGUI",
            "UnityEngine.UI.Text"
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            var seen = new HashSet<MethodBase>();

            foreach (string typeName in Targets)
            {
                Type type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    MelonLogger.Msg($"Tipo no presente, se omite: {typeName}");
                    continue;
                }

                MethodBase setter = AccessTools.PropertySetter(type, "text");
                if (setter == null)
                {
                    MelonLogger.Msg($"Sin setter 'text' en {typeName}");
                    continue;
                }

                if (seen.Add(setter))
                {
                    MelonLogger.Msg($"Enganchado: {typeName}.text");
                    yield return setter;
                }
            }
        }

        private static void Prefix(ref string value, Component __instance)
        {
            var store = Ro3Mod.Store;
            if (store == null || value == null) return;

            string original = value;

            try
            {
                string translated = store.Translate(original, Describe(__instance));
                if (!string.Equals(original, translated, StringComparison.Ordinal))
                {
                    value = translated;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Fallo traduciendo '{Truncate(original)}': {ex.Message}");
            }
        }

        private static string Describe(Component component)
        {
            if (component == null) return null;

            try
            {
                string scene = SceneManager.GetActiveScene().name;
                string obj = component.gameObject != null ? component.gameObject.name : "?";
                return $"{scene}/{obj}";
            }
            catch
            {
                return null;
            }
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= 60 ? value : value.Substring(0, 60) + "…";
        }

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(MelonTextHooks));
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"No se pudieron aplicar los hooks: {ex}");
            }
        }
    }
}
