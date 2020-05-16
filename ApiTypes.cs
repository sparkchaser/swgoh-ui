using goh_ui.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace goh_ui
{

    // Classes for objects used by the swgoh.help APIs.
    // The classes are suitable for JSON serialization.
    // Adapted from: https://gist.github.com/dwcullop/9c6b7933fe23163e59b94da1997adee7

    #region Data from the 'Login' command
    
    /// <summary>
    /// API response to a login request.
    /// </summary>
    public class LoginResponse
    {
        public string token_type { get; set; }
        /// <summary> Bearer token to use with future API requests. </summary>
        public string access_token { get; set; }
        /// <summary> Number of seconds until this token expires. </summary>
        public int expires_in { get; set; }
    }

    #endregion

    #region Data from the 'Get Player Info' command

    /// <summary>
    /// Metadata for a player's account.
    /// </summary>
    public class PlayerInfo
    {
        /// <summary> Player name. </summary>
        public string name { get; set; }
        /// <summary> Titles earned by this player. </summary>
        public Titles titles { get; set; }
        /// <summary> Portrait icons earned by this player. </summary>
        public Titles portraits { get; set; }
        /// <summary> Various recorded statistics. </summary>
        public PlayerStat[] stats { get; set; }
        /// <summary> Current player level. </summary>
        public int level { get; set; }
        /// <summary> In-game ally code. </summary>
        public int allyCode { get; set; }
        /// <summary> Name of the player's guild. </summary>
        public string guildName { get; set; }
        /// <summary> Total galactic power. </summary>
        public int gpFull { get; set; }
        /// <summary> Galactic power (characters only). </summary>
        public int gpChar { get; set; }
        /// <summary> Galactic power (ships only). </summary>
        public int gpShip { get; set; }
        /// <summary> All ships and characters owned by this player. </summary>
        public Character[] roster { get; set; }
        /// <summary> Arena squads and ranking information. </summary>
        public Arena arena { get; set; }
        /// <summary> Statistics about Grand Arena performance. </summary>
        public GASeason[] grandArena { get; set; }
        public int grandArenaLifeTime { get; set; }
        public long updated { get; set; }

        [JsonIgnore]
        /// <summary> Timestamp when this information was last updated. </summary>
        public DateTimeOffset Updated => DateTimeOffset.FromUnixTimeMilliseconds(this.updated);
    }

    /// <summary>
    /// Titles available to a player.
    /// </summary>
    public class Titles
    {
        /// <summary> Currently-displayed title. </summary>
        public string selected { get; set; }
        /// <summary> All of the titles that this character has unlocked. </summary>
        public string[] unlocked { get; set; }
    }

    /// <summary>
    /// Statistical information about a player.
    /// </summary>
    public class PlayerStat
    {
        /// <summary> Description, as seen on the character's "Stats" screen. </summary>
        [JsonProperty("nameKey")]
        public string name { get; set; }
        /// <summary> Value for this particular stat. </summary>
        public long value { get; set; }
    }

    /// <summary>
    /// Arena team/ranking metadata.
    /// </summary>
    public class Arena
    {
        /// <summary> Character arena metadata. </summary>
        [JsonProperty("char")]
        public Ranking character { get; set; }
        /// <summary> Ship arena metadata. </summary>
        public Ranking ship { get; set; }
    }

    /// <summary>
    /// Arena team and rank.
    /// </summary>
    public class Ranking
    {
        /// <summary> Current ranking. </summary>
        public int? rank { get; set; }
        /// <summary> Current team. </summary>
        public Squad[] squad { get; set; }
    }

    /// <summary>
    /// Member of an arena squad.
    /// </summary>
    public class Squad
    {
        /// <summary> Internal ID used by the game. </summary>
        public string id { get; set; }
        /// <summary> Semi-human-readable name for this unit. </summary>
        public string defId { get; set; }
        /// <summary> Describes the type of slot this unit is assigned to. </summary>
        public string squadUnitType { get; set; }

        #region Squad Unit Type constants

        /// <summary> This character is in the leader slot. </summary>
        public static readonly string UNITTYPE_SQUAD_LEADER  = "UNITTYPELEADER";
        /// <summary> This character or ship is not in a special slot. </summary>
        public static readonly string UNITTYPE_NORMAL        = "UNITTYPEDEFAULT";
        /// <summary> This ship is a capital ship. </summary>
        public static readonly string UNITTYPE_CAPITAL_SHIP  = "UNITTYPECOMMANDER";
        /// <summary> This ship is in a reinforcement slot. </summary>
        public static readonly string UNITTYPE_REINFORCEMENT = "UNITTYPEREINFORCEMENT";

        #endregion
    }

    /// <summary>
    /// A single character/ship in a player's roster.
    /// </summary>
    public class Character
    {
        /// <summary> Game's internal ID. </summary>
        public string id { get; set; }
        /// <summary> Semi-human-readable name for this unit.  Correlates to <see cref="UnitDetails.baseId"/>. </summary>
        public string defId { get; set; }
        /// <summary> Localized unit name. </summary>
        [JsonProperty("nameKey")]
        public string name { get; set; }
        /// <summary> Star level. </summary>
        public int rarity { get; set; }
        /// <summary> Character level. </summary>
        public int level { get; set; }
        /// <summary> Unit galactic power. </summary>
        public long gp { get; set; }
        /// <summary> Unit experience. </summary>
        public int xp { get; set; }
        /// <summary> Type of unit this is (1=char, 2=ship). </summary>
        public string combatType { get; set; }
        /// <summary> Gear level. </summary>
        public int gear { get; set; }
        /// <summary> For characters, the currently-equipped set of gear. </summary>
        public Equipped[] equipped { get; set; }
        /// <summary> Abilities this unit knows. </summary>
        public Skill[] skills { get; set; }
        /// <summary> For ships, the crew members needed for this unit. </summary>
        public Crew[] crew { get; set; }
        /// <summary> For characters, the mods currently assigned to this unit. </summary>
        public Mod[] mods { get; set; }
        /// <summary> For G13 characters, the current relic level + 2. </summary>
        public RelicInfo relic { get; set; }

        #region Derived properties, for display purposes

        /// <summary> Combined gear+relic level, as a number for sorting purposes. </summary>
        [JsonIgnore]
        public decimal GearLevelSortable
        {
            get
            {
                if (gear < 13)
                    return gear;
                return gear + (decimal)(0.1 * (relic.currentTier - 2));
            }
        }

        /// <summary> String describing gear+relic level. </summary>
        [JsonIgnore]
        public string GearLevelDescriptive
        {
            get
            {
                if (gear < 13 || relic.currentTier <= 2)
                    return gear.ToString();
                return $"R{relic.currentTier - 2}";
            }
        }

        /// <summary> Number of zetas unlocked for this character. </summary>
        [JsonIgnore]
        public int NumZetas => skills.Where(s => (s.tier == s.tiers) && s.isZeta).Count();

        /// <summary> List of abilities that have unlocked zetas. </summary>
        [JsonIgnore]
        public List<string> Zetas => skills.Where(s => (s.tier == s.tiers) && s.isZeta).Select(s => s.name).ToList();

        /// <summary> Unit power, adjusted for relics. </summary>
        /// <remarks>
        /// This value should match the power displayed in-game.
        /// Save to JSON, since we won't have the data to re-compute it when importing.
        /// </remarks>
        public long TruePower { get; set; }
        #endregion

        #region CombatType constants

        public static readonly string COMBATTYPE_CHARACTER = "CHARACTER";
        public static readonly string COMBATTYPE_SHIP      = "SHIP";

        #endregion
    }

    /// <summary>
    /// A unit's current relic level.
    /// </summary>
    public class RelicInfo
    {
        /// <summary> The character's current relic level. </summary>
        /// <remarks>
        /// For G12 and below, will == 1.
        /// For G13 and above, will == (relic level + 2)
        /// </remarks>
        public int currentTier { get; set; }
    }

    /// <summary>
    /// A piece of gear equipped by a character.
    /// </summary>
    public class Equipped
    {
        /// <summary> Internal game ID for this piece of gear; </summary>
        public string equipmentId { get; set; }
        /// <summary> Indicates which gear slot this item is assigned to. </summary>
        public int slot { get; set; }

        #region Slot constants

        public static readonly int SLOT_TOP_LEFT  = 0;
        public static readonly int SLOT_MID_LEFT  = 1;
        public static readonly int SLOT_BOT_LEFT  = 2;
        public static readonly int SLOT_TOP_RIGHT = 3;
        public static readonly int SLOT_MID_RIGHT = 4;
        public static readonly int SLOT_BOT_RIGHT = 5;

        #endregion
    }

    /// <summary>
    /// An active or passive ability.
    /// </summary>
    public class Skill
    {
        /// <summary> Internal game ID. </summary>
        public string id { get; set; }
        /// <summary> Current level of this skill. </summary>
        public int tier { get; set; }
        /// <summary> Max possible level for this skill. </summary>
        public int tiers { get; set; }
        /// <summary> Localized name for the skill. </summary>
        [JsonProperty("nameKey")]
        public string name { get; set; }
        /// <summary> Whether this skill has a zeta (even if not yet activated). </summary>
        public bool isZeta { get; set; }
    }

    /// <summary>
    /// A crew member on a ship.
    /// </summary>
    public class Crew
    {
        /// <summary> The 'defId' for this character. </summary>
        public string unitId { get; set; }
        /// <summary> Which crew member slot this character fits into. </summary>
        public int slot { get; set; }
        /// <summary> The ship ability contributed by this crew member. </summary>
        public SkillReference[] skillReferenceList { get; set; }
        /// <summary> The amount of "Crew Power" contributed by this character. </summary>
        public float cp { get; set; }
        /// <summary> This character's Galactic Power. </summary>
        public float gp { get; set; }

        #region Slot constants

        public static readonly string SLOT_TOP_LEFT    = "1";
        public static readonly string SLOT_BOTTOM_LEFT = "2";
        public static readonly string SLOT_TOP_RIGHT   = "3";

        #endregion
    }

    /// <summary>
    /// One level of a ship ability contributed by a crew member.
    /// </summary>
    public class SkillReference
    {
        /// <summary> Internal game ID for this skill. </summary>
        public string skillId { get; set; }
        // Appears unused
        public int requiredTier { get; set; }
        // Appears unused
        public int requiredRarity { get; set; }
    }

    /// <summary>
    /// Metadata for a single character mod.
    /// </summary>
    public class Mod
    {
        /// <summary> Game's internal ID for this mod. </summary>
        public string id { get; set; }
        /// <summary> Which slot this mod goes in. </summary>
        public string slot { get; set; }
        // Currently unused
        public int setId { get; set; }
        /// <summary> Which set this mod belongs to. </summary>
        public string set { get; set; }
        /// <summary> Mod level. </summary>
        public int level { get; set; }
        /// <summary> Number of 'dots' on the mod. </summary>
        public int pips { get; set; }
        
        #region These fields don't currently get set
        
        public string primaryBonusType { get; set; }
        public string primaryBonusValue { get; set; }
        public string secondaryType_1 { get; set; }
        public string secondaryValue_1 { get; set; }
        public string secondaryType_2 { get; set; }
        public string secondaryValue_2 { get; set; }
        public string secondaryType_3 { get; set; }
        public string secondaryValue_3 { get; set; }
        public string secondaryType_4 { get; set; }
        public string secondaryValue_4 { get; set; }

        #endregion

        #region Slot ID constants

        public static readonly string SLOT_SQUARE   = "1";
        public static readonly string SLOT_ARROW    = "2";
        public static readonly string SLOT_DIAMOND  = "3";
        public static readonly string SLOT_TRIANGLE = "4";
        public static readonly string SLOT_CIRCLE   = "5";
        public static readonly string SLOT_CROSS    = "6";

        #endregion

        #region Set constants

        public static readonly string SET_HEALTH      = "1";
        public static readonly string SET_OFFENSE     = "2";
        public static readonly string SET_DEFENSE     = "3";
        public static readonly string SET_SPEED       = "4";
        public static readonly string SET_CRIT_CHANCE = "5";
        public static readonly string SET_CRIT_DAMAGE = "6";
        public static readonly string SET_POTENCY     = "7";
        public static readonly string SET_TENACITY    = "8";

        #endregion
    }

    /// <summary>
    /// Metadata for a Grand Arena season.
    /// </summary>
    public class GASeason
    {
        public string seasonId { get; set; }
        public string eventInstanceId { get; set; }
        public string league { get; set; }
        public int wins { get; set; }
        public int losses { get; set; }
        public bool eliteDivision { get; set; }
        public int seasonPoints { get; set; }
        public int division { get; set; }
        public long joinTime { get; set; }
        public long endTime { get; set; }
        public bool remove { get; set; }
        public int rank { get; set; }
    }

    #endregion

    #region Data from the 'Get Guild Info' command

    /// <summary>
    /// Information for a guild.
    /// </summary>
    public class GuildInfo
    {
        /// <summary> Guild name. </summary>
        public string name { get; set; }
        /// <summary> Guild's public description. </summary>
        public string desc { get; set; }
        /// <summary> Number of members. </summary>
        public int members { get; set; }
        public int status { get; set; }
        public int required { get; set; }
        /// <summary> Color selected for guild icon. </summary>
        public string bannerColor { get; set; }
        /// <summary> Selected guild icon. </summary>
        public string bannerLogo { get; set; }
        /// <summary> Message displayed in the banner above the guild chat window. </summary>
        public string message { get; set; }
        /// <summary> Total guild galactic power. </summary>
        public int gp { get; set; }
        /// <summary> Information about this guild's raids. </summary>
        public Raid raid { get; set; }
        /// <summary> List of guild members. </summary>
        public GuildMember[] roster { get; set; }
        public long updated { get; set; }

        [JsonIgnore]
        /// <summary> Timestamp when this information was last updated. </summary>
        public DateTimeOffset Updated => DateTimeOffset.FromUnixTimeMilliseconds(this.updated);
    }

    /// <summary>
    /// Guild raid metadata.
    /// </summary>
    public class Raid
    {
        /// <summary> Level of the last rancor raid. </summary>
        public string rancor { get; set; }
        /// <summary> Level of the last AAT raid. </summary>
        public string aat { get; set; }
        /// <summary> Level of the last sith raid. </summary>
        public string sith_raid { get; set; }
    }

    /// <summary>
    /// A member of a guild.
    /// </summary>
    public class GuildMember
    {
        /// <summary> Player name. </summary>
        public string name { get; set; }
        /// <summary> Player's ranking within the guild. </summary>
        public string guildMemberLevel { get; set; }
        /// <summary> Player level. </summary>
        public int level { get; set; }
        /// <summary> In-game ally code. </summary>
        public int allyCode { get; set; }
        /// <summary> Total galactic power. </summary>
        public int gp { get; set; }
        /// <summary> Galactic power (characters only). </summary>
        public int gpChar { get; set; }
        /// <summary> Galactic power (ships only). </summary>
        public int gpShip { get; set; }
        public long updated { get; set; }

        [JsonIgnore]
        /// <summary> Timestamp when this information was last updated. </summary>
        public DateTimeOffset Updated => DateTimeOffset.FromUnixTimeMilliseconds(this.updated);

        #region Member Level constants

        public static readonly string LEVEL_MEMBER = "GUILDMEMBER";
        public static readonly string LEVEL_OFFICER = "GUILDOFFICER";
        public static readonly string LEVEL_LEADER = "GUILDLEADER";

        #endregion
    }

    #endregion

    #region Data from the 'Get Game Data' command

    /// <summary>
    /// Information about an unlockable player title.
    /// </summary>
    public class TitleInfo
    {
        /// <summary> The game's internal ID. </summary>
        public string id { get; set; }
        /// <summary> Title text (localized). </summary>
        [JsonProperty("nameKey")]
        public string name { get; set; }
        /// <summary> Extended description. </summary>
        public string descKey { get; set; }
        /// <summary> Whether this title is currently obtainable. </summary>
        public bool obtainable { get; set; }
        public bool hidden { get; set; }
        /// <summary> Brief description. </summary>
        public string shortDescKey { get; set; }
        public long updated { get; set; }

        [JsonIgnore]
        /// <summary> Timestamp when this information was last updated. </summary>
        public DateTimeOffset Updated => DateTimeOffset.FromUnixTimeMilliseconds(this.updated);
    }

    /// <summary>
    /// A data table from the "tableList" collection.
    /// </summary>
    public class DataTable
    {
        /// <summary> The game's internal ID. </summary>
        public string id { get; set; }
        /// <summary> Table data. </summary>
        public DataTableEntry[] rowList { get; set; }
        public long updated { get; set; }

        [JsonIgnore]
        /// <summary> Timestamp when this information was last updated. </summary>
        public DateTimeOffset Updated => DateTimeOffset.FromUnixTimeMilliseconds(this.updated);
    }

    /// <summary>
    /// An entry in a table from the "tableList" collection.
    /// </summary>
    public class DataTableEntry
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    /// <summary>
    /// Detailed information about a unit.
    /// </summary>
    public class UnitDetails
    {
        /// <summary> Localized unit name. </summary>
        [JsonProperty("nameKey")]
        public string name { get; set; }
        /// <summary> Whether this character is dark- or light-side aligned. </summary>
        public string forceAlignment { get; set; }
        /// <summary> Whether this is a character or a ship. </summary>
        public string combatType { get; set; }
        /// <summary> Semi-human-readable ID string. Correlate to <see cref="Character.defId"/>. </summary>
        public string baseId { get; set; }
        /// <summary> List of categories/tags assigned to this character. </summary>
        public string[] categoryIdList { get; set; }

        #region ForceAlignment constants

        public static readonly string ALIGNMENT_LIGHT = "LIGHT";
        public static readonly string ALIGNMENT_DARK  = "DARK";

        #endregion

        #region CombatType constants

        public static readonly string COMBATTYPE_CHARACTER = "CHARACTER";
        public static readonly string COMBATTYPE_SHIP = "SHIP";

        #endregion
    }

    /// <summary>
    /// Details about a category/tag that can be assigned to a unit.
    /// </summary>
    public class Category
    {
        /// <summary> ID for this category, cross-reference with <see cref="UnitDetails.categoryIdList"/>. </summary>
        public string id { get; set; }
        /// <summary> Localized description for this category. </summary>
        /// <remarks> For some non-visible categories, this might be a placeholder. </remarks>
        public string descKey { get; set; }
        /// <summary> Whether this category is visible to the user in-game. </summary>
        public bool visible { get; set; }
        /// <summary> Indicates which types of unit filters should include this category. </summary>
        /// <remarks>
        ///  1: Use this category when sorting characters
        ///  2: Use this category when sorting ships
        /// </remarks>
        public int[] uiFilterList { get; set; }
    }

    #endregion

    #region Data from the 'Get Zeta Info' command

    /// <summary>
    /// List of zeta recommendations.
    /// </summary>
    public class ZetaRecommendations
    {
        public ZetaStats[] zetas { get; set; }
    }

    /// <summary>
    /// Information about a specific zeta ability.
    /// </summary>
    /// <remarks> For numerical ratings, 1 is best and 10 is worst. </remarks>
    public class ZetaStats
    {
        /// <summary> Name of the ability. </summary>
        public string name { get; set; }
        /// <summary> Type of ability (unique, leader, etc). </summary>
        public string type { get; set; }
        /// <summary> Character associated with this ability. </summary>
        public string toon { get; set; }
        /// <summary> Usefulness of this zeta in PVP (1-10). </summary>
        public double pvp { get; set; }
        /// <summary> Usefulness of this zeta in Territory War (1-10). </summary>
        public double tw { get; set; }
        /// <summary> Usefulness of this zeta in Geonosis Territory Battle (1-10). </summary>
        public double tb { get; set; }
        /// <summary> Usefulness of this zeta in the Rancor raid [heroic] (1-10). </summary>
        public double pit { get; set; }
        /// <summary> Usefulness of this zeta in the AAT raid [heroic] (1-10). </summary>
        public double tank { get; set; }
        /// <summary> Usefulness of this zeta in the Sith Triumverate raid [heroic] (1-10). </summary>
        public double sith { get; set; }
        /// <summary> Usefulness of this zeta across the entire game (1-10). </summary>
        public double versa { get; set; }
    }

    #endregion

    #region Command payloads

    /// <summary>
    /// Command to fetch information for a player or guild.
    /// </summary>
    public class PlayerGuildInfoCommand
    {
        /// <summary> Array of ally codes to query. </summary>
        public string[] allycodes { get; set; }
        
        #region Optional fields, but this program only works if they're set as shown.
        
        /// <summary> Language to localize strings into. </summary>
        public string language { get; set; } = "ENG_US";
        /// <summary> Whether to translate numeric enums to readable strings. </summary>
        public bool enums { get; set; } = true;

        #endregion
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    /// <summary>
    /// Command to fetch a collection of misc game data.
    /// </summary>
    public class GameDataCommand
    {
        /// <summary> Data collection to fetch. </summary>
        public string collection { get; set; }

        #region Optional fields, but this program only works if they're set as shown.

        /// <summary> Language to localize strings into. </summary>
        public string language { get; set; } = "ENG_US";
        /// <summary> Whether to translate numeric enums to readable strings. </summary>
        public bool enums { get; set; } = true;

        #endregion

        /// <summary> (Optional) Filter results to entries that match the specified criteria. </summary>
        /// <remarks>
        /// Each entry's "key" is a property name on the target object, and the "value" is the
        /// expected value for that property.  Only entries who have the specified value for
        /// the specified property will be included in the results.  See the swgoh.help API
        /// documentation for details and examples.
        /// </remarks>
        public Dictionary<string, object> match { get; set; } = null;

        /// <summary> (Optional) Alter the returned data's format by cherry-picking fields or generating computed fields. </summary>
        /// <remarks> This is directly passed to the 'project' operator in MongoDB. See the MongoDB docs for details. </remarks>
        public Dictionary<string, object> project { get; set; } = null;
    }

    /// <summary>
    /// Command to fetch list of zeta recommendations.
    /// </summary>
    public class ZetaRecommendationsCommand
    {
        /// <summary> (Optional) Alter the returned data's format by cherry-picking fields or generating computed fields. </summary>
        /// <remarks> This is directly passed to the 'project' operator in MongoDB. See the MongoDB docs for details. </remarks>
        public Dictionary<string, object> project { get; set; } = null;
    }

    #endregion

    #region Import/export serialization helpers

    /// <summary>
    /// Wrapper class used to serialize a complete information set.
    /// </summary>
    public class CompleteInfoSet
    {
        /// <summary> Overall guild data. </summary>
        public GuildInfo guild { get; set; }
        /// <summary> Data for each guild member. </summary>
        public PlayerInfo[] players { get; set; }
    }
    
    #endregion
}
