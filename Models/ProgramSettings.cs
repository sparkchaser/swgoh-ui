using Newtonsoft.Json;
using System;
using System.IO;

namespace goh_ui.Models
{
    /// <summary>
    /// Program-wide settings.
    /// </summary>
    /// <remarks> This is intended to be a replacement for the .NET program settings,
    /// so that we can control where they get stored. </remarks>
    public class ProgramSettings
    {
        public string username { get; set; } = "";
        public string userid { get; set; } = "";
        public string allycode { get; set; } = "";

        #region Object serialization

        /// <summary> Load settings from disk if it present. </summary>
        /// <param name="file"> Path to target file. </param>
        public static ProgramSettings LoadOrCreate(string file)
        {
            ProgramSettings retval;
            try
            {
                using (StreamReader fd = File.OpenText(file))
                {
                    JsonSerializer js = new JsonSerializer();
                    retval = (ProgramSettings)js.Deserialize(fd, typeof(ProgramSettings));
                }
            }
            // Create new if file doesn't exist or can't be decoded
            catch (FileNotFoundException) { return new ProgramSettings(); }
            catch (IOException) { return new ProgramSettings(); }
            catch (JsonException) { return new ProgramSettings(); }

            return retval;
        }

        /// <summary> Store a <see cref="ProgramSettings"/> object to disk. </summary>
        /// <param name="data"> Object to store. </param>
        /// <param name="file"> Path to target file. </param>
        public static void Store(ProgramSettings data, string file)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (file == null)
                throw new ArgumentNullException("file");
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("Target file path was empty.", "file");

            using (StreamWriter fd = File.CreateText(file))
            {
                JsonSerializer js = new JsonSerializer();
                js.Serialize(fd, data);
            }
        }

        #endregion
    }
}
