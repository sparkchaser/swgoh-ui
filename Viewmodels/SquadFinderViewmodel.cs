using goh_ui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

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


        /// <summary> List of detailed metadata for all defined units. </summary>
        private IEnumerable<UnitDetails> UnitDetails { get; set; }

        /// <summary> Data for each guild member. </summary>
        private PlayerList Members { get; set; }


        public SquadFinderViewmodel(PlayerList members, IEnumerable<UnitDetails> unitDetails)
        {
            Members = members ?? throw new ArgumentNullException("members");
            UnitDetails = unitDetails;

            ShowFilter = UnitDetails != null;

            SearchCommand = new SimpleCommand(DoSearch);

            BuildUnitList();
            UnitSource1 = new CollectionViewSource { Source = Units };
            UnitSource1.Filter += FilterUnits;
            UnitSource2 = new CollectionViewSource { Source = Units };
            UnitSource2.Filter += FilterUnits;
            UnitSource3 = new CollectionViewSource { Source = Units };
            UnitSource3.Filter += FilterUnits;
            UnitSource4 = new CollectionViewSource { Source = Units };
            UnitSource4.Filter += FilterUnits;
            UnitSource5 = new CollectionViewSource { Source = Units };
            UnitSource5.Filter += FilterUnits;

            BuildFilterList();
            SelectedFilter = Filters.FirstOrDefault();

            PresetsList = SquadPreset.LoadPresets();
            ShowPresets = PresetsList.Any();
        }

        // Filtered lists of units
        public CollectionViewSource UnitSource1 { get; private set; }
        public CollectionViewSource UnitSource2 { get; private set; }
        public CollectionViewSource UnitSource3 { get; private set; }
        public CollectionViewSource UnitSource4 { get; private set; }
        public CollectionViewSource UnitSource5 { get; private set; }

        /// <summary> List of all known units. </summary>
        public ObservableCollection<string> Units { get; private set; }

        /// <summary> List of all supported unit filters. </summary>
        public List<string> Filters { get; private set; } = new List<string>();

        /// <summary> Whether or not the filter dropdown should be displayed. </summary>
        public bool ShowFilter { get; private set; }


        /// <summary> Rebuild unit list. </summary>
        public void BuildUnitList()
        {
            if (Members == null)
            {
                Units = null;
                return;
            }

            Units = new ObservableCollection<string>();

            // Use UnitDetails if available, since it can contain units not found
            // in any player's roster.
            if (UnitDetails != null && UnitDetails.Count() > 0)
            {
                foreach (var name in UnitDetails.Where(u => u.combatType == goh_ui.UnitDetails.COMBATTYPE_CHARACTER).Select(u => u.name).OrderBy(x => x))
                    Units.Add(name);
                return;
            }

            // If UnitDetails not available, derive a list by combining the members' rosters.
            var units = Members.Select(m => m.Roster.Where(u => u.combatType == goh_ui.UnitDetails.COMBATTYPE_CHARACTER)
                               .Select(c => c.name)) // map each member to an array of unit names
                               .SelectMany(x => x)   // flatten list
                               .Distinct()           // de-duplicate
                               .OrderBy(x => x);     // sort alphabetically
            foreach (var unit in units)
                Units.Add(unit);
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

        /// <summary> Currently-selected unit filter. </summary>
        public string SelectedFilter
        {
            get { return (string)GetValue(SelectedFilterProperty); }
            set { SetValue(SelectedFilterProperty, value); }
        }
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register("SelectedFilter", typeof(string), typeof(SquadFinderViewmodel),
                                        new PropertyMetadata(null, SelectedFilterChanged));

        #endregion

        #region Unit list filtering

        /// <summary> Whenever the selected filter changes, signal the view to refresh the list. </summary>
        private static void SelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = d as SquadFinderViewmodel;
            vm.UnitSource1.View.Refresh();
            vm.UnitSource2.View.Refresh();
            vm.UnitSource3.View.Refresh();
            vm.UnitSource4.View.Refresh();
            vm.UnitSource5.View.Refresh();
        }

        /// <summary> Rebuild the list of available filters. </summary>
        private void BuildFilterList()
        {
            Filters.Clear();

            if (UnitDetails == null || UnitDetails.Count() == 0)
                return;

            // Add a few generic filters at the top of the list.
            Filters.Add("All");
            Filters.Add("Light Side");
            Filters.Add("Dark Side");

            // Add filters based on unit tags
            Filters.AddRange(UnitDetails.Select(u => u.categoryIdList).SelectMany(x => x).Distinct().OrderBy(x => x));
        }

        /// <summary> Unit filtering logic. </summary>
        private void FilterUnits(object sender, FilterEventArgs e)
        {
            // If filters aren't supported, return everything
            if (UnitDetails == null || UnitDetails.Count() == 0)
            {
                e.Accepted = true;
                return;
            }

            // Look up character metadata
            var character = e.Item as string;
            var unit = UnitDetails.Where(u => u.name == character).FirstOrDefault();
            if (unit == null)
            {
                e.Accepted = false;
                return;
            }

            // Apply selected filter
            switch (SelectedFilter)
            {
                // Handle generic filters
                case "All": e.Accepted = true; break;
                case "Light Side": e.Accepted = unit.forceAlignment == goh_ui.UnitDetails.ALIGNMENT_LIGHT; break;
                case "Dark Side": e.Accepted = unit.forceAlignment == goh_ui.UnitDetails.ALIGNMENT_DARK; break;
                // Handle tag-based filters
                default: e.Accepted = unit.categoryIdList.Contains(SelectedFilter); break;
            }
        }

        #endregion

        #region Squad searching

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

        #endregion

        #region Squad presets

        /// <summary> List of preset squads. </summary>
        public List<SquadPreset> PresetsList { get; private set; } = new List<SquadPreset>();

        /// <summary> Whether the UI should show the presets menu. </summary>
        public bool ShowPresets { get; private set; } = false;

        /// <summary> Populate the UI with the preset with the specified name. </summary>
        public void LoadPreset(string name)
        {
            if (!PresetsList.Any(p => p.Name == name))
                return;

            var pr = PresetsList.Where(p => p.Name == name).First();

            // Must clear any filters or units might not be selectable
            if (ShowFilter)
                SelectedFilter = Filters.First();

            // Override dropdowns with preset
            SelectedUnit1 = FindUnit(pr.Character1, Units);
            SelectedUnit2 = FindUnit(pr.Character2, Units);
            SelectedUnit3 = FindUnit(pr.Character3, Units);
            SelectedUnit4 = FindUnit(pr.Character4, Units);
            SelectedUnit5 = FindUnit(pr.Character5, Units);
        }

        /// <summary> Finds a unit with a name similar to the provided name. </summary>
        /// <remarks>Does case-insensitive matching and works around non-ASCII characters.</remarks>
        /// <param name="name">Character name, or something pretty close.</param>
        /// <param name="units">List of unit names to select from.</param>
        /// <returns>Actual character name, or null if no match found.</returns>
        internal static string FindUnit(string name, IEnumerable<string> units)
        {
            if (name == null || string.IsNullOrWhiteSpace(name))
                return null;

            // Look for a match, ignoring case
            foreach (var unit in units)
            {
                if (unit.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return unit;
            }

            // Manually handle cases with weird characters
            if (name.StartsWith("Padm"))
                return units.FirstOrDefault(u => u.StartsWith("Padm"));
            if (name.StartsWith("Chirrut"))
                return units.FirstOrDefault(u => u.StartsWith("Chirrut"));

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
        public Character Unit1 { get; set; }
        public Character Unit2 { get; set; }
        public Character Unit3 { get; set; }
        public Character Unit4 { get; set; }
        public Character Unit5 { get; set; }

        public SquadLookupResult() { }
        public SquadLookupResult(Character c1, Character c2, Character c3, Character c4, Character c5)
        {
            Unit1 = c1;
            Unit2 = c2;
            Unit3 = c3;
            Unit4 = c4;
            Unit5 = c5;

            TotalPower = Unit1.TruePower + Unit2.TruePower + Unit3.TruePower + Unit4.TruePower + Unit5.TruePower;
        }
    }
}
