using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ro3Es
{
    /// <summary>
    /// Engancha el asignador de texto de TextMeshPro y de UGUI.
    /// RO3 usa <c>LangTransUtils_TMP</c> sobre TMP, así que todo el texto
    /// de interfaz pasa por aquí.
    /// </summary>
    [HarmonyPatch]
    internal static class TextHooks
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
                    Ro3EsPlugin.Log?.LogDebug($"Tipo no presente, se omite: {typeName}");
                    continue;
                }

                MethodBase setter = AccessTools.PropertySetter(type, "text");
                if (setter == null)
                {
                    Ro3EsPlugin.Log?.LogDebug($"Sin setter 'text' en {typeName}");
                    continue;
                }

                if (seen.Add(setter))
                {
                    Ro3EsPlugin.Log?.LogInfo($"Enganchado: {typeName}.text");
                    yield return setter;
                }
            }
        }

        private static void Prefix(ref string value, Component __instance)
        {
            TranslationStore store = Ro3EsPlugin.Store;
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
                Ro3EsPlugin.Log?.LogWarning($"Fallo traduciendo '{Truncate(original)}': {ex.Message}");
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

        public static void Apply(Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(TextHooks));
            }
            catch (Exception ex)
            {
                Ro3EsPlugin.Log?.LogError($"No se pudieron aplicar los hooks: {ex}");
            }
        }
    }
}
