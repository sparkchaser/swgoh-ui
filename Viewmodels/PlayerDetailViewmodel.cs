using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class PlayerDetailViewmodel : DependencyObject
    {

        public PlayerDetailViewmodel(Player p)
        {
            Player = p;
            PlayerUpdated();
        }


        /// <summary> Update derived fields after Player is changed. </summary>
        private void PlayerUpdated()
        {
            if (Player == null)
            {
                Ships = null;
                Characters = null;
                ZetaList = null;
                StatsList = null;
                WindowTitle = "Player Details";
                return;
            }

            // When Player gets updated, re-generate the derived roster views
            Ships = Player.Roster.Where(x => x.combatType == Character.COMBATTYPE_SHIP).ToList();
            Characters = Player.Roster.Where(x => x.combatType == Character.COMBATTYPE_CHARACTER).ToList();
            WindowTitle = $"Player Details - {Player.Name}";

            // Build list of zetas
            var list = new List<Tuple<string, string>>();
            foreach (var c in Player.Roster.Where(c => c.NumZetas > 0))
            {
                foreach (var zeta in c.Zetas)
                {
                    list.Add(new Tuple<string, string>(c.name, zeta));
                }
            }
            ZetaList = list;

            // Build stats list
            var stats = new List<PlayerStat>();
            foreach (var s in Player.Stats)
            {
                var this_stat = new PlayerStat
                {
                    Description = s.name,
                    Value = $"{s.value:N0}"
                };
                if (this_stat.Description.Contains("Championship Best Rank Achieved"))
                {
                    // Decode the championship rank value
                    // Bits 0-31 are the numerical rank.
                    int score = (int)(s.value & 0xFFFFFFFF);
                    // Bits 32-40 are the division.  (12 - division) * 5 = value
                    int div = (int)((s.value >> 32) & 0xFF);
                    div = 12 - (div / 5);
                    // Bits 41-48 are the league.  [divide value by 20]
                    int league = (int)((s.value >> 40) & 0xFF);
                    Dictionary<int, string> leagues = new Dictionary<int, string>()
                    {
                        { 20, "Carbonite" },
                        { 40, "Bronzium" },
                        { 60, "Chromium" },
                        { 80, "Aurodium" },
                        { 100, "Khyber" }
                    };
                    string league_s = "Unknown";
                    if (leagues.ContainsKey(league))
                        league_s = leagues[league];
                    // Build string from values
                    this_stat.Value = $"Division {div} / {league_s} / {score:N0}";
                }
                stats.Add(this_stat);
            }
            StatsList = stats;
        }

        
        #region Public properties

        public Player Player { get; private set; }

        /// <summary> Characters in the player's roster. </summary>
        public List<Character> Characters { get; set; }

        /// <summary> Ships in the player's roster. </summary>
        public List<Character> Ships { get; set; }

        /// <summary> Dynamic window title. </summary>
        public string WindowTitle { get; set; }

        /// <summary> List of zetas this player has unlocked. </summary>
        public List<Tuple<string, string>> ZetaList { get; set; }

        /// <summary> List of player statistics. </summary>
        public List<PlayerStat> StatsList { get; set; }

        #endregion
    }

    /// <summary>
    /// A player statistic (description and value).
    /// </summary>
    public class PlayerStat
    {
        public string Description { get; set; }
        public string Value { get; set; }
    }
}
