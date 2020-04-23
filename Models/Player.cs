using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace goh_ui.Models
{
    /// <summary>
    /// Player details (UI-friendly version).
    /// </summary>
    public class Player : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(Player), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(Player));
        }

        #region XAML bind-able properties

        /// <summary> Player name </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
        public static readonly DependencyProperty NameProperty = _dp<string>("Name");

        /// <summary> Player level. </summary>
        public int Level
        {
            get { return (int)GetValue(LevelProperty); }
            set { SetValue(LevelProperty, value); }
        }
        public static readonly DependencyProperty LevelProperty = _dp<int>("Level");

        /// <summary> Ally code. </summary>
        public AllyCode AllyCode
        {
            get { return (AllyCode)GetValue(AllyCodeProperty); }
            set { SetValue(AllyCodeProperty, value); }
        }
        public static readonly DependencyProperty AllyCodeProperty = _dp<AllyCode>("AllyCode");

        /// <summary> Galactic power. </summary>
        public long Power
        {
            get { return (long)GetValue(PowerProperty); }
            set { SetValue(PowerProperty, value); }
        }
        public static readonly DependencyProperty PowerProperty = _dp<long>("Power");

        /// <summary> Galactic power (ships only). </summary>
        public long ShipPower
        {
            get { return (long)GetValue(ShipPowerProperty); }
            set { SetValue(ShipPowerProperty, value); }
        }
        public static readonly DependencyProperty ShipPowerProperty = _dp<long>("ShipPower");

        /// <summary> Galactic power (characters only). </summary>
        public long CharacterPower
        {
            get { return (long)GetValue(CharacterPowerProperty); }
            set { SetValue(CharacterPowerProperty, value); }
        }
        public static readonly DependencyProperty CharacterPowerProperty = _dp<long>("CharacterPower");

        /// <summary> Galactic power for characters meaningful to TW. </summary>
        public long MeaningfulPower
        {
            get { return (long)GetValue(MeaningfulPowerProperty); }
            set { SetValue(MeaningfulPowerProperty, value); }
        }
        public static readonly DependencyProperty MeaningfulPowerProperty = _dp<long>("MeaningfulPower");

        /// <summary> Index measuring how well the total GP indicates usefulness in TW. </summary>
        public double TwEfficiency
        {
            get { return (double)GetValue(TwEfficiencyProperty); }
            set { SetValue(TwEfficiencyProperty, value); }
        }
        public static readonly DependencyProperty TwEfficiencyProperty = _dp<double>("TwEfficiency");



        #endregion

        #region Non-bindable public members

        /// <summary> Complete list of unlocked characters. </summary>
        public List<Character> Roster { get; set; }

        /// <summary> Total number of zeta abilities this player has unlocked. </summary>
        public int NumZetas => Roster.Select(c => c.NumZetas).Sum();

        /// <summary> List of player statistics. </summary>
        public PlayerStat[] Stats { get; set; }

        /// <summary> Currently-selected title. </summary>
        public string CurrentTitle { get; set; } = "";

        #endregion


        public Player(PlayerInfo pi)
        {
            // Copy static values
            Name = pi.name;
            Level = pi.level;
            AllyCode = new AllyCode((uint)pi.allyCode);
            Power = pi.stats.Where(s => s.name == "Galactic Power:").FirstOrDefault().value;
            ShipPower = pi.stats.Where(s => s.name == "Galactic Power (Ships):").FirstOrDefault().value;
            CharacterPower = pi.stats.Where(s => s.name == "Galactic Power (Characters):").FirstOrDefault().value;
            Stats = pi.stats;

            // Copy roster
            Roster = new List<Character>(pi.roster);

            // Compute derived properties
            CalculateProperties();
        }


        /// <summary> Calculate properties based on this character's roster. </summary>
        private void CalculateProperties()
        {
            if (Roster == null || Roster.Count == 0)
                return;

            // Filter down to what's usable in TW
            var relevant = Roster.Where(c =>
            {
                return c.combatType == Character.COMBATTYPE_CHARACTER &&
                       c.level >= 85 &&
                       c.rarity >= 7 &&
                       c.gear >= 10 &&
                       c.gp >= 13_000 &&
                       UselessCharacters.Contains(c.defId) == false;
            });

            MeaningfulPower = relevant.Select(c => c.gp).Sum();

            TwEfficiency = Math.Round(((double)MeaningfulPower / Power) * 100.0, 2);
        }

        /// <summary> List of characters that serve no point whatsoever. </summary>
        private readonly List<string> UselessCharacters = new List<string>()
        {
            "CORUSCANTUNDERWORLDPOLICE",
            "JEDIKNIGHTGUARDIAN",
            "TUSKENRAIDER",
            "TUSKENSHAMAN",
            "URORRURRR",
            "UGNAUGHT",
            "GARSAXON",
            "IMPERIALSUPERCOMMANDO",
            "LOBOT",
            "BODHIROOK",
            "HUMANTHUG",
            "ROSETICO",
            "EETHKOTH",
            "KITFISTO"
        };
    }
}
