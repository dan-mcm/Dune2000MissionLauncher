using System;
using System.Collections.Generic;
using System.Text;
using FunkyFr3sh.Dune2000;
using Nyerguds.Ini;
using FunkyFr3sh;
using System.Diagnostics;
using System.IO;
using MissionLauncher.Feda.Services;
using System.Windows.Forms;

namespace MissionLauncher
{
    public struct Mission
    {
        public string FileName;
        public string Briefing;
        public string RawBriefing; // Briefing text without color tags
        public string Name;
        public int SideId;
        public int Number;
        public string TextUib;
        public string CampaignFolder;
        public string ModsFolder;
        public string ColorsFile;
        public string IntelId;

        public Mission(string fileName, string briefing, string name, int sideId, int number, string textUib, string campaignFolder, string colorsFile, string modsFolder, string intelId)
        {
            FileName = fileName;
            SideId = sideId;
            Name = name;
            Briefing = briefing;
            RawBriefing = RemoveColorTags(briefing); // Store a version without color tags
            Number = number;
            TextUib = textUib;
            CampaignFolder = campaignFolder;
            ModsFolder = modsFolder;
            ColorsFile = colorsFile;
            IntelId = intelId;
        }

        private static string RemoveColorTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Define color tags to remove
            var colorTags = new[]
            {
                "{red}", "{green}", "{blue}", "{yellow}", "{cyan}", "{magenta}",
                "{white}", "{gray}", "{orange}", "{purple}", "{brown}", "{pink}",
                "{lime}", "{teal}", "{navy}", "{end}", "{endp}"
            };

            // Remove each color tag
            foreach (var tag in colorTags)
            {
                text = text.Replace(tag, "");
            }

