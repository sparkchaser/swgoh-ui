using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class PlayerDetailViewmodel : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(PlayerDetailViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(PlayerDetailViewmodel));
        }


        public PlayerDetailViewmodel()
        {
        }


        #region XAML bind-able properties

        public Player Player
        {
            get { return (Player)GetValue(PlayerProperty); }
            set { SetValue(PlayerProperty, value); }
        }
        public static readonly DependencyProperty PlayerProperty =
            DependencyProperty.Register("Player", typeof(Player), typeof(PlayerDetailViewmodel), new PropertyMetadata(null, PlayerChanged));

        private static void PlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is PlayerDetailViewmodel vm))
                return;
            // When Player gets updated, re-generate the derived roster views
            vm.Ships = vm.Player.Roster.Where(x => x.combatType == "SHIP").ToList();
            vm.Characters = vm.Player.Roster.Where(x => x.combatType == "CHARACTER").ToList();
            vm.WindowTitle = $"Player Details - {vm.Player.Name}";

            // Build list of zetas
            var list = new List<Tuple<string, string>>();
            foreach (var c in vm.Player.Roster.Where(c => c.NumZetas > 0))
            {
                foreach (var zeta in c.Zetas)
                {
                    list.Add(new Tuple<string, string>(c.name, zeta));
                }
            }
            vm.ZetaList = list;

            // Build stats list
            var stats = new List<PlayerStat>();
            foreach (var s in vm.Player.Stats)
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
            vm.StatsList = stats;
        }

        /// <summary> Characters in the player's roster. </summary>
        public List<Character> Characters
        {
            get { return (List<Character>)GetValue(CharactersProperty); }
            set { SetValue(CharactersProperty, value); }
        }
        public static readonly DependencyProperty CharactersProperty = _dp<List<Character>>("Characters");

        /// <summary> Ships in the player's roster. </summary>
        public List<Character> Ships
        {
            get { return (List<Character>)GetValue(ShipsProperty); }
            set { SetValue(ShipsProperty, value); }
        }
        public static readonly DependencyProperty ShipsProperty = _dp<List<Character>>("Ships");

        /// <summary> Dynamic window title. </summary>
        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }
        public static readonly DependencyProperty WindowTitleProperty = _dp<string>("WindowTitle", "Player Details");

        /// <summary> List of zetas this player has unlocked. </summary>
        public List<Tuple<string,string>> ZetaList
        {
            get { return (List<Tuple<string, string>>)GetValue(ZetaListProperty); }
            set { SetValue(ZetaListProperty, value); }
        }
        public static readonly DependencyProperty ZetaListProperty = _dp<List<Tuple<string, string>>>("ZetaList");

        /// <summary> List of player statistics. </summary>
        public List<PlayerStat> StatsList
        {
            get { return (List<PlayerStat>)GetValue(StatsListProperty); }
            set { SetValue(StatsListProperty, value); }
        }
        public static readonly DependencyProperty StatsListProperty = _dp<List<PlayerStat>>("StatsList");



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
