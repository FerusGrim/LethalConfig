using LethalConfig.ConfigItems;
using LethalConfig.Mods;
using LethalConfig.AutoConfig;
using LethalConfig.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace LethalConfig
{
    public static class LethalConfigManager
    {
        internal static Dictionary<string, Mod> Mods { get; private set; } = new Dictionary<string, Mod>();
        private static Dictionary<Mod, Assembly> ModToAssemblyMap { get; set; } = new Dictionary<Mod, Assembly>();

        private static bool hasGeneratedMissingConfigs = false;

        internal static void AutoGenerateMissingConfigsIfNeeded()
        {
            if (hasGeneratedMissingConfigs) return;

            var existingModEntries = Mods.Values.ToArray();
            var existingConfigsFlat = Mods.SelectMany(kv => kv.Value.configItems);
            var generatedConfigs = AutoConfigGenerator.AutoGenerateConfigs();
            var missingConfigs = generatedConfigs
                .GroupBy(c => ModForAssembly(c.Assembly), c => c.ConfigItem)
                .Where(kv => kv.Key != null)
                .SelectMany(kv =>
                {
                    return kv.Select(c => { c.Owner = kv.Key; return c; })
                        .Where(c => !kv.Key.entriesToSkipAutoGen.Any(path => path.Matches(c)))
                        .Where(c => existingConfigsFlat.FirstOrDefault(ec => c.IsSameConfig(ec)) == null)
                        .GroupBy(c => c.Owner);
                }).ToDictionary(kv => kv.Key, kv => kv.Select(c => c));

            var generatedModEntries = missingConfigs.Keys.Except(existingModEntries).ToArray();
            foreach ( var entry in generatedModEntries)
            {
                entry.IsAutoGenerated = true;
                entry.modInfo.Description += "\n*This mod entry was automatically generated as it does not use LethalConfig directly.";
            }

            foreach (var kv in missingConfigs)
            {
                var assembly = ModToAssemblyMap.GetValueOrDefault(kv.Key);
                if (assembly != null)
                {
                    foreach (var config in kv.Value)
                    {
                        AddConfigItemForAssembly(config, assembly);
                    }
                }
            }

            LogUtils.LogInfo($"Generated {generatedModEntries.Count()} mod entries.");
            LogUtils.LogInfo($"Generated {generatedConfigs.Length} configs, of which {missingConfigs.SelectMany(kv => kv.Value).Count()} were missing and registered.");

            hasGeneratedMissingConfigs = true;
        }

        public static void AddConfigItem(BaseConfigItem configItem)
        {
            if (AddConfigItemForAssembly(configItem, Assembly.GetCallingAssembly()))
            {
                LogUtils.LogInfo($"Registered config \"{configItem}\"");
            }
        }

        private static bool AddConfigItemForAssembly(BaseConfigItem configItem, Assembly assembly)
        {
            var mod = ModForAssembly(assembly);
            if (mod == null)
            {
                LogUtils.LogWarning("Mod for assembly not found.");
                return false;
            }
            configItem.Owner = mod;
            if (mod.configItems.Where(c => c.IsSameConfig(configItem)).Count() > 0)
            {
                LogUtils.LogWarning($"Ignoring duplicated config \"{configItem}\"");
                return false;
            }
            mod.AddConfigItem(configItem);
            return true;
        }

        private static Mod ModForAssembly(Assembly assembly)
        {
            if (assembly.TryGetModInfo(out var modInfo))
            {
                if (Mods.TryGetValue(modInfo.GUID, out var mod)) return mod;

                var newMod = new Mod(modInfo);
                Mods.Add(modInfo.GUID, newMod);
                ModToAssemblyMap.Add(newMod, assembly);
                return newMod;
            }

            return null;
        }

        public static void SetModIcon(Sprite sprite)
        {
            if (sprite == null) return;

            var mod = ModForAssembly(Assembly.GetCallingAssembly());
            if (mod == null) return;

            mod.modInfo.Icon = sprite;
        }

        public static void SetModDescription(string description)
        {
            if (description == null) return;

            var mod = ModForAssembly(Assembly.GetCallingAssembly());
            if (mod == null) return;

            mod.modInfo.Description = description;
        }

        /// <summary>
        /// Skip Automatic Generation for a specific section
        /// </summary>
        public static void SkipAutoGenFor(string configSection)
        {
            var mod = ModForAssembly(Assembly.GetCallingAssembly());

            mod?.entriesToSkipAutoGen.Add(new ConfigEntryPath(configSection, "*"));
        }

        /// <summary>
        /// Skip Automatic Generation for a specific <see cref="ConfigEntry{T}"/>
        /// </summary>
        public static void SkipAutoGenFor(ConfigEntryBase configEntryBase)
        {
            var mod = ModForAssembly(Assembly.GetCallingAssembly());
            
            mod?.entriesToSkipAutoGen.Add(new ConfigEntryPath(configEntryBase.Definition.Section, configEntryBase.Definition.Key));
        }

        /// <summary>
        /// Skip Automatic Generation for your mod entirely
        /// </summary>
        public static void SkipAutoGen()
        {
            var mod = ModForAssembly(Assembly.GetCallingAssembly());
            
            mod?.entriesToSkipAutoGen.Add(new ConfigEntryPath("*", "*"));
        }
    } 
}
