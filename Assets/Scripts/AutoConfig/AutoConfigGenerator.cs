﻿using BepInEx.Bootstrap;
using BepInEx.Configuration;
using LethalConfig.ConfigItems;
using LethalConfig.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LethalConfig.AutoConfig
{
    internal static class AutoConfigGenerator
    {
        public struct AutoConfigItem
        {
            public BaseConfigItem ConfigItem { get; set; }
            public Assembly Assembly { get; set; }
        }

        public struct ConfigFileAssemblyPair
        {
            public ConfigFile ConfigFile { get; set; }
            public Assembly Assembly { get; set; }
        }

        public static AutoConfigItem[] AutoGenerateConfigs(params ConfigFileAssemblyPair[] customConfigFiles)
        {
            var configItems = new List<AutoConfigItem>();
            var generatedConfigFiles = new List<ConfigFile>();

            var plugins = Chainloader.PluginInfos.Values.ToList();

            LogUtils.LogDebug($"{plugins.Count} mods loaded: {string.Join(";", plugins.Select(p => p.Metadata.GUID))}");

            foreach (var plugin in plugins )
            {
                LogUtils.LogDebug($"{plugin.Metadata.GUID} : {plugin.Metadata.Name} : {plugin.Metadata.Version}");
                var info = plugin.Metadata;

                try
                {
                    var assembly = Assembly.GetAssembly(plugin.Instance.GetType());

                    var pluginConfigItems = AutoGenerateConfigsForFile(new ConfigFileAssemblyPair
                    {
                        ConfigFile = plugin.Instance.Config,
                        Assembly = assembly
                    });
                    configItems.AddRange(pluginConfigItems);

                    generatedConfigFiles.Add(plugin.Instance.Config);
                }
                catch (Exception)
                {
                    LogUtils.LogWarning($"Invalid instance for \"{info.Name}\" plugin. Skipping.");
                    continue;
                }
            }

            foreach (var customConfigFile in customConfigFiles)
            {
                if (generatedConfigFiles.Contains(customConfigFile.ConfigFile))
                {
                    LogUtils.LogWarning($"Custom config file provided was already auto generated ({customConfigFile.ConfigFile.ConfigFilePath})");
                    continue;
                }

                var customConfigFileItems = AutoGenerateConfigsForFile(customConfigFile);
                configItems.AddRange(customConfigFileItems);

                generatedConfigFiles.Add(customConfigFile.ConfigFile);
            }

            return configItems.ToArray();
        }

        private static AutoConfigItem[] AutoGenerateConfigsForFile(ConfigFileAssemblyPair configAssemblyPair)
        {
            var configItems = new List<AutoConfigItem>();
            var configs = configAssemblyPair.ConfigFile.Select(c => c.Value);

            foreach (var config in configs)
            {
                var configItem = GenerateConfigForEntry(config);

                if (configItem != null)
                {
                    configItem.IsAutoGenerated = true;
                    configItems.Add(new AutoConfigItem
                    {
                        ConfigItem = configItem,
                        Assembly = configAssemblyPair.Assembly
                    });
                }
                else
                {
                    LogUtils.LogWarning($"No UI component found for config of type {config.SettingType.Name} ({config.Definition.Section}/{config.Definition.Key})");
                }
            }

            return configItems.ToArray();
        }

        private static BaseConfigItem GenerateConfigForEntry(ConfigEntryBase configEntryBase)
        {
            var type = configEntryBase.SettingType;
            if (type.IsEquivalentTo(typeof(int))) 
            {
                return GenerateItemForInt(configEntryBase);
            } 
            else if (type.IsEquivalentTo(typeof(float)))
            {
                return GenerateItemForFloat(configEntryBase);
            }
            else if (type.IsEquivalentTo(typeof(bool)))
            {
                return GenerateItemForBool(configEntryBase);
            }
            else if (type.IsEquivalentTo(typeof(string)))
            {
                return GenerateItemForString(configEntryBase);
            }
            else if (type.IsEnum)
            {
                return GenerateItemForEnum(configEntryBase);
            }

            return null;
        }

        private static BaseConfigItem GenerateItemForInt(ConfigEntryBase configEntryBase)
        {
            var configEntry = (ConfigEntry<int>)configEntryBase;
            var acceptableValues = configEntry.Description?.AcceptableValues as AcceptableValueRange<int>;

            if (acceptableValues != null)
            {
                return new IntSliderConfigItem(configEntry, true);
            } else
            {
                return new IntInputFieldConfigItem(configEntry, true);
            }
        }

        private static BaseConfigItem GenerateItemForFloat(ConfigEntryBase configEntryBase)
        {
            var configEntry = (ConfigEntry<float>)configEntryBase;
            var acceptableValues = configEntry.Description?.AcceptableValues as AcceptableValueRange<float>;

            if (acceptableValues != null)
            {
                return new FloatSliderConfigItem(configEntry, true);
            }
            else
            {
                return new FloatInputFieldConfigItem(configEntry, true);
            }
        }

        private static BaseConfigItem GenerateItemForBool(ConfigEntryBase configEntryBase)
        {
            var configEntry = (ConfigEntry<bool>)configEntryBase;
            return new BoolCheckBoxConfigItem(configEntry, true);
        }

        private static BaseConfigItem GenerateItemForString(ConfigEntryBase configEntryBase)
        {
            var configEntry = (ConfigEntry<string>)configEntryBase;
            return new TextInputFieldConfigItem(configEntry, true);
        }

        private static BaseConfigItem GenerateItemForEnum(ConfigEntryBase configEntryBase)
        {
            var enumType = configEntryBase.SettingType;
            var componentType = typeof(EnumDropDownConfigItem<>).MakeGenericType(enumType);
            var instance = Activator.CreateInstance(componentType, new object[] { configEntryBase, true });
            return (BaseConfigItem)instance;
        }
    }
}
