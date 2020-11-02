using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace goh_ui.Models
{
    /// <summary>
    /// Pre-defined squad setup.
    /// </summary>
    public class SquadPreset
    {
        /// <summary> Descriptive name for this squad. </summary>
        public string Name { get; set; }
        /// <summary> Name of squad leader. </summary>
        public string Character1 { get; set; }
        /// <summary> Name of second character in squad. </summary>
        public string Character2 { get; set; }
        /// <summary> Name of third character in squad. </summary>
        public string Character3 { get; set; }
        /// <summary> Name of fourth character in squad. </summary>
        public string Character4 { get; set; }
        /// <summary> Name of fifth character in squad. </summary>
        public string Character5 { get; set; }

        public SquadPreset() { }
        public SquadPreset(string name, string c1, string c2, string c3, string c4, string c5)
        {
            Name = name;
            Character1 = c1;
            Character2 = c2;
            Character3 = c3;
            Character4 = c4;
            Character5 = c5;
        }

        #region Static methods

        /// <summary> Load presets from a CSV file on disk. </summary>
        /// <returns>List of presets from file, or an empty list on error.</returns>
        public static List<SquadPreset> LoadPresets()
        {
            List<SquadPreset> results = new List<SquadPreset>();

            // Look for CSV file in directory with the program
            string preset_filename = "presets.csv";
            string fn = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, preset_filename);
            if (!File.Exists(fn))
                return results;

            foreach (string line in File.ReadLines(preset_filename))
            {
                // Remove comments
                string thisline = Regex.Replace(line, @"#.*^", "");

                if (string.IsNullOrWhiteSpace(thisline))
                    continue;

                // Extract fields
                var fields = thisline.Split(',').Select(f => f.Trim()).ToArray();
                if (fields.Length < 6)
                    continue;
                if (fields.Any(f => string.IsNullOrWhiteSpace(f)))
                    continue;

                // Add to preset list
                results.Add(new SquadPreset(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5]));
            }

            return results;
        }

        #endregion
    }
}
