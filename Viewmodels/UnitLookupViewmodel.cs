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

        public UnitLookupViewmodel(PlayerList members)
        {
            Members = members ?? throw new ArgumentNullException("members");
            RebuildUnitList();
        }


        /// <summary> Data for each guild member. </summary>
        public PlayerList Members { get; private set; }

        /// <summary> List of all known units. </summary>
        public List<string> Units { get; private set; }

        /// <summary> Rebuild unit list when roster changes. </summary>
        private void RebuildUnitList()
        {
            if (Members == null)
            {
                Units = null;
                return;
            }

            var dict = new List<string>();
            foreach (var member in Members)
            {
                foreach (var unit in member.Roster.Where(c => c.combatType == Character.COMBATTYPE_CHARACTER && !dict.Contains(c.name)))
                {
                    dict.Add(unit.name);
                }
            }
            dict.Sort();
            Units = dict;
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
