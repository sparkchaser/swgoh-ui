using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class SquadFinderViewmodel : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(SquadFinderViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(SquadFinderViewmodel));
        }


        public SquadFinderViewmodel(PlayerList members, List<UnitDetails> unitDetails)
        {
            Members = members ?? throw new ArgumentNullException("members");
            UnitDetails = unitDetails;

            SearchCommand = new SimpleCommand(DoSearch);

            PlayerListUpdated();

            TryLoadPresets();
        }


        /// <summary> Data for each guild member. </summary>
        public PlayerList Members { get; private set; }

        /// <summary> List of all known units. </summary>
        public List<string> Units { get; private set; }

        /// <summary> List of detailed metadata for all defined units. </summary>
        public List<UnitDetails> UnitDetails { get; private set; }

        /// <summary> Rebuild unit list after a roster change. </summary>
        public void PlayerListUpdated()
        {
            if (Members == null)
            {
                Units = null;
                return;
            }

            // Use UnitDetails if available, since it can contain units not found
            // in any player's roster.
            if (UnitDetails != null && UnitDetails.Count > 0)
            {
                Units = UnitDetails.Where(u => u.combatType == goh_ui.UnitDetails.COMBATTYPE_CHARACTER).Select(u => u.name).ToList();
                Units.Sort();
                return;
            }

            // If UnitDetails not available, derive a list by combining the members' rosters.
            Units = Members.Select(m => m.Roster.Where(u => u.combatType == goh_ui.UnitDetails.COMBATTYPE_CHARACTER)
                                                .Select(c => c.name)) // map each member to an array of unit names
                           .SelectMany(x => x) // flatten list
                           .Distinct()         // de-duplicate
                           .ToList();
            Units.Sort();
        }


        #region Dependency properties

        /// <summary> Currently-selected unit (slot #1). </summary>
        public string SelectedUnit1
        {
            get { return (string)GetValue(SelectedUnit1Property); }
            set { SetValue(SelectedUnit1Property, value); }
        }
        public static readonly DependencyProperty SelectedUnit1Property = _dp<string>("SelectedUnit1");

        /// <summary> Currently-selected unit (slot #2). </summary>
        public string SelectedUnit2
        {
            get { return (string)GetValue(SelectedUnit2Property); }
            set { SetValue(SelectedUnit2Property, value); }
        }
        public static readonly DependencyProperty SelectedUnit2Property = _dp<string>("SelectedUnit2");

        /// <summary> Currently-selected unit (slot #3). </summary>
        public string SelectedUnit3
        {
            get { return (string)GetValue(SelectedUnit3Property); }
            set { SetValue(SelectedUnit3Property, value); }
        }
        public static readonly DependencyProperty SelectedUnit3Property = _dp<string>("SelectedUnit3");

        /// <summary> Currently-selected unit (slot #4). </summary>
        public string SelectedUnit4
        {
            get { return (string)GetValue(SelectedUnit4Property); }
            set { SetValue(SelectedUnit4Property, value); }
        }
        public static readonly DependencyProperty SelectedUnit4Property = _dp<string>("SelectedUnit4");

        /// <summary> Currently-selected unit (slot #5). </summary>
        public string SelectedUnit5
        {
            get { return (string)GetValue(SelectedUnit5Property); }
            set { SetValue(SelectedUnit5Property, value); }
        }
        public static readonly DependencyProperty SelectedUnit5Property = _dp<string>("SelectedUnit5");

        /// <summary> List of squads matching the current setup. </summary>
        public List<SquadLookupResult> SearchResults
        {
            get { return (List<SquadLookupResult>)GetValue(SearchResultsProperty); }
            set { SetValue(SearchResultsProperty, value); }
        }
        public static readonly DependencyProperty SearchResultsProperty = _dp<List<SquadLookupResult>>("SearchResults");

        /// <summary> Number of search results in the latest query. </summary>
        public int ResultCount
        {
            get { return (int)GetValue(ResultCountProperty); }
            set { SetValue(ResultCountProperty, value); }
        }
        public static readonly DependencyProperty ResultCountProperty = _dp<int>("ResultCount");

        #endregion


        /// <summary> Search for squads matching the current setup. </summary>
        private void DoSearch()
        {
            // Make sure unit selection is complete and valid
            List<String> selected = new List<string>(){SelectedUnit1, SelectedUnit2, SelectedUnit3, SelectedUnit4, SelectedUnit5};
            selected.RemoveAll(x => string.IsNullOrWhiteSpace(x)); // no empty selections
            if (selected.Distinct().Count() != 5) // no duplicates
            {
                MessageBox.Show("Invalid squad selection", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Figure out who else has all of these toons
            var list = new List<SquadLookupResult>();
            foreach (var m in Members)
            {
                var names = m.Roster.Select(c => c.name).ToList();
                if (names.Contains(SelectedUnit1) && names.Contains(SelectedUnit2) &&
                    names.Contains(SelectedUnit3) && names.Contains(SelectedUnit4) &&
                    names.Contains(SelectedUnit5))
                {
                    var c1 = m.Roster.Where(c => c.name == SelectedUnit1).First();
                    var c2 = m.Roster.Where(c => c.name == SelectedUnit2).First();
                    var c3 = m.Roster.Where(c => c.name == SelectedUnit3).First();
                    var c4 = m.Roster.Where(c => c.name == SelectedUnit4).First();
                    var c5 = m.Roster.Where(c => c.name == SelectedUnit5).First();

                    list.Add(new SquadLookupResult(c1, c2, c3, c4, c5){ Player = m.Name });
                }
            }

            SearchResults = list.OrderBy(x => x.TotalPower).Reverse().ToList();
            ResultCount = SearchResults.Count();
        }

        /// <summary> Command executed when the 'Search' button is pressed. </summary>
        public SimpleCommand SearchCommand { get; private set; }

        #region Squad presets

        /// <summary> List of preset squads. </summary>
        public List<SquadPreset> PresetsList { get; private set; } = new List<SquadPreset>();

        /// <summary> Whether the UI should show the presets menu. </summary>
        public bool ShowPresets { get; private set; } = false;

        /// <summary> Load presets from a CSV file on disk. </summary>
        private void TryLoadPresets()
        {
            PresetsList.Clear();

            // Look for CSV file in directory with the program
            string preset_filename = "presets.csv";
            string fn = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, preset_filename);
            if (!File.Exists(fn))
                return;

            foreach (string line in File.ReadLines(preset_filename))
            {
                // Remove comments
                string thisline = Regex.Replace(line, @"#.*^", "");

                if (string.IsNullOrWhiteSpace(thisline))
                    continue;

                // Extract fields
                var fields = thisline.Split(',').Select(f => f.Trim()).ToArray();
                if (fields.Length < 6)
                    continue;
                if (fields.Any(f => string.IsNullOrWhiteSpace(f)))
                    continue;

                // Add to preset list
                PresetsList.Add(new SquadPreset()
                {
                    Name = fields[0],
                    Character1 = fields[1],
                    Character2 = fields[2],
                    Character3 = fields[3],
                    Character4 = fields[4],
                    Character5 = fields[5]
                });
            }

            ShowPresets = PresetsList.Any();
        }

        /// <summary> Populate the UI with the preset with the specified name. </summary>
        public void LoadPreset(string name)
        {
            if (!PresetsList.Any(p => p.Name == name))
                return;

            var pr = PresetsList.Where(p => p.Name == name).First();

            // Override dropdowns with preset
            SelectedUnit1 = FindUnit(pr.Character1);
            SelectedUnit2 = FindUnit(pr.Character2);
            SelectedUnit3 = FindUnit(pr.Character3);
            SelectedUnit4 = FindUnit(pr.Character4);
            SelectedUnit5 = FindUnit(pr.Character5);
        }

        /// <summary> Finds a unit with a name similar to the provided name. </summary>
        /// <remarks>Does case-insensitive matching and works around non-ASCII characters.</remarks>
        /// <param name="name">Character name, or something pretty close.</param>
        /// <returns>Actual character name, or null if no match found.</returns>
        private string FindUnit(string name)
        {
            if (name == null || string.IsNullOrWhiteSpace(name))
                return null;

            // Look for a match, ignoring case
            foreach (var unit in Units)
            {
                if (unit.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return unit;
            }

            // Manually handle cases with weird characters
            if (name.StartsWith("Padm"))
                return Units.FirstOrDefault(u => u.StartsWith("Padm"));
            if (name.StartsWith("Chirrut"))
                return Units.FirstOrDefault(u => u.StartsWith("Chirrut"));

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Object describing one player's version of the currently-selected squad.
    /// </summary>
    public class SquadLookupResult
    {
        /// <summary> Player name. </summary>
        public string Player { get; set; }
        /// <summary> Total squad power. </summary>
        public long TotalPower { get; set; }
        public SquadLookupUnit Unit1 { get; set; }
        public SquadLookupUnit Unit2 { get; set; }
        public SquadLookupUnit Unit3 { get; set; }
        public SquadLookupUnit Unit4 { get; set; }
        public SquadLookupUnit Unit5 { get; set; }

        public SquadLookupResult() { }
        public SquadLookupResult(Character c1, Character c2, Character c3, Character c4, Character c5)
        {
            Unit1 = new SquadLookupUnit(c1);
            Unit2 = new SquadLookupUnit(c2);
            Unit3 = new SquadLookupUnit(c3);
            Unit4 = new SquadLookupUnit(c4);
            Unit5 = new SquadLookupUnit(c5);

            TotalPower = Unit1.Power + Unit2.Power + Unit3.Power + Unit4.Power + Unit5.Power;
        }
    }

    /// <summary>
    /// Stats for a single unit in a squad.
    /// </summary>
    public class SquadLookupUnit
    {
        public int Level { get; set; }
        public int Stars { get; set; }
        public int GearLevel { get; set; }
        public long Power { get; set; }

        public SquadLookupUnit() { }
        public SquadLookupUnit(Character c)
        {
            Level = c.level;
            GearLevel = c.gear;
            Stars = c.rarity;
            Power = c.TruePower;
        }
    }

    /// <summary>
    /// Pre-defined squad setup.
    /// </summary>
    public class SquadPreset
    {
        public string Name { get; set; }
        public string Character1 { get; set; }
        public string Character2 { get; set; }
        public string Character3 { get; set; }
        public string Character4 { get; set; }
        public string Character5 { get; set; }
        public SquadPreset() { }
        public SquadPreset(string name, string c1, string c2, string c3, string c4, string c5)
        {
            Name = name;
            Character1 = c1;
            Character2 = c2;
            Character3 = c3;
            Character4 = c4;
            Character5 = c5;
        }
    }
}
