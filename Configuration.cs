﻿using BepInEx.Configuration;
using Chameleon.Info;
using System.Collections.Generic;
using UnityEngine;

namespace Chameleon
{
    internal class Configuration
    {
        internal enum GordionStorms
        {
            Never = -1,
            Chance,
            Always
        }

        internal struct MoonCavernMapping
        {
            internal string moon;
            internal CavernType type;
            internal int weight;
        }

        static ConfigFile configFile;

        internal static ConfigEntry<bool> fancyEntranceDoors, recolorRandomRocks, doorLightColors, rainyMarch, eclipsesBlockMusic;
        internal static ConfigEntry<GordionStorms> stormyGordion;

        internal static List<MoonCavernMapping> mappings = [];

        internal static void Init(ConfigFile cfg)
        {
            configFile = cfg;

            ExteriorConfig();
            InteriorConfig();
            MigrateLegacyConfigs();
        }

        static void ExteriorConfig()
        {
            fancyEntranceDoors = configFile.Bind(
                "Exterior",
                "FancyEntranceDoors",
                true,
                "Changes the front doors to match how they look on the inside when a manor interior generates. (Works for ONLY vanilla levels!)");

            recolorRandomRocks = configFile.Bind(
                "Exterior",
                "RecolorRandomRocks",
                true,
                "Recolors random boulders to be white on snowy moons so they blend in better.");

            rainyMarch = configFile.Bind(
                "Exterior",
                "RainyMarch",
                true,
                "March is constantly rainy, as described in its terminal page. This is purely visual and does not affect quicksand generation.");

            stormyGordion = configFile.Bind(
                "Exterior",
                "StormyGordion",
                GordionStorms.Chance,
                "Allows for storms on Gordion, as described in its terminal page. This is purely visual and lightning does not strike at The Company.");

            eclipsesBlockMusic = configFile.Bind(
                "Exterior",
                "EclipsesBlockMusic",
                true,
                "Prevents the morning/afternoon ambience music from playing during Eclipsed weather, which has its own ambient track.");
        }

        static void InteriorConfig()
        {
            doorLightColors = configFile.Bind(
                "Interior",
                "DoorLightColors",
                true,
                "Dynamically adjust the color of the light behind the entrance doors depending on where you land and the current weather.");

            InteriorMineshaftConfig();
        }

        static void InteriorMineshaftConfig()
        {
            PopulateGlobalListWithCavernType(CavernType.Vanilla, "Vow:100,March:100,Adamance:100,Artifice:80");
            PopulateGlobalListWithCavernType(CavernType.Mesa, "Experimentation:100,Titan:100");
            PopulateGlobalListWithCavernType(CavernType.Desert, "Assurance:100,Offense:100");
            PopulateGlobalListWithCavernType(CavernType.Ice, "Rend:100,Dine:100");
            PopulateGlobalListWithCavernType(CavernType.Amethyst, "Embrion:100");
            PopulateGlobalListWithCavernType(CavernType.Gravel, "Artifice:20");
        }

        static void PopulateGlobalListWithCavernType(CavernType type, string defaultList)
        {
            string listName = $"{type}CavesList";
            
            string customList = configFile.Bind(
                "Interior.Mineshaft",
                listName,
                defaultList,
                $"A list of moons for which to assign \"{type}\" caves, with their respective weights.{(type != CavernType.Vanilla ? " Leave empty to disable." : string.Empty)}\n"
              + "Moon names are not case-sensitive, and can be left incomplete (ex. \"as\" will map to both Assurance and Asteroid-13.)"
              + (type == CavernType.Vanilla ? "\nUpon hosting a lobby, the full list of moon names will be printed in the debug log, which you can use as a guide." : string.Empty)).Value;

            if (string.IsNullOrEmpty(customList))
            {
                Plugin.Logger.LogDebug($"User has no {listName} defined");
                return;
            }

            try
            {
                foreach (string weightedMoon in customList.Split(','))
                {
                    string[] moonAndWeight = weightedMoon.Split(':');
                    int weight = -1;
                    if (moonAndWeight.Length == 2 && int.TryParse(moonAndWeight[1], out weight))
                    {
                        MoonCavernMapping mapping = new()
                        {
                            moon = moonAndWeight[0].ToLower(),
                            type = type,
                            weight = (int)Mathf.Clamp(weight, 0f, 99999f)
                        };
                        mappings.Add(mapping);
                        Plugin.Logger.LogDebug($"Successfully added \"{mapping.moon}\" to \"{mapping.type}\" caves list with weight {mapping.weight}");
                    }
                    else
                        Plugin.Logger.LogWarning($"Encountered an error parsing entry \"{weightedMoon}\" in the \"{listName}\" setting. It has been skipped");
                }
            }
            catch //(System.Exception e)
            {
                Plugin.Logger.LogError($"Encountered an error parsing the \"{listName}\" setting. Please double check that your config follows proper syntax, then restart your game.");
            }
        }

        static void MigrateLegacyConfigs()
        {
            // old cavern settings
            foreach (string oldCaveKey in new string[]{
                "IceCaves",
                "AmethystCave",
                "DesertCave",
                "MesaCave",
                "IcyTitan",
                "AdaptiveArtifice"})
            {
                configFile.Bind("Interior", oldCaveKey, false, "Legacy setting, doesn't work");
                configFile.Remove(configFile["Interior", oldCaveKey].Definition);
            }

            configFile.Save();
        }
    }
}