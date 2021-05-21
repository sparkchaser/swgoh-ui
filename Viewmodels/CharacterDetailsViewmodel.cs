using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;

namespace goh_ui.Viewmodels
{
    public class CharacterDetailsViewmodel
    {
        #region Constructors

        public CharacterDetailsViewmodel()
        {

        }

        public CharacterDetailsViewmodel(Character c)
        {
            character = c ?? throw new ArgumentNullException("c");
            PopulateFields();
        }

        #endregion

        /// <summary> Character/ship that we're currently displaying. </summary>
        protected Character character;


        #region UI Properties

        /// <summary> Character name. </summary>
        public string Name => character.name;

        /// <summary> Character level. </summary>
        public int Level => character.level;

        /// <summary> Star level. </summary>
        public int Stars => character.rarity;

        /// <summary> Galactic power. </summary>
        public long Power => character.TruePower;


        /// <summary> Star level, as displayable string. </summary>
        public string DisplayStars { get; private set; }


        /// <summary> List of abilities with unlocked zetas. </summary>
        public List<string> Zetas => character.Zetas;

        /// <summary> Whether or not this character has any zetas unlocked. </summary>
        public bool HasZetas => character.NumZetas != 0;


        /// <summary> Whether or not this character is a normal character (non-ship). </summary>
        public bool IsChar => character.combatType == Character.COMBATTYPE_CHARACTER;

        /// <summary> Human-readable description of the character's gear level. </summary>
        public string GearLevel { get; private set; }
        /// <summary> Color to use for displaying the gear level. </summary>
        public Brush GearLevelColor { get; private set; }

        public bool Gearslot1Filled { get; private set; }
        public bool Gearslot2Filled { get; private set; }
        public bool Gearslot3Filled { get; private set; }
        public bool Gearslot4Filled { get; private set; }
        public bool Gearslot5Filled { get; private set; }
        public bool Gearslot6Filled { get; private set; }

        /// <summary> Which mod sets this character is using (complete sets with bonus activated). </summary>
        public string ModSets { get; private set; }

        #endregion

        /// <summary> Process the currently-selected character, populating all of this object's derived properties. </summary>
        protected void PopulateFields()
        {
            if (character == null)
                return;

            if (IsChar)
            {
                // Determine which gear slots are filled
                Gearslot1Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_TOP_LEFT && !string.IsNullOrWhiteSpace(g.equipmentId));
                Gearslot2Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_MID_LEFT && !string.IsNullOrWhiteSpace(g.equipmentId));
                Gearslot3Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_BOT_LEFT && !string.IsNullOrWhiteSpace(g.equipmentId));
                Gearslot4Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_TOP_RIGHT && !string.IsNullOrWhiteSpace(g.equipmentId));
                Gearslot5Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_MID_RIGHT && !string.IsNullOrWhiteSpace(g.equipmentId));
                Gearslot6Filled = character.equipped.Any(g => g.slot == Equipped.SLOT_BOT_RIGHT && !string.IsNullOrWhiteSpace(g.equipmentId));

                // Convert gear level into a number/color
                string[] roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII" };
                if (character.gear <= roman.Length && character.relic != null && character.relic.currentTier <= 2)
                {
                    GearLevel = $"Gear Level {roman[character.gear - 1]}";
                }
                else if (character.relic != null)
                {
                    GearLevel = $"Gear Level XIII Relic {character.relic.currentTier - 2}";
                }
                else
                {
                    GearLevel = "Gear Level Unknown";
                }
                if (character.gear >= 1 && character.gear <= 3)
                {
                    // 1-3 light green
                    GearLevelColor = new SolidColorBrush(Color.FromRgb(128, 255, 128));
                }
                else if (character.gear >= 4 && character.gear <= 6)
                {
                    // 4-6 light blue
                    GearLevelColor = new SolidColorBrush(Color.FromRgb(128, 255, 255));
                }
                else if (character.gear >= 7 && character.gear <= 11)
                {
                    // 7-11 purple
                    GearLevelColor = new SolidColorBrush(Color.FromRgb(128, 0, 255));
                }
                else if (character.gear == 12)
                {
                    // 12 gold
                    GearLevelColor = new SolidColorBrush(Color.FromRgb(220, 230, 40));
                }
                else
                {
                    // 13 orange
                    GearLevelColor = new SolidColorBrush(Color.FromRgb(255, 64, 64));
                }

                // Compute which mod sets this character has bonuses for
                List<string> sets = new List<string>();
                // Look for 4-piece sets first
                if (character.mods.Count(m => m.set == Mod.SET_CRIT_DAMAGE) >= 4)
                    sets.Add("Crit Damage");
                if (character.mods.Count(m => m.set == Mod.SET_OFFENSE) >= 4)
                    sets.Add("Offense");
                if (character.mods.Count(m => m.set == Mod.SET_SPEED) >= 4)
                    sets.Add("Speed");
                // Now look for 2-piece sets, taking into account multiple sets
                int count = character.mods.Count(m => m.set == Mod.SET_HEALTH);
                for (int i = 0; i < count / 2; i++)
                    sets.Add("Health");
                count = character.mods.Count(m => m.set == Mod.SET_DEFENSE);
                for (int i = 0; i < count / 2; i++)
                    sets.Add("Defense");
                count = character.mods.Count(m => m.set == Mod.SET_CRIT_CHANCE);
                for (int i = 0; i < count / 2; i++)
                    sets.Add("Crit Chance");
                count = character.mods.Count(m => m.set == Mod.SET_POTENCY);
                for (int i = 0; i < count / 2; i++)
                    sets.Add("Potency");
                count = character.mods.Count(m => m.set == Mod.SET_TENACITY);
                for (int i = 0; i < count / 2; i++)
                    sets.Add("Tenacity");
                // Build output string
                if (sets.Any())
                {
                    sets.Sort();
                    ModSets = string.Join(", ", sets);
                }
                else
                    ModSets = "None";
            }

            // Build star level display string
            DisplayStars = string.Join("", Enumerable.Repeat("★", Stars)) + string.Join("", Enumerable.Repeat("☆", 7 - Stars));
        }

    }

    /// <summary>
    /// Sample data to populate the UI in the designer.
    /// </summary>
    public class CharacterDetailsDesigntimeViewmodel : CharacterDetailsViewmodel
    {
        public CharacterDetailsDesigntimeViewmodel()
        {
            character = new Character()
            {
                name = "Kanan Jarrus",
                id = "xo2FOt5AQhqu6Pa9yWziXQ",
                defId = "KANANJARRUSS3",
                rarity = 7,
                level =  85,
                gp = 16144,
                TruePower = 16144,
                xp = 883025,
                combatType = Character.COMBATTYPE_CHARACTER,
                gear = 8,
                relic = new RelicInfo()
                {
                    currentTier = 1
                },
                equipped = new Equipped[]
                {
                    new Equipped() { equipmentId = "108", slot = 3 },
                    new Equipped() { equipmentId = "113", slot = 4 },
                    new Equipped() { equipmentId = "055", slot = 5 }
                },
                skills = new Skill[] { },
                crew = new Crew[] { },
                mods = new Mod[]
                {
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_SQUARE },
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_ARROW },
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_DIAMOND },
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_TRIANGLE },
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_CIRCLE },
                    new Mod() { level = 15, pips = 5, set = Mod.SET_HEALTH, slot = Mod.SLOT_CROSS }
                }
            };
            PopulateFields();
        }
    }
}
