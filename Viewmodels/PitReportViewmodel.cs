using goh_ui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace goh_ui.Viewmodels
{
    public class PitReportViewmodel : DependencyObject
    {
        public PitReportViewmodel(GuildInfo guild, PlayerList members)
        {
            if (guild == null)
                throw new ArgumentNullException("guild");
            if (members == null)
                throw new ArgumentNullException("members");
            Members = FilterUnits(members);

            GuildName = guild.name;

            NumEligibleMessage = $"{Members.Count}/{guild.members} members eligible";

            // Measure length of longest player/unit name
            float namelen = Members.Select(m => StringPixelLength(m.Name)).Max();
            NameColumnWidth = (int)(namelen + Math.Min(namelen * 0.1, 10));
            namelen = Members.SelectMany(m => m.Roster.Select(u => u.name))
                             .Distinct()
                             .Select(unit => StringPixelLength(unit))
                             .Max();
            UnitColumnWidth = (int)(namelen + Math.Min(namelen * 0.1, 10));

            // Build reverse unit lookup table
            var unitdict = new Dictionary<string, List<string>>();
            foreach (var m in Members)
            {
                foreach (var unit in m.Roster)
                {
                    if (unitdict.ContainsKey(unit.name))
                        unitdict[unit.name].Add(m.Name);
                    else
                        unitdict.Add(unit.name, new List<string> { m.Name });
                }
            }
            ReverseUnitLookup = new List<Tuple<string, string>>();
            foreach (var k in unitdict.Keys.OrderBy(x => x))
            {
                ReverseUnitLookup.Add(new Tuple<string, string>(k, string.Join("\n", unitdict[k].OrderBy(x => x).ToArray())));
            }
        }

        /// <summary> Compute the width (px) of a string in the default UI font. </summary>
        private float StringPixelLength(string s)
        {
            using (Graphics graphics = Graphics.FromImage(new Bitmap(1, 1)))
            {
                return graphics.MeasureString(s, new Font("Segoe UI", 11, System.Drawing.FontStyle.Regular, GraphicsUnit.Point)).Width;
            }
        }

        #region Public members

        /// <summary> Name of the current guild. </summary>
        public string GuildName { get; private set; }

        /// <summary> Message indicating how many guild members have eligible characters. </summary>
        public string NumEligibleMessage { get; private set; }

        /// <summary> Data for each guild member. </summary>
        public ObservableCollection<PitPlayer> Members { get; private set; }

        /// <summary> Width to use for the 'Name' column on the 'by player' tab. </summary>
        public int NameColumnWidth { get; private set; }

        /// <summary> Width to use for the 'Unit' column on the 'by character' tab. </summary>
        public int UnitColumnWidth { get; private set; }

        /// <summary> Character roster sorted by unit. </summary>
        public List<Tuple<string, string>> ReverseUnitLookup { get; private set; }

        #endregion

        /// <summary> Filter down a guild roster to only members/characters eligible for challenge-mode Pit raid. </summary>
        private ObservableCollection<PitPlayer> FilterUnits(PlayerList members)
        {
            ObservableCollection<PitPlayer> roster = new ObservableCollection<PitPlayer>();

            foreach (Player player in members)
            {
                var eligible = player.Roster.Where(c => c.GearLevelSortable >= 13.5m);
                if (eligible.Count() <= 0)
                    continue;
                
                // Create a duplicate Player object using the slimmed-down roster
                PlayerInfo pi = new PlayerInfo()
                {
                    // Only copy over the stats that we'll need to use
                    name = player.Name,
                    level = player.Level,
                    allyCode = (int)player.AllyCode.Value,
                    stats = player.Stats.ToArray(),
                    roster = eligible.ToArray()
                };

                // Re-compute power for new roster
                pi.gpChar = (int)pi.roster.Select(c => c.TruePower).Sum();
                pi.gpShip = 0;
                pi.gpFull = pi.gpChar;
                var stat = pi.stats.Where(s => s.name == "Galactic Power:").FirstOrDefault();
                if (stat != null)
                    stat.value = pi.gpFull;
                stat = pi.stats.Where(s => s.name == "Galactic Power (Ships):").FirstOrDefault();
                if (stat != null)
                    stat.value = pi.gpShip;
                stat = pi.stats.Where(s => s.name == "Galactic Power (Characters):").FirstOrDefault();
                if (stat != null)
                    stat.value = pi.gpChar;

                // Add this player to the roster
                PitPlayer this_player = new PitPlayer(pi);
                roster.Add(this_player);
            }

            return roster;
        }
    }

    public class PitPlayer : Player
    {
        public PitPlayer(PlayerInfo pi) : base(pi)
        {
            NumR5 = Roster.Where(c => c.GearLevelSortable >= 13.5m).Count();
            NumR7 = Roster.Where(c => c.GearLevelSortable >= 13.7m).Count();
        }

        /// <summary> Number of characters this player has at gear level relic 5 or higher. </summary>
        public int NumR5 { get; private set; }
        /// <summary> Number of characters this player has at gear level relic 7 or higher. </summary>
        public int NumR7 { get; private set; }
    }

    /// <summary>
    /// Generate a list of unit names from a roster.
    /// </summary>
    public class RosterSummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is List<Character> roster))
                return "";
            return string.Join("\n", roster.Select(c => c.name).OrderBy(n => n));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
