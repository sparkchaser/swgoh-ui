using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

        public UnitLookupViewmodel(PlayerList members, List<UnitDetails> unitDetails)
        {
            Members = members ?? throw new ArgumentNullException("members");
            UnitDetails = unitDetails;

            if (UnitDetails != null && UnitDetails.Count > 0)
            {
                FilterVisible = true;
                Filters = new List<string> { "All" };
                Filters.AddRange(unitDetails.Select(u => u.categoryIdList).SelectMany(x => x).Distinct().OrderBy(x => x));
                SelectedFilter = Filters.First();
            }
            else
            {
                FilterVisible = false;
            }

            RebuildUnitList();
        }


        /// <summary> Data for each guild member. </summary>
        public PlayerList Members { get; private set; }

        /// <summary> List of detailed metadata for all defined units. </summary>
        public List<UnitDetails> UnitDetails { get; private set; }

        /// <summary> List of all supported unit filters. </summary>
        public List<string> Filters { get; private set; }

        /// <summary> Rebuild unit list when roster changes. </summary>
        private void RebuildUnitList()
        {
            if (Members == null)
            {
                FilteredUnits = null;
                return;
            }

            // Use UnitDetails if available, since it can contain units not found
            // in any player's roster.
            var list = new List<string>();
            if (UnitDetails != null && UnitDetails.Count > 0)
            {
                if (SelectedFilter == "All")
                    list = UnitDetails.Select(u => u.name).ToList();
                else
                {
                    list = UnitDetails.Where(u => u.categoryIdList.Contains(SelectedFilter))
                                      .Select(u => u.name)
                                      .ToList();
                }
            }
            else
            {
                // If UnitDetails not available, derive a list by combining the members' rosters.
                list = Members.Select(m => m.Roster.Select(c => c.name).ToArray()) // map each member to an array of unit names
                              .ToArray()
                              .SelectMany(x => x) // flatten list
                              .Distinct()         // de-duplicate
                              .ToList();
            }
            list.Sort();
            FilteredUnits = list;
        }


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

        /// <summary> List of units, filtered based on current selection. </summary>
        public List<string> FilteredUnits
        {
            get { return (List<string>)GetValue(FilteredUnitsProperty); }
            set { SetValue(FilteredUnitsProperty, value); }
        }
        public static readonly DependencyProperty FilteredUnitsProperty = _dp<List<string>>("FilteredUnits");

        /// <summary> Currently-selected unit filter. </summary>
        public string SelectedFilter
        {
            get { return (string)GetValue(SelectedFilterProperty); }
            set { SetValue(SelectedFilterProperty, value); }
        }
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register("SelectedFilter", typeof(string), typeof(UnitLookupViewmodel),
                                        new PropertyMetadata(null, SelectedFilterChanged));
        public static void SelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as UnitLookupViewmodel).RebuildUnitList();

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
