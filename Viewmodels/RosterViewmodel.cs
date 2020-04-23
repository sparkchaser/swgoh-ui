using goh_ui.Models;
using goh_ui.Views;
using System;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class RosterViewmodel : DependencyObject
    {

        public RosterViewmodel(GuildInfo guild, PlayerList members)
        {
            Guild = guild ?? throw new ArgumentNullException("guild");
            Members = members ?? throw new ArgumentNullException("members");

            GuildName = Guild.name;
        }


        #region Public members

        /// <summary> Name of the current guild. </summary>
        public string GuildName { get; private set; }

        /// <summary> Data for each guild member. </summary>
        public PlayerList Members { get; private set; }

        /// <summary> Guild metadata. </summary>
        public GuildInfo Guild { get; private set; }

        #endregion


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
