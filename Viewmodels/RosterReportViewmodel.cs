using goh_ui.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace goh_ui.Viewmodels
{
    public class RosterReportViewmodel : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(RosterReportViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(RosterReportViewmodel));
        }

        /// <summary> List of detailed metadata for all defined units. </summary>
        private IEnumerable<UnitDetails> UnitDetails { get; set; }

        /// <summary> Data for each guild member. </summary>
        private PlayerList Members { get; set; }

        public RosterReportViewmodel(PlayerList members, IEnumerable<UnitDetails> unitDetails)
        {
            Members = members ?? throw new ArgumentNullException("members");
            UnitDetails = unitDetails;

            DoBrowse = new SimpleCommand(BrowseForTarget);
            GenerateReport = new SimpleCommand(DoReport);
        }


        #region Public members

        /// <summary> Whether the "minimum star level" box is selected. </summary>
        public bool StarLevelSelected
        {
            get { return (bool)GetValue(StarLevelSelectedProperty); }
            set { SetValue(StarLevelSelectedProperty, value); }
        }
        public static readonly DependencyProperty StarLevelSelectedProperty = _dp<bool>("StarLevelSelected", false);

        /// <summary> Whether the "minimum gear level" box is selected. </summary>
        public bool GearLevelSelected
        {
            get { return (bool)GetValue(GearLevelSelectedProperty); }
            set { SetValue(GearLevelSelectedProperty, value); }
        }
        public static readonly DependencyProperty GearLevelSelectedProperty = _dp<bool>("GearLevelSelected", false);

        /// <summary> Whether the "minimum character power" box is selected. </summary>
        public bool CharPowerSelected
        {
            get { return (bool)GetValue(CharPowerSelectedProperty); }
            set { SetValue(CharPowerSelectedProperty, value); }
        }
        public static readonly DependencyProperty CharPowerSelectedProperty = _dp<bool>("CharPowerSelected", false);

        /// <summary> Whether the "minimum team power" box is selected. </summary>
        public bool TeamPowerSelected
        {
            get { return (bool)GetValue(TeamPowerSelectedProperty); }
            set { SetValue(TeamPowerSelectedProperty, value); }
        }
        public static readonly DependencyProperty TeamPowerSelectedProperty = _dp<bool>("TeamPowerSelected", false);

        /// <summary> List of possible star levels. </summary>
        public int[] StarLevels { get; } = { 1, 2, 3, 4, 5, 6, 7 };

        /// <summary> Currently-selected star level. </summary>
        public int SelectedStarLevel
        {
            get { return (int)GetValue(SelectedStarLevelProperty); }
            set { SetValue(SelectedStarLevelProperty, value); }
        }
        public static readonly DependencyProperty SelectedStarLevelProperty = _dp<int>("SelectedStarLevel", 0);

        /// <summary> List of possible gear levels. </summary>
        public int[] GearLevels { get; } = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        /// <summary> Currently-selected gear level. </summary>
        public int SelectedGearLevel
        {
            get { return (int)GetValue(SelectedGearLevelProperty); }
            set { SetValue(SelectedGearLevelProperty, value); }
        }
        public static readonly DependencyProperty SelectedGearLevelProperty = _dp<int>("SelectedGearLevel", 0);

        /// <summary> User-specified minimum character power. </summary>
        public string CharPower
        {
            get { return (string)GetValue(CharPowerProperty); }
            set { SetValue(CharPowerProperty, value); }
        }
        public static readonly DependencyProperty CharPowerProperty = _dp<string>("CharPower");

        /// <summary> User-specified minimum team power. </summary>
        public string TeamPower
        {
            get { return (string)GetValue(TeamPowerProperty); }
            set { SetValue(TeamPowerProperty, value); }
        }
        public static readonly DependencyProperty TeamPowerProperty = _dp<string>("TeamPower");

        /// <summary> Path to output file. </summary>
        public string OutputPath
        {
            get { return (string)GetValue(OutputPathProperty); }
            set { SetValue(OutputPathProperty, value); }
        }
        public static readonly DependencyProperty OutputPathProperty = _dp<string>("OutputPath");

        public ICommand DoBrowse { get; private set; }
        public ICommand GenerateReport { get; private set; }

        #endregion

        /// <summary> Browse for and select output file. </summary>
        private void BrowseForTarget()
        {
            // Prompt user for output filename
            var dlg = new SaveFileDialog()
            {
                Title = "Save report as ...",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true,
                ValidateNames = true,
                DefaultExt = ".csv",
                Filter = "CSV Files|*.csv|All Files|*.*"
            };
            var result = dlg.ShowDialog();
            if (!result.HasValue || result == false || string.IsNullOrWhiteSpace(dlg.FileName))
            {
                return;
            }

            // Store selected path
            OutputPath = dlg.FileName;
        }


        /// <summary>
        /// A guild member's squad that matches a preset and filter criteria.
        /// </summary>
        private class PresetSquad
        {
            /// <summary> Player that owns this squad. </summary>
            public string Owner;
            /// <summary> Units in the squad. </summary>
            public List<Character> Characters;
            /// <summary> Total power of this squad. </summary>
            public long SquadPower => Characters.Select(c => c.TruePower).Sum();
        }

        /// <summary>
        /// All the matching squads for a preset.
        /// </summary>
        private class PresetMatch
        {
            public PresetMatch(SquadPreset preset)
            {
                Preset = preset;
                Matches = new List<PresetSquad>();
            }

            /// <summary> Preset to match. </summary>
            public SquadPreset Preset;
            /// <summary> Squads that match this preset. </summary>
            public List<PresetSquad> Matches;
        }

        /// <summary> Generate and save the roster report. </summary>
        private void DoReport()
        {
            if (!ValidateFilters())
                return;
            int char_power = 0, team_power = 0;
            try
            {
                if (CharPowerSelected)
                    char_power = int.Parse(CharPower);
                if (TeamPowerSelected)
                    team_power = int.Parse(TeamPower);
            }
            catch (Exception)
            {
                // Should never happen since we validated earlier
                ErrorMessage("Invalid character or team power specified.");
                return;
            }

            // Load squad presets from file
            List<SquadPreset> PresetsList = SquadPreset.LoadPresets();
            if (PresetsList.Count == 0)
            {
                ErrorMessage("Unable to load squad presets.");
                return;
            }

            // Normalize names in presets
            var unit_names = UnitDetails.Select((u) => u.name);
            foreach(var preset in PresetsList)
            {
                preset.Character1 = SquadFinderViewmodel.FindUnit(preset.Character1, unit_names);
                preset.Character2 = SquadFinderViewmodel.FindUnit(preset.Character2, unit_names);
                preset.Character3 = SquadFinderViewmodel.FindUnit(preset.Character3, unit_names);
                preset.Character4 = SquadFinderViewmodel.FindUnit(preset.Character4, unit_names);
                preset.Character5 = SquadFinderViewmodel.FindUnit(preset.Character5, unit_names);
            }

            // Find out who has each squad
            List<PresetMatch> results = new List<PresetMatch>();
            foreach (var preset in PresetsList)
            {
                PresetMatch this_preset = new PresetMatch(preset);

                List<string> names = new List<string>()
                {
                    preset.Character1,
                    preset.Character2,
                    preset.Character3,
                    preset.Character4,
                    preset.Character5
                };

                // Check each guild member's roster
                foreach(var player in Members)
                {
                    PresetSquad ps = new PresetSquad()
                    {
                        Owner = player.Name,
                        Characters = null
                    };

                    // Make sure this player has all of the characters, and that they meet any
                    // filter criteria that was set.
                    bool problem = false;
                    var candidates = player.Roster.Where(c => names.Contains(c.name)).ToList();
                    if (candidates == null || candidates.Count() != 5)
                        problem = true;
                    else
                    {
                        if (StarLevelSelected && (candidates.Select(m => m.rarity).Min() < SelectedStarLevel))
                            problem = true;
                        if (GearLevelSelected && (candidates.Select(m => m.gear).Min() < SelectedGearLevel))
                            problem = true;
                        if (CharPowerSelected && (candidates.Select(m => m.TruePower).Min() < char_power))
                            problem = true;
                        if (TeamPowerSelected && (candidates.Select(m => m.TruePower).Sum() < team_power))
                            problem = true;
                    }

                    if (!problem)
                        ps.Characters = new List<Character>(candidates);

                    // Add this squad to the list
                    this_preset.Matches.Add(ps);
                }

                // Add results for this preset to the master list
                results.Add(this_preset);
            }

            // Generate output file
            try
            {
                GenerateReportFile(PresetsList, results);
            }
            catch (Exception e)
            {
                ErrorMessage($"Error saving file:\n{e.Message}");
                return;
            }
            MessageBox.Show("Report generation complete.", "Success", MessageBoxButton.OK, MessageBoxImage.None);
        }

        /// <summary> Validate any filters set by the user. </summary>
        /// <returns>True if validation successful, false if an error found.</returns>
        private bool ValidateFilters()
        {
            if (StarLevelSelected && !StarLevels.Contains(SelectedStarLevel))
            {
                ErrorMessage("Invalid star level selected.");
                return false;
            }

            if (GearLevelSelected && !GearLevels.Contains(SelectedGearLevel))
            {
                ErrorMessage("Invalid gear level selected.");
                return false;
            }

            if (CharPowerSelected)
            {
                if (string.IsNullOrWhiteSpace(CharPower))
                {
                    ErrorMessage("Minimum character power not specified.");
                    return false;
                }
                if (!int.TryParse(CharPower, out int power) || (power < 0))
                {
                    ErrorMessage("Character power must be a number greater than zero.");
                    return false;
                }
            }

            if (TeamPowerSelected)
            {
                if (string.IsNullOrWhiteSpace(TeamPower))
                {
                    ErrorMessage("Minimum squad power not specified.");
                    return false;
                }
                if (!int.TryParse(TeamPower, out int power) || (power < 0))
                {
                    ErrorMessage("Squad power must be a number greater than zero.");
                    return false;
                }
            }

            return true;
        }

        /// <summary> Generate a .csv file with the roster report results. </summary>
        /// <param name="presets">List of presets used in the report.</param>
        /// <param name="matches">List of matches for the presets.</param>
        private void GenerateReportFile(IEnumerable<SquadPreset> presets, IEnumerable<PresetMatch> matches)
        {
            if (presets == null)
                throw new ArgumentNullException("presets");
            if (matches == null)
                throw new ArgumentNullException("matches");

            using (var writer = new StreamWriter(OutputPath))
            {
                // Write header
                StringBuilder line = new StringBuilder();
                line.Append("Player,");
                line.Append(string.Join(",", presets.Select(p => p.Name)));
                writer.WriteLine(line.ToString());

                // Write row for each guild member
                foreach (var player in Members)
                {
                    line.Clear();
                    line.Append($"{player.Name},");
                    foreach (var preset in presets)
                    {
                        // If player has a squad that matched the criteria, record its power in the report
                        var this_preset = matches.FirstOrDefault(r => r.Preset == preset);
                        if (this_preset != null)
                        {
                            var this_squad = this_preset.Matches.FirstOrDefault(m => m.Owner == player.Name);
                            if (this_squad != null && this_squad.Characters != null)
                                line.Append(this_squad.SquadPower);
                        }
                        line.Append(",");
                    }
                    writer.WriteLine(line.ToString());
                }
            }
        }

        /// <summary> Display an error message in a popup dialog. </summary>
        /// <param name="message">Message to display.</param>
        private void ErrorMessage(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
