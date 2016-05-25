﻿using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A multiplayer map.
    /// </summary>
    public class Map
    {
        const int MAX_PLAYERS = 8;
        const int MAP_SIZE_X = 48;
        const int MAP_SIZE_Y = 24;

        public Map(string path)
        {
            BaseFilePath = path;
        }

        /// <summary>
        /// The name of the map.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The maximum amount of players supported by the map.
        /// </summary>
        public int MaxPlayers { get; private set; }

        /// <summary>
        /// The minimum amount of players supported by the map.
        /// </summary>
        public int MinPlayers { get; private set; }

        /// <summary>
        /// Whether to use AmountOfPlayers for limiting the player count of the map.
        /// If false (which is the default), AmountOfPlayers is only used for randomizing
        /// players to starting waypoints.
        /// </summary>
        public bool EnforceMaxPlayers { get; private set; }

        /// <summary>
        /// Controls if the map is meant for a co-operation game mode
        /// (enables briefing logic and forcing options, among others).
        /// </summary>
        public bool IsCoop { get; private set; }

        /// <summary>
        /// Contains co-op information.
        /// </summary>
        public CoopMapInfo CoopInfo { get; private set; }

        /// <summary>
        /// The briefing of the map.
        /// </summary>
        public string Briefing { get; private set; }

        /// <summary>
        /// The author of the map.
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// The calculated SHA1 of the map.
        /// </summary>
        public string SHA1 { get; private set; }

        /// <summary>
        /// The path to the map file.
        /// </summary>
        public string BaseFilePath { get; private set; }

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        public string PreviewPath { get; private set; }

        /// <summary>
        /// The game modes that the map is listed for.
        /// </summary>
        public string[] GameModes;

        /// <summary>
        /// The forced UnitCount for the map. -1 means none.
        /// </summary>
        int UnitCount = -1;

        /// <summary>
        /// The forced starting credits for the map. -1 means none.
        /// </summary>
        int Credits = -1;

        int NeutralHouseColor = -1;

        int SpecialHouseColor = -1;

        /// <summary>
        /// The pixel coordinates of the map's player starting locations.
        /// </summary>
        public List<Point> StartingLocations = new List<Point>();

        public Texture2D PreviewTexture { get; set; }

        private bool extractCustomPreview = true;

        /// <summary>
        /// If false, the preview shouldn't be extracted for this (custom) map.
        /// </summary>
        public bool ExtractCustomPreview
        {
            get { return extractCustomPreview; }
            set { extractCustomPreview = value; }
        }

        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>();

        List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>();

        public bool SetInfoFromINI(IniFile iniFile)
        {
            try
            {
                string baseSectionName = iniFile.GetStringValue(BaseFilePath, "BaseSection", String.Empty);

                if (!String.IsNullOrEmpty(baseSectionName))
                    iniFile.CombineSections(baseSectionName, BaseFilePath);

                Name = iniFile.GetStringValue(BaseFilePath, "Description", "Unnamed map");
                Author = iniFile.GetStringValue(BaseFilePath, "Author", "Unknown author");
                GameModes = iniFile.GetStringValue(BaseFilePath, "GameModes", "Default").Split(',');
                MinPlayers = iniFile.GetIntValue(BaseFilePath, "MinPlayers", 0);
                MaxPlayers = iniFile.GetIntValue(BaseFilePath, "MaxPlayers", 0);
                EnforceMaxPlayers = iniFile.GetBooleanValue(BaseFilePath, "EnforceMaxPlayers", false);
                PreviewPath = Path.GetDirectoryName(BaseFilePath) + "\\" +
                    iniFile.GetStringValue(BaseFilePath, "PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
                Briefing = iniFile.GetStringValue(BaseFilePath, "Briefing", String.Empty).Replace("@", Environment.NewLine);
                SHA1 = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + BaseFilePath + ".map");
                IsCoop = iniFile.GetBooleanValue(BaseFilePath, "IsCoopMission", false);
                Credits = iniFile.GetIntValue(BaseFilePath, "Credits", -1);
                UnitCount = iniFile.GetIntValue(BaseFilePath, "UnitCount", -1);
                NeutralHouseColor = iniFile.GetIntValue(BaseFilePath, "NeutralColor", -1);
                SpecialHouseColor = iniFile.GetIntValue(BaseFilePath, "SpecialColor", -1);

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = iniFile.GetStringValue(BaseFilePath, "DisallowedPlayerSides", String.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(Int32.Parse(sideIndex));

                    string[] disallowedColors = iniFile.GetStringValue(BaseFilePath, "DisallowedPlayerColors", String.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(Int32.Parse(colorIndex));

                    for (int i = 0; ; i++)
                    {
                        string[] enemyInfo = iniFile.GetStringValue(BaseFilePath, "EnemyHouse" + i, String.Empty).Split(
                            new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (enemyInfo.Length == 0)
                            break;

                        int[] info = Conversions.IntArrayFromStringArray(enemyInfo);

                        CoopInfo.EnemyHouses.Add(new CoopHouseInfo(info[0], info[1], info[2]));
                    }
                }

                string[] localSize = iniFile.GetStringValue(BaseFilePath, "LocalSize", "0,0,0,0").Split(',');
                string[] size = iniFile.GetStringValue(BaseFilePath, "Size", "0,0,0,0").Split(',');

                string[] previewSize = iniFile.GetStringValue(BaseFilePath, "PreviewSize", "0,0").Split(',');
                Point previewSizePoint = new Point(Int32.Parse(previewSize[0]), Int32.Parse(previewSize[1]));

                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = iniFile.GetStringValue(BaseFilePath, "Waypoint" + i, String.Empty);

                    if (String.IsNullOrEmpty(waypoint))
                        break;

                    StartingLocations.Add(GetWaypointCoords(waypoint, size, localSize, previewSizePoint));
                }

                if (MCDomainController.Instance.GetMapPreviewPreloadStatus())
                    PreviewTexture = LoadPreviewTexture();

                // Parse forced options

                string forcedOptionsSection = iniFile.GetStringValue(BaseFilePath, "ForcedOptions", String.Empty);

                if (!String.IsNullOrEmpty(forcedOptionsSection))
                    ParseForcedOptions(iniFile, forcedOptionsSection);

                string forcedSpawnIniOptionsSection = iniFile.GetStringValue(BaseFilePath, "ForcedSpawnIniOptions", String.Empty);

                if (!String.IsNullOrEmpty(forcedSpawnIniOptionsSection))
                    ParseSpawnIniOptions(iniFile, forcedSpawnIniOptionsSection);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Setting info for " + BaseFilePath + " failed! Reason: " + ex.Message);
                return false;
            }
        }

        public bool SetInfoFromMap(string path)
        {
            try
            {
                IniFile iniFile = new IniFile();
                iniFile.FileName = path;
                iniFile.AddSection("Basic");
                iniFile.AddSection("Map");
                iniFile.AddSection("Waypoints");
                iniFile.AddSection("ForcedOptions");
                iniFile.AddSection("ForcedSpawnIniOptions");

                iniFile.Parse();

                Name = iniFile.GetStringValue("Basic", "Name", "Unnamed map");
                Author = iniFile.GetStringValue("Basic", "Author", "Unknown author");
                GameModes = iniFile.GetStringValue("Basic", "GameMode", "Default").Split(',');
                for (int i = 0; i < GameModes.Length; i++)
                {
                    string gameMode = GameModes[i].Trim();
                    gameMode = gameMode.Substring(0, 1).ToUpperInvariant() + gameMode.Substring(1);
                    GameModes[i] = gameMode;
                }

                MinPlayers = iniFile.GetIntValue("Basic", "MinPlayer", 0);
                MaxPlayers = iniFile.GetIntValue("Basic", "MaxPlayer", 0);
                EnforceMaxPlayers = iniFile.GetBooleanValue("Basic", "EnforceMaxPlayers", true);
                //PreviewPath = Path.GetDirectoryName(BaseFilePath) + "\\" +
                //    iniFile.GetStringValue(BaseFilePath, "PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
                Briefing = iniFile.GetStringValue("Basic", "Briefing", string.Empty).Replace("@", Environment.NewLine);
                SHA1 = Utilities.CalculateSHA1ForFile(path);
                IsCoop = iniFile.GetBooleanValue("Basic", "IsCoopMission", false);
                Credits = iniFile.GetIntValue("Basic", "Credits", -1);
                UnitCount = iniFile.GetIntValue("Basic", "UnitCount", -1);
                NeutralHouseColor = iniFile.GetIntValue("Basic", "NeutralColor", -1);
                SpecialHouseColor = iniFile.GetIntValue("Basic", "SpecialColor", -1);

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = iniFile.GetStringValue("Basic", "DisallowedPlayerSides", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(Int32.Parse(sideIndex));

                    string[] disallowedColors = iniFile.GetStringValue("Basic", "DisallowedPlayerColors", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(Int32.Parse(colorIndex));

                    for (int i = 0; ; i++)
                    {
                        string[] enemyInfo = iniFile.GetStringValue("Basic", "EnemyHouse" + i, String.Empty).Split(
                            new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (enemyInfo.Length == 0)
                            break;

                        int[] info = Conversions.IntArrayFromStringArray(enemyInfo);

                        CoopInfo.EnemyHouses.Add(new CoopHouseInfo(info[0], info[1], info[2]));
                    }
                }

                // TODO Rework once we're able to load previews
                for (int i = 0; i < MaxPlayers; i++)
                {
                    StartingLocations.Add(new Point(i * 10, i * 10));
                }

                string forcedOptionsSection = iniFile.GetStringValue(BaseFilePath, "ForcedOptions", String.Empty);

                if (!String.IsNullOrEmpty(forcedOptionsSection))
                    ParseForcedOptions(iniFile, forcedOptionsSection);

                string forcedSpawnIniOptionsSection = iniFile.GetStringValue(BaseFilePath, "ForcedSpawnIniOptions", String.Empty);

                if (!String.IsNullOrEmpty(forcedSpawnIniOptionsSection))
                    ParseSpawnIniOptions(iniFile, forcedSpawnIniOptionsSection);

                return true;
            }
            catch
            {
                Logger.Log("Loading custom map " + path + " failed!");
                return false;
            }
        }

        private void ParseForcedOptions(IniFile iniFile, string forcedOptionsSection)
        {
            List<string> keys = iniFile.GetSectionKeys(forcedOptionsSection);

            foreach (string key in keys)
            {
                string value = iniFile.GetStringValue(forcedOptionsSection, key, String.Empty);

                int intValue = 0;
                if (Int32.TryParse(value, out intValue))
                {
                    ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni, string spawnIniOptionsSection)
        {
            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(spawnIniOptionsSection);

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key, 
                    forcedOptionsIni.GetStringValue(spawnIniOptionsSection, key, String.Empty)));
            }
        }

        public Texture2D LoadPreviewTexture()
        {
            if (File.Exists(ProgramConstants.GamePath + PreviewPath))
                return AssetLoader.LoadTextureUncached(PreviewPath);
            else
                return AssetLoader.LoadTexture("nopreview.png");
        }

        public void ApplySpawnIniCode(IniFile spawnIni, int totalPlayerCount, 
            int aiPlayerCount, int coopDifficultyLevel)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetStringValue("Settings", key.Key, key.Value);

            if (Credits != -1)
                spawnIni.SetIntValue("Settings", "Credits", Credits);

            if (UnitCount != -1)
                spawnIni.SetIntValue("Settings", "UnitCount", UnitCount);

            int neutralHouseIndex = totalPlayerCount + 1;
            int specialHouseIndex = totalPlayerCount + 2;

            if (IsCoop)
            {
                for (int i = 0; i < CoopInfo.EnemyHouses.Count; i++)
                {
                    int multiId = totalPlayerCount + i + 1;

                    CoopHouseInfo houseInfo = CoopInfo.EnemyHouses[i];

                    spawnIni.SetIntValue("HouseHandicaps", "Multi" + multiId, coopDifficultyLevel);
                    spawnIni.SetIntValue("HouseCountries", "Multi" + multiId, houseInfo.Side);
                    spawnIni.SetIntValue("HouseColors", "Multi" + multiId, houseInfo.Color);
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiId, houseInfo.StartingLocation);

                    int allyIndex = 0;

                    // Write alliances
                    for (int enemyIndex = 0; enemyIndex < CoopInfo.EnemyHouses.Count; enemyIndex++)
                    {
                        int allyMultiId = totalPlayerCount + enemyIndex;

                        if (enemyIndex == i)
                            continue;

                        spawnIni.SetIntValue("Multi" + multiId + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(allyIndex), allyMultiId);
                        allyIndex++;
                    }
                }

                spawnIni.SetIntValue("Settings", "AIPlayers", aiPlayerCount + CoopInfo.EnemyHouses.Count);

                neutralHouseIndex += CoopInfo.EnemyHouses.Count;
                specialHouseIndex += CoopInfo.EnemyHouses.Count;
            }

            if (NeutralHouseColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + neutralHouseIndex, NeutralHouseColor);

            if (SpecialHouseColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + specialHouseIndex, SpecialHouseColor);
        }

        private static string HouseAllyIndexToString(int index)
        {
            string[] houseAllyIndexStrings = new string[]
            {
                "One",
                "Two",
                "Three",
                "Four",
                "Five",
                "Six",
                "Seven"
            };

            return houseAllyIndexStrings[index];
        }

        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as a point.</returns>
        private static Point GetWaypointCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            Point previewSizePoint)
        {
            int rx = 0;
            int ry = 0;

            if (waypoint.Length == 5)
            {
                ry = Convert.ToInt32(waypoint.Substring(0, 2));
                rx = Convert.ToInt32(waypoint.Substring(2));
            }
            else // if location.Length == 6
            {
                ry = Convert.ToInt32(waypoint.Substring(0, 3));
                rx = Convert.ToInt32(waypoint.Substring(3));
            }

            int isoTileX = rx - ry + Convert.ToInt32(actualSizeValues[2]) - 1;
            int isoTileY = rx + ry - Convert.ToInt32(actualSizeValues[2]) - 1;

            int pixelPosX = isoTileX * MAP_SIZE_X / 2;
            int pixelPosY = isoTileY * MAP_SIZE_Y / 2;

            pixelPosX = pixelPosX - (Convert.ToInt32(localSizeValues[0]) * 48);
            pixelPosY = pixelPosY - (Convert.ToInt32(localSizeValues[1]) * 24);

            // Calculate map size
            int mapSizeX = Convert.ToInt32(localSizeValues[2]) * MAP_SIZE_X;
            int mapSizeY = Convert.ToInt32(localSizeValues[3]) * MAP_SIZE_Y;

            double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
            double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

            int x = Convert.ToInt32(ratioX * previewSizePoint.X);
            int y = Convert.ToInt32(ratioY * previewSizePoint.Y);

            return new Point(x, y);
        }
    }
}