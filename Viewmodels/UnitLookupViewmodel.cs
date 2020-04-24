using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace goh_ui.Viewmodels
{
    public class UnitLookupViewmodel : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(UnitLookupViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(UnitLookupViewmodel));
        }


        /// <summary> Data for each guild member. </summary>
        private PlayerList Members { get; set; }

        /// <summary> List of detailed metadata for all defined units. </summary>
        private IEnumerable<UnitDetails> UnitDetails { get; set; }


        public UnitLookupViewmodel(PlayerList members, IEnumerable<UnitDetails> unitDetails)
        {
            Members = members ?? throw new ArgumentNullException("members");
            UnitDetails = unitDetails;

            AllUnits = new ObservableCollection<string>();

            if (UnitDetails != null && UnitDetails.Count() > 0)
            {
                FilterVisible = true;
                foreach (var unitname in unitDetails.Select(u => u.name).OrderBy(x => x))
                    AllUnits.Add(unitname);
            }
            else
            {
                FilterVisible = false;
                foreach (var unitname in members.Select(p => p.Roster.Select(c => c.name)).SelectMany(x => x).Distinct().OrderBy(x => x))
                    AllUnits.Add(unitname);
            }

            UnitSource = new CollectionViewSource { Source = AllUnits };
            UnitSource.Filter += FilterUnits;
            
            BuildFilterList();
            SelectedFilter = Filters.FirstOrDefault();
        }


        /// <summary> Filtered list of units. </summary>
        public CollectionViewSource UnitSource { get; private set; }

        /// <summary> Complete list of all known units. </summary>
        public ObservableCollection<string> AllUnits { get; private set; }

        /// <summary> List of all supported unit filters. </summary>
        public List<string> Filters { get; private set; } = new List<string>();

        #region Dependency properties

        /// <summary> Currently-selected unit. </summary>
        public string SelectedUnit
        {
            get { return (string)GetValue(SelectedUnitProperty); }
            set { SetValue(SelectedUnitProperty, value); }
        }
        public static readonly DependencyProperty SelectedUnitProperty =
            DependencyProperty.Register("SelectedUnit", typeof(string), typeof(UnitLookupViewmodel),
                                        new PropertyMetadata(null, SelectedUnitChanged));
        public static void SelectedUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = d as UnitLookupViewmodel;
            var selected = (string)e.NewValue;

            var list = new List<UnitLookupResult>();
            foreach (var member in vm.Members)
            {
                var matches = member.Roster.Where(c => c.name == selected);
                if (matches.Any())
                {
                    var c = matches.First();
                    list.Add(new UnitLookupResult(member.Name, c));
                }
            }
            vm.ResultsList = list.OrderBy(x => x.Power).Reverse().ToList();
            vm.ResultCount = vm.ResultsList.Count();
        }

        /// <summary> Live query results for the currently-selected character. </summary>
        public List<UnitLookupResult> ResultsList
        {
            get { return (List<UnitLookupResult>)GetValue(ResultsListProperty); }
            set { SetValue(ResultsListProperty, value); }
        }
        public static readonly DependencyProperty ResultsListProperty = _dp<List<UnitLookupResult>>("ResultsList");

        /// <summary> Number of search results in the latest query. </summary>
        public int ResultCount
        {
            get { return (int)GetValue(ResultCountProperty); }
            set { SetValue(ResultCountProperty, value); }
        }
        public static readonly DependencyProperty ResultCountProperty = _dp<int>("ResultCount");

        /// <summary> Whether or not the unit filter should be visible. </summary>
        public bool FilterVisible
        {
            get { return (bool)GetValue(FilterVisibleProperty); }
            set { SetValue(FilterVisibleProperty, value); }
        }
        public static readonly DependencyProperty FilterVisibleProperty = _dp<bool>("FilterVisible");

        /// <summary> Currently-selected unit filter. </summary>
        public string SelectedFilter
        {
            get { return (string)GetValue(SelectedFilterProperty); }
            set { SetValue(SelectedFilterProperty, value); }
        }
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register("SelectedFilter", typeof(string), typeof(UnitLookupViewmodel),
                                        new PropertyMetadata(null, SelectedFilterChanged));
        #endregion

        #region Unit filtering

        /// <summary> Whenever the selected filter changes, signal the view to refresh the list. </summary>
        private static void SelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as UnitLookupViewmodel).UnitSource.View.Refresh();
        }

        /// <summary> Rebuild the list of available filters. </summary>
        private void BuildFilterList()
        {
            Filters.Clear();

            if (UnitDetails == null || UnitDetails.Count() == 0)
                return;

            // Add a few generic filters at the top of the list.
            Filters.Add("All");
            Filters.Add("Characters");
            Filters.Add("Ships");
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
                case "Characters": e.Accepted = unit.combatType == goh_ui.UnitDetails.COMBATTYPE_CHARACTER; break;
                case "Ships": e.Accepted = unit.combatType == goh_ui.UnitDetails.COMBATTYPE_SHIP; break;
                case "Light Side": e.Accepted = unit.forceAlignment == goh_ui.UnitDetails.ALIGNMENT_LIGHT; break;
                case "Dark Side": e.Accepted = unit.forceAlignment == goh_ui.UnitDetails.ALIGNMENT_DARK; break;
                // Handle tag-based filters
                default: e.Accepted = unit.categoryIdList.Contains(SelectedFilter); break;
            }
        }

        #endregion
    }
    
    /// <summary>
    /// Object describing one player's version of the currently-selected character.
    /// </summary>
    public class UnitLookupResult
    {
        public string Player { get; private set; }
        public int Level => _char.level;
        public int Stars => _char.rarity;
        public int GearLevel => _char.gear;
        public long Power => _char.TruePower;
        public decimal GearLevelSortable => _char.GearLevelSortable;
        public string GearLevelDescriptive => _char.GearLevelDescriptive;

        private readonly Character _char;

        public UnitLookupResult(string name, Character character)
        {
            Player = name;
            _char = character;
        }
    }
}
