using Newtonsoft.Json;
using System;
using System.IO;

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

        #region Object serialization

        /// <summary> Load cached game metadata from disk if it exists. </summary>
        /// <param name="file"> Path to target file. </param>
        public static GameData LoadOrCreate(string file)
        {
            GameData retval;
            try
            {
                using (StreamReader fd = File.OpenText(file))
                {
                    JsonSerializer js = new JsonSerializer();
                    retval = (GameData)js.Deserialize(fd, typeof(GameData));
                }
            }
            // Create new if file doesn't exist or can't be decoded
            catch (FileNotFoundException) { return new GameData(); }
            catch (IOException) { return new GameData(); }
            catch (JsonException) { return new GameData(); }

            return retval;
        }

        /// <summary> Store a <see cref="GameData"/> object to disk. </summary>
        /// <param name="data"> Object to store. </param>
        /// <param name="file"> Path to target file. </param>
        public static void Store(GameData data, string file)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (file == null)
                throw new ArgumentNullException("file");
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("Target file path was empty.", "file");
            if (!data.HasData())
                throw new InvalidOperationException("Object to serialize is not complete.");

            using (StreamWriter fd = File.CreateText(file))
            {
                JsonSerializer js = new JsonSerializer();
                js.Serialize(fd, data);
            }
        }

        #endregion
    }
}
