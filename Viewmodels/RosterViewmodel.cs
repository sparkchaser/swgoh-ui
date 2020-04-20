using goh_ui.Views;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class RosterViewmodel : DependencyObject
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(RosterViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(RosterViewmodel));
        }

        public RosterViewmodel(GuildInfo guild, PlayerList members)
        {
            Guild = guild;
            Members = members;

            GuildName = Guild.name;
        }


        #region XAML bind-able members

        /// <summary> Name of the current guild. </summary>
        public string GuildName
        {
            get { return (string)GetValue(GuildNameProperty); }
            private set { SetValue(GuildNameProperty, value); }
        }
        public static readonly DependencyProperty GuildNameProperty = _dp<string>("GuildName");

        /// <summary> Data for each guild member. </summary>
        public PlayerList Members
        {
            get { return (PlayerList)GetValue(MembersProperty); }
            private set { SetValue(MembersProperty, value); }
        }
        public static readonly DependencyProperty MembersProperty = _dp<PlayerList>("Members");

        /// <summary> Guild metadata. </summary>
        public GuildInfo Guild
        {
            get { return (GuildInfo)GetValue(GuildProperty); }
            private set { SetValue(GuildProperty, value); }
        }
        public static readonly DependencyProperty GuildProperty = _dp<GuildInfo>("Guild");

        #endregion


        /// <summary> Show detailed information about a specific player. </summary>
        /// <param name="owner">Window that will own the details window.</param>
        /// <param name="p">Player to display.</param>
        public void OpenPlayerDetails(Window owner, Player p)
        {
            if (p != null)
            {
                // Generate player details view
                var vm = new PlayerDetailViewmodel() { Player = p };
                var win = new PlayerDetailView(vm) { Owner = owner };
                win.ShowDialog();
            }
        }
    }
}
