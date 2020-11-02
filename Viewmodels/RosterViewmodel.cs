using goh_ui.Models;
using goh_ui.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class RosterViewmodel : DependencyObject
    {
        private IEnumerable<UnitDetails> units;

        public RosterViewmodel(GuildInfo guild, PlayerList members, IEnumerable<UnitDetails> unitDetails)
        {
            Guild = guild ?? throw new ArgumentNullException("guild");
            Members = members ?? throw new ArgumentNullException("members");

            GuildName = Guild.name;
            units = unitDetails;

            Export = new SimpleCommand(ExportTable);
            GenerateReport = new SimpleCommand(() => DoReport(Owner));
        }


        #region Public members

        /// <summary> Name of the current guild. </summary>
        public string GuildName { get; private set; }

        /// <summary> Data for each guild member. </summary>
        public PlayerList Members { get; private set; }

        /// <summary> Guild metadata. </summary>
        public GuildInfo Guild { get; private set; }

        /// <summary> Window that owns this viewmodel. </summary>
        public Window Owner { get; set; }

        #endregion

        public SimpleCommand Export { get; private set; }

        public SimpleCommand GenerateReport { get; private set; }

        /// <summary> Export the currently-displayed table data to a CSV file. </summary>
        private void ExportTable()
        {
            // Prompt user for output filename
            var dlg = new SaveFileDialog()
            {
                Title = "Export to file",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true,
                DefaultExt = ".csv",
                Filter = "CSV Files|*.csv|All Files|*.*"
            };
            var result = dlg.ShowDialog();
            if (!result.HasValue || result == false || string.IsNullOrWhiteSpace(dlg.FileName))
            {
                return;
            }

            // Generate CSV version of table
            try
            {
                using (var writer = new StreamWriter(dlg.FileName))
                {
                    writer.WriteLine("Name,Total Power,Character Power,Meaningful Power");
                    foreach (var p in Members)
                    {
                        writer.WriteLine($"{p.Name},{p.Power},{p.CharacterPower},{p.MeaningfulPower}");
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error saving file:\n{e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary> Open the 'Roster Report' dialog. </summary>
        private void DoReport(Window owner)
        {
            var vm = new RosterReportViewmodel(Members, units);
            var win = new RosterReportView(vm) { Owner = owner };
            win.ShowDialog();
        }


        /// <summary> Show detailed information about a specific player. </summary>
        /// <param name="owner">Window that will own the details window.</param>
        /// <param name="p">Player to display.</param>
        public void OpenPlayerDetails(Window owner, Player p)
        {
            if (p != null)
            {
                // Generate player details view
                var vm = new PlayerDetailViewmodel(p);
                var win = new PlayerDetailView(vm) { Owner = owner };
                win.ShowDialog();
            }
        }
    }
}
