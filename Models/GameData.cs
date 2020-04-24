
using System;

namespace goh_ui.Models
{
    /// <summary>
    /// General game metadata, not specific to any player or guild.
    /// </summary>
    public class GameData
    {
        /// <summary> Timestamp for when this data was last updated. </summary>
        public DateTime Updated { get; set; } = DateTime.Now.AddDays(-365);

        /// <summary> Character metadata. </summary>
        public UnitDetails[] Units { get; set; } = null;

        /// <summary> Unlockable player titles. </summary>
        public TitleInfo[] Titles { get; set; } = null;

        /// <summary> Power multipliers for units with relics. </summary>
        public int[] RelicMultipliers { get; set; } = null;


        /// <summary> Indicates whether or not this object has been fully populated with data. </summary>
        public bool HasData()
        {
            if (Units == null || Units.Length == 0)
                return false;
            if (Titles == null || Titles.Length == 0)
                return false;
            if (RelicMultipliers == null || RelicMultipliers.Length == 0)
                return false;
            return true;
        }

        /// <summary> Indicates whether or not this data is likely to be out of date and need refreshing. </summary>
        /// <returns>True if the data is stale, false if recent enough.</returns>
        /// <remarks>Data is considered stale if it's more than a week old.</remarks>
        public bool IsOutdated() => Updated <= DateTime.Now.AddDays(-7);
    }
}