            return text;
        }
    }

    public static class Missions
    {
        public static List<Mission> AtreidesMissions = new List<Mission>();
        public static List<Mission> HarkonnenMissions = new List<Mission>();
        public static List<Mission> OrdosMissions = new List<Mission>();
        public static List<Mission> EmperorMissions = new List<Mission>();
        public static List<Mission> FremenMissions = new List<Mission>();
        public static List<Mission> SmugglersMissions = new List<Mission>();
        public static List<Mission> MercenariesMissions = new List<Mission>();
        public static Mission CurrentMission;
        public static string CurrentCampaignFolder;
        public static string CurrentModsFolder;
        public static string CurrentColorsFile;
        public static string CurrentIntelId;
        public static UibFile TextUib;
        public static string MissionPath;

        public static void StartMission(int difficultyLevel, bool isWol = false)
        {
            CampaignManagerService.HandleWolSpecialCase(isWol);
            // Temporarily store the original briefing
            string originalBriefing = CurrentMission.Briefing;
            // Use the raw briefing without color tags for the game
            CurrentMission.Briefing = CurrentMission.RawBriefing;
            StartMission(difficultyLevel, CurrentMission);
            // Restore the original briefing with color tags
            CurrentMission.Briefing = originalBriefing;
            CampaignManagerService.RestoreFiles();
            CampaignManagerService.RestoreColors();
        }

        public static void StartMission(int difficultyLevel, Mission mission)
        {
            if (!string.IsNullOrWhiteSpace(CurrentCampaignFolder) && !string.IsNullOrWhiteSpace(CurrentModsFolder))
            {
                CampaignManagerService.InstallMods(CurrentCampaignFolder, CurrentModsFolder);
            }

            if (!string.IsNullOrEmpty(CurrentColorsFile))
            {
                CampaignManagerService.InstallColors(CurrentColorsFile, CurrentCampaignFolder);
            }

            // Get the original mission file paths
            string missionIniPath = Utils.PathCombine(MissionPath, mission.FileName + ".ini");
            
            try
            {
                // Temporarily modify the original mission file
                if (File.Exists(missionIniPath))
                {
                    var missionIni = new IniFile(missionIniPath);
                    string originalBriefing = missionIni.GetStringValue("Basic", "Briefing", "");
                    missionIni.SetStringValue("Basic", "Briefing", mission.RawBriefing.Replace(Environment.NewLine, "_"));
                    missionIni.WriteIni();

                    string spawnIniPath = Utils.PathCombine(Program.Path, "spawn.ini");
                    if (File.Exists(spawnIniPath)) File.Delete(spawnIniPath);
                    var spawnIni = new IniFile(spawnIniPath);
                    spawnIni.SetStringValue("Settings", "Scenario", mission.FileName);
                    spawnIni.SetIntValue("Settings", "MySideID", mission.SideId);
                    spawnIni.SetIntValue("Settings", "MissionNumber", mission.Number);
                    spawnIni.SetIntValue("Settings", "DifficultyLevel", difficultyLevel);
                    spawnIni.SetIntValue("Settings", "Seed", new Random().Next(int.MaxValue));
                    if (mission.TextUib.Length > 0) spawnIni.SetStringValue("Settings", "TextUib", mission.TextUib);
                    spawnIni.WriteIni();

                    var psi = new ProcessStartInfo(Utils.PathCombine(Program.Path, "dune2000.exe"));
                    psi.WorkingDirectory = Program.Path;
                    psi.Arguments = "-SPAWN";
                    if (Environment.OSVersion.Version >= new Version(6, 2, 9200, 0))
                    {
                        psi.EnvironmentVariables["__COMPAT_LAYER"] += "DWM8And16BitMitigation 16BITCOLOR ";
                        psi.UseShellExecute = false;
                    }
                    Process.Start(psi)?.WaitForExit();

                    // Restore the original briefing
                    missionIni.SetStringValue("Basic", "Briefing", originalBriefing);
                    missionIni.WriteIni();
                }
                else
                {
                    MessageBox.Show($"Error: Could not find mission file: {missionIniPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting mission: {ex.Message}");
            }
        }

        public static Mission GetMission(string fileName)
        {
            foreach (var mission in Missions.AtreidesMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.HarkonnenMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.OrdosMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.EmperorMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.FremenMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.SmugglersMissions)
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;
            foreach (var mission in Missions.MercenariesMissions) 
                if (mission.FileName.ToLower() == fileName.ToLower()) return mission;

            throw new Exception(string.Format("Mission '{0}' not found.", fileName));
        }

        public static void Load()
        {
            if (TextUib != null) return;
            string resourceCfgPath = Path.Combine(Program.Path, "RESOURCE.CFG");
            var resourceCfg = File.ReadAllLines(resourceCfgPath);
            MissionPath = Path.Combine(Program.Path, resourceCfg[3]);
            string language = new IniFile(Utils.PathCombine(Program.Path, "dune2000.ini")).GetStringValue("Options", "Language", "");
            TextUib = new UibFile(Utils.PathCombine(Program.Path, "data", "UI_DATA", string.Format("text{0}.uib", language)));
            var iniFiles = Directory.GetFiles(MissionPath, "*.ini", SearchOption.TopDirectoryOnly);

            if (iniFiles != null)
                foreach (string iniFile in iniFiles)
                {
                    var mapIni = new IniFile(iniFile);
                    string fileName = Path.GetFileNameWithoutExtension(iniFile);
                    int sideId = mapIni.GetIntValue("Basic", "SideId", -1);
                    if (sideId == -1) continue;

                    string textUib = mapIni.GetStringValue("Basic", "TextUib", "");
                    string briefing = mapIni.GetStringValue("Basic", "Briefing", "").Replace("_", Environment.NewLine);
                    if (briefing == "")
                    {
                        string briefingKey = mapIni.GetStringValue("Basic", "TextUibBriefingKey", "ääü+");
                        if (textUib.Length == 0) briefing = TextUib.GetValue(briefingKey, "No Briefing...");
                        else briefing = new UibFile(Utils.PathCombine(Program.Path, "data", "UI_DATA", textUib)).GetValue(briefingKey, "No Briefing...");
                        briefing = briefing.Replace("¬", Environment.NewLine);
                    }
                    string name = mapIni.GetStringValue("Basic", "Name", fileName);
                    int number = mapIni.GetIntValue("Basic", "MissionNumber", 0);
                    string campaignFolder = mapIni.GetStringValue("Data", "CampaignFolder", "");
                    string modsFolder = mapIni.GetStringValue("Data", "ModsFolder", "");
                    string colorsFile = mapIni.GetStringValue("Data", "ColoursFile", "");
                    string intelId = mapIni.GetStringValue("Data", "IntelId", "");

                    // Create mission using the constructor to properly initialize all fields including RawBriefing
                    var mission = new Mission(fileName, briefing, name, sideId, number, textUib, campaignFolder, colorsFile, modsFolder, intelId);

                    switch (mission.SideId)
                    {
                        case 0: AtreidesMissions.Add(mission); break;
                        case 1: HarkonnenMissions.Add(mission); break;
                        case 2: OrdosMissions.Add(mission); break;
                        case 3: EmperorMissions.Add(mission); break;
                        case 4: FremenMissions.Add(mission); break;
                        case 5: SmugglersMissions.Add(mission); break;
                        case 6: MercenariesMissions.Add(mission); break;
                        default: break;
                    }
                }
        }

        public static string ParseBriefingColors(string briefing)
        {
            if (string.IsNullOrEmpty(briefing))
                return briefing;

            // Define color markers and their corresponding RTF color codes
            var colorMap = new Dictionary<string, string>
            {
                {"{red}", "\\cf1 "},
                {"{green}", "\\cf2 "},
                {"{blue}", "\\cf3 "},
                {"{yellow}", "\\cf4 "},
                {"{cyan}", "\\cf5 "},
                {"{magenta}", "\\cf6 "},
                {"{white}", "\\cf7 "},
                {"{gray}", "\\cf8 "},
                {"{orange}", "\\cf9 "},
                {"{purple}", "\\cf10 "},
                {"{brown}", "\\cf11 "},
                {"{pink}", "\\cf12 "},
                {"{lime}", "\\cf13 "},
                {"{teal}", "\\cf14 "},
                {"{navy}", "\\cf15 "},
                {"{end}", "\\cf16 "}, // Just reset color without paragraph breaks
                {"{endp}", "\\cf16\\par\\par "} // Reset color and add paragraph breaks
            };

            // Build RTF header with color table and default text properties
            var rtf = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033" +
                      @"{\fonttbl{\f0\fnil\fcharset0 Microsoft Sans Serif;}}" +
                      @"{\colortbl ;" +
                      @"\red255\green0\blue0;" +      // red (1)
                      @"\red0\green255\blue0;" +      // green (2)
                      @"\red0\green0\blue255;" +      // blue (3)
                      @"\red255\green255\blue0;" +    // yellow (4)
                      @"\red0\green255\blue255;" +    // cyan (5)
                      @"\red255\green0\blue255;" +    // magenta (6)
                      @"\red255\green255\blue255;" +  // white (7)
                      @"\red128\green128\blue128;" +  // gray (8)
                      @"\red255\green165\blue0;" +    // orange (9)
                      @"\red128\green0\blue128;" +    // purple (10)
                      @"\red165\green42\blue42;" +    // brown (11)
                      @"\red255\green192\blue203;" +  // pink (12)
                      @"\red0\green255\blue0;" +      // lime (13)
                      @"\red0\green128\blue128;" +    // teal (14)
                      @"\red0\green0\blue128;" +      // navy (15)
                      @"\red192\green192\blue192;}" + // silver (16)
                      @"\cf16\highlight0\pard\li500\par\par "; // Set default color to silver, default indentation, and initial spacing

            // Process text for colors and preserve line breaks
            var text = briefing;

            // First handle line breaks
            text = text.Replace("__", "\\par\\par ");

            // Special handling for objectives and briefing sections
            text = text.Replace("MISSION OBJECTIVES:", "\\par\\par MISSION OBJECTIVES:\\par ");
            text = text.Replace("TACTICAL OBJECTIVES:", "\\par\\par TACTICAL OBJECTIVES:\\par ");
            text = text.Replace("BRIEFING:", "\\par\\par BRIEFING:");  // Removed extra \par after BRIEFING
            
            // Handle numbered objectives (looking for patterns like "_1. ", "_2. ", etc.)
            for (int i = 1; i <= 9; i++)
            {
                // Replace both patterns: "_1. " and "1. "
                text = text.Replace($"_{i}. ", $"\\par {i}. ");
                text = text.Replace($"{i}. ", $"\\par {i}. ");
            }

            // Handle remaining single underscores (that aren't part of numbered objectives)
            text = text.Replace("_", "\\par\\par "); // Add double paragraph for more spacing

            // Handle indentation for lines starting with spaces
            text = text.Replace("\r\n     ", "\\par\\par\\pard\\li500 "); // Add extra spacing
            text = text.Replace("\n     ", "\\par\\par\\pard\\li500 "); // Add extra spacing
            text = text.Replace("     ", "\\pard\\li500 ");

            // Replace color markers with RTF color codes
            foreach (var color in colorMap)
            {
                text = text.Replace(color.Key, color.Value);
            }

            // Add the text and close RTF
            rtf += text + "}";
            return rtf;
        }
    }
}
