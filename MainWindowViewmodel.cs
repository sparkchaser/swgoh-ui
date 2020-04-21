using goh_ui.Views;
using goh_ui.Viewmodels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace goh_ui
{
    /// <summary>
    /// Simplistic ICommand wrapper for a button handler.
    /// </summary>
    public class SimpleCommand : ICommand
    {
        private readonly Action action;
        private readonly Func<bool> buttonTest;

        public SimpleCommand(Action a, Func<bool> test = null)
        {
            action = a;
            buttonTest = test;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => buttonTest != null ? buttonTest.Invoke() : true;
        public void Execute(object parameter) => action?.Invoke();
    }

    /// <summary>
    /// Simplistic ICommand wrapper for a button handler that takes an argument.
    /// </summary>
    public class SimpleArgCommand : ICommand
    {
        private readonly Action<object> action;
        private readonly Func<bool> buttonTest;

        public SimpleArgCommand(Action<object> a, Func<bool> test = null)
        {
            action = a;
            buttonTest = test;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => buttonTest != null ? buttonTest.Invoke() : true;
        public void Execute(object parameter) => action?.Invoke(parameter);
    }

    public class MainWindowViewmodel : DependencyObject, INotifyPropertyChanged
    {
        /// <summary> Helper function to simplify generation of dependency properties. </summary>
        /// <typeparam name="T">Datatype of property</typeparam>
        /// <param name="name">Name of property</param>
        /// <param name="defaultvalue">Default value for property (optional)</param>
        private static DependencyProperty _dp<T>(string name, object defaultvalue = null)
        {
            if (defaultvalue != null)
                return DependencyProperty.Register(name, typeof(T), typeof(MainWindowViewmodel), new PropertyMetadata(defaultvalue));
            else
                return DependencyProperty.Register(name, typeof(T), typeof(MainWindowViewmodel));
        }

        #region INotifyPropertyChanged methods

        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary> Notify listeners that a dependency property has changed. </summary>
        /// <param name="propname">Name of the property that changed.</param>
        private void OnPropertyChanged(string propname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));

        #endregion

        #region XAML bind-able properties


        /// <summary> Description of whatever the program is currently doing in the background. </summary>
        public string CurrentActivity
        {
            get { return (string)GetValue(CurrentActivityProperty); }
            set { SetValue(CurrentActivityProperty, value); }
        }
        public static readonly DependencyProperty CurrentActivityProperty = _dp<string>("CurrentActivity", "");

        /// <summary> Whether or not the user is currently logged in. </summary>
        public bool LoggedIn
        {
            get { return (bool)GetValue(LoggedInProperty); }
            private set { SetValue(LoggedInProperty, value); }
        }
        public static readonly DependencyProperty LoggedInProperty = _dp<bool>("LoggedIn", false);

        /// <summary> swgoh.help account username. </summary>
        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }
        public static readonly DependencyProperty UsernameProperty = _dp<string>("Username", "");

        /// <summary> swgoh.help account password. </summary>
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }
        public static readonly DependencyProperty PasswordProperty = _dp<string>("Password", "");

        /// <summary> User ID from swgoh.help account. </summary>
        public string UserId
        {
            get { return (string)GetValue(UserIdProperty); }
            set { SetValue(UserIdProperty, value); }
        }
        public static readonly DependencyProperty UserIdProperty = _dp<string>("UserId", "");

        /// <summary> In-game ally code. </summary>
        public string AllyCode
        {
            get { return (string)GetValue(AllyCodeProperty); }
            set { SetValue(AllyCodeProperty, value); }
        }
        public static readonly DependencyProperty AllyCodeProperty = _dp<string>("AllyCode", "");

        /// <summary> Name of the guild the logged-in player belongs to. </summary>
        public string GuildName
        {
            get { return (string)GetValue(GuildNameProperty); }
            set { SetValue(GuildNameProperty, value); }
        }
        public static readonly DependencyProperty GuildNameProperty = _dp<string>("GuildName", "");

        /// <summary> Name of the logged-in player. </summary>
        public string PlayerName
        {
            get { return (string)GetValue(PlayerNameProperty); }
            set { SetValue(PlayerNameProperty, value); }
        }
        public static readonly DependencyProperty PlayerNameProperty = _dp<string>("PlayerName", "");

        /// <summary> List of guild members. </summary>
        public PlayerList Members { get; set; } = new PlayerList();


        #endregion

        /// <summary> Event invoked when the viewmodel wants to close the program. </summary>
        public event EventHandler ExitEvent;

        #region Button/menu handlers

        /// <summary> Handler for the 'get guild info' button. </summary>
        public SimpleCommand GetGuildCommand { get; private set; }

        public SimpleCommand ExitCommand { get; private set; }
        public SimpleCommand ExportCommand { get; private set; }
        public SimpleCommand ImportCommand { get; private set; }
        public SimpleCommand RosterCommand { get; private set; }
        public SimpleCommand WhoHasCommand { get; private set; }
        public SimpleCommand SquadCheckerCommand { get; private set; }
        public SimpleCommand ShowAbout { get; private set; }

        #endregion


        private ApiWrapper api;

        private GuildInfo guild;

        /// <summary> Unprocessed player data as returned by the API.  Only use for serialization purposes. </summary>
        private List<PlayerInfo> rawPlayerInfo;

        /// <summary> The window connected to this viewmodel. </summary>
        private MainWindow parent;

        /// <summary> Scale factor to apply to values from the "galactic_power_per_relic_tier" data table. </summary>
        /// <remarks>
        /// Warning: This value was determined experimentally.  Tests show it to be accurate
        /// within +/- one point (plus rounding error).  It would be better to figure out how
        /// to pull this information from game data instead of hard-coding an educated guess.
        /// </remarks>
        private static readonly double GP_PER_RELIC_SCALE_FACTOR = 2.2587;

        public MainWindowViewmodel(MainWindow parent)
        {
            this.parent = parent;

            // Wire up button handlers
            GetGuildCommand = new SimpleCommand(PullData);
            ExitCommand = new SimpleCommand(DoExit);
            ExportCommand = new SimpleCommand(DoExport);
            ImportCommand = new SimpleCommand(DoImport);
            RosterCommand = new SimpleCommand(DoRoster);
            WhoHasCommand = new SimpleCommand(DoWhoHas);
            SquadCheckerCommand = new SimpleCommand(DoSquadChecker);
            ShowAbout = new SimpleCommand(() => AboutWindow.Display(this.parent));

            api = new ApiWrapper();

            CurrentActivity = "No data available";

            // Pull user settings from last time
            Username = Properties.Settings.Default.username;
            UserId = Properties.Settings.Default.userid;
            AllyCode = Properties.Settings.Default.allycode;
        }

        /// <summary> Display an error message in a dialog box. </summary>
        /// <param name="message">Message to display.</param>
        private void ShowError(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary> Indicates whether all guild and player data has been successfully retrieved. </summary>
        public bool IsAllDataAvailable()
        {
            if (guild == null)
                return false;

            if (Members == null)
                return false;

            if (guild.members != Members.Count)
                return false;

            return true;
        }


        #region Menu item handlers

        /// <summary> Exit the program. </summary>
        private void DoExit() => ExitEvent?.Invoke(this, new EventArgs());

        /// <summary> Export guild/player data to a file. </summary>
        private void DoExport()
        {
            // Ensure we have all the data we need
            if (!IsAllDataAvailable())
            {
                ShowError("Guild data has not yet been successfully retrieved.");
                return;
            }

            // Prompt user for output file name/location
            SaveFileDialog dlg = new SaveFileDialog()
            {
                Title = "Export to",
                Filter = "JSON File (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != true)
                return;

            // Serialize information to file
            CompleteInfoSet c = new CompleteInfoSet()
            {
                guild = guild,
                players = rawPlayerInfo.ToArray()
            };
            string data = Newtonsoft.Json.JsonConvert.SerializeObject(c, Newtonsoft.Json.Formatting.Indented);
            try
            {
                File.WriteAllText(dlg.FileName, data);
            }
            catch (Exception e)
            {
                ShowError($"Error writing file:\n{e.Message}");
            }
        }

        /// <summary> Import guild/player data from a file. </summary>
        private void DoImport()
        {
            // Prompt for input file
            OpenFileDialog dlg = new OpenFileDialog()
            {
                Title = "Import from",
                Filter = "JSON File (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != true)
                return;

            // Read file
            string data;
            try
            {
                data = File.ReadAllText(dlg.FileName);
            }
            catch (Exception e)
            {
                ShowError($"Error opening file:\n{e.Message}");
                return;
            }

            // Parse data
            CompleteInfoSet info;
            try
            {
                info = Newtonsoft.Json.JsonConvert.DeserializeObject<CompleteInfoSet>(data);
            }
            catch (Exception)
            {
                ShowError($"Unable to parse input file");
                return;
            }

            // Reload this object
            guild = info.guild;
            rawPlayerInfo = new List<PlayerInfo>(info.players);
            Members.Clear();
            foreach (var player in info.players)
            {
                Members.Add(new Player(player));
            }

            GuildName = guild.name;
            PlayerName = "Member";
            if (!string.IsNullOrWhiteSpace(AllyCode))
            {
                var me = Members.Where(m => m.AllyCode.ToString() == AllyCode).FirstOrDefault();
                if (me != null && !string.IsNullOrWhiteSpace(me.Name))
                    PlayerName = me.Name;
            }
            LoggedIn = true;
            CurrentActivity = "Ready";
        }

        /// <summary> Display guild roster. </summary>
        private void DoRoster()
        {
            // Ensure we have all the data we need
            if (!IsAllDataAvailable())
            {
                ShowError("Guild data has not yet been successfully retrieved.");
                return;
            }

            var vm = new RosterViewmodel(guild, Members);
            var view = new RosterView(vm) { Owner = parent };
            view.ShowDialog();
        }

        /// <summary> Display reverse roster lookup. </summary>
        private void DoWhoHas()
        {
            // Ensure we have all the data we need
            if (!IsAllDataAvailable())
            {
                ShowError("Guild data has not yet been successfully retrieved.");
                return;
            }

            var vm = new UnitLookupViewmodel() { Members = Members };
            var view = new UnitLookupView(vm) { Owner = parent };
            view.ShowDialog();
        }

        /// <summary> Display squad builder. </summary>
        private void DoSquadChecker()
        {
            // Ensure we have all the data we need
            if (!IsAllDataAvailable())
            {
                ShowError("Guild data has not yet been successfully retrieved.");
                return;
            }

            var vm = new SquadFinderViewmodel() { Members = Members };
            var view = new SquadFinderView(vm) { Owner = parent };
            view.ShowDialog();
        }

        #endregion


        /// <summary> Log into the swgoh.help service and pull complete guild data. </summary>
        /// <remarks>TODO: refactor and break up.</remarks>
        private async void PullData()
        {
            if (!api.IsLoggedIn)
            {
                // Verify that all necessary information has been provided
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(AllyCode))
                {
                    ShowError("Please provide all credentials.");
                    return;
                }

                // Set up API helper
                api.UpdateCredentials(Username, Password, UserId, AllyCode);
                if (!api.AllInputsProvided)
                {
                    ShowError("Credentials not provided or invalid");
                    return;
                }

                // Try to log in
                CurrentActivity = "Logging in";
                var result = false;
                try
                {
                    result = await api.LogIn();
                }
                catch (Exception e)
                {
                    ShowError($"Error:\n{e.Message}");
                }

                LoggedIn = result;
                if (!LoggedIn)
                {
                    CurrentActivity = "No data available";
                    return;
                }
            }

            // If we successfully logged in, store these settings to re-use next time
            Properties.Settings.Default.username = Username;
            Properties.Settings.Default.userid = UserId;
            Properties.Settings.Default.allycode = AllyCode;
            Properties.Settings.Default.Save();

            CurrentActivity = "Fetching guild information";
            GuildInfo resp = null;
            bool success = false;

            // Fetch guild information
            try
            {
                resp = await api.GetGuildInfo(api.AllyCode);
                success = true;
            }
            catch (Exception e)
            {
                ShowError($"Error fetching guild info:\n{e.Message}");
            }

            // Update UI and VM with results
            if (!success)
            {
                guild = null;
                Members.Clear();
                rawPlayerInfo = null;
                PlayerName = "";
                GuildName = "";
                CurrentActivity = "No data available";
                return;
            }
            guild = resp;
            // NOTE: It would be better to pull the player's info first and check if they were in a guild.
            //       The API is so slow, though, that it saves a noticible amount of time to blindly pull
            //       guild info first and try to catch the error.  If this becomes unmanageably awkward,
            //       change it to work the other way.
            if (guild != null)
            {
                // Player is in a guild, pull roster info and unlock full UI
                PlayerName = guild.roster.Where(p => p.allyCode == api.AllyCode.Value).FirstOrDefault().name;
                GuildName = guild.name;

                // Fetch player info
                CurrentActivity = "Fetching member details";
                var codes = guild.roster.Select(r => new AllyCode((uint)r.allyCode));
                List<PlayerInfo> pinfo = null;
                success = false;
                try
                {
                    pinfo = await api.GetPlayerInfo(codes);
                    success = true;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    ShowError($"Error deserializing JSON:\n{e.Message}");
                }
                catch (Exception e)
                {
                    ShowError($"Error fetching members:\n{e.Message}");
                }

                // Update UI and VM with results
                Members.Clear();
                rawPlayerInfo = null;
                if (!success)
                {
                    CurrentActivity = "No data available";
                    return;
                }

                rawPlayerInfo = pinfo;

                // Build task to fetch title metadata
                List<TitleInfo> _titles = new List<TitleInfo>();
                var title_task = api.GetTitleInfo();
                var tt = title_task.ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                    {
                        task.Exception.Handle(e =>
                        {
                            if (e is Newtonsoft.Json.JsonReaderException)
                                ShowError($"Error deserializing JSON:\n{e.Message}");
                            else
                                ShowError($"Error fetching titles:\n{e.Message}");
                            return true;
                        });

                        return;
                    }

                    _titles = task.Result;
                });

                // Build task to fetch relic power metadata
                var relic_task = api.GetRelicMetadata();
                var rt = relic_task.ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                    {
                        task.Exception.Handle(e =>
                        {
                            if (e is Newtonsoft.Json.JsonReaderException)
                                ShowError($"Error deserializing JSON:\n{e.Message}");
                            else
                                ShowError($"Error fetching relic metadata:\n{e.Message}");
                            return true;
                        });

                        return;
                    }

                    int[] relic_modifiers = task.Result;
                    if (relic_modifiers.Length == 7)
                    {
                        // Compute what the game would show for each unit's power
                        foreach (var player in rawPlayerInfo)
                        {
                            foreach (var character in player.roster)
                            {
                                character.TruePower = character.gp;
                                if (character.combatType == Character.COMBATTYPE_CHARACTER && character.gear >= 13 && character.relic.currentTier > 2)
                                {
                                    character.TruePower += (long)Math.Round(relic_modifiers[character.relic.currentTier - 3] * GP_PER_RELIC_SCALE_FACTOR);
                                }
                            }
                        }
                    }
                });

                // Run both tasks in parallel and wait for them to complete
                CurrentActivity = "Fetching game data";
                await Task.WhenAll(new Task[]{ title_task, relic_task });


                // Build roster
                foreach (var player in rawPlayerInfo)
                {
                    Player p = new Player(player);

                    // Decode title
                    string selected_title = player.titles.selected;
                    if (!string.IsNullOrWhiteSpace(selected_title) && _titles.Any(t => t.id == selected_title))
                    {
                        p.CurrentTitle = _titles.First(t => t.id == selected_title).name;
                    }

                    Members.Add(p);
                }

                CurrentActivity = "Ready";
            }
            else
            {
                // Player doesn't seem to be in a guild, try pulling individual player information
                CurrentActivity = "Fetching player details";
                var codes = new List<AllyCode>() { api.AllyCode };
                List<PlayerInfo> pinfo = null;
                success = false;
                try
                {
                    pinfo = await api.GetPlayerInfo(codes);
                    success = true;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    ShowError($"Error deserializing JSON:\n{e.Message}");
                }
                catch (Exception e)
                {
                    ShowError($"Error fetching player:\n{e.Message}");
                }

                if (success)
                {
                    // Display player info
                    Player p = new Player(pinfo.First());
                    var vm = new PlayerDetailViewmodel() { Player = p };
                    var win = new PlayerDetailView(vm) { Owner = parent };
                    win.ShowDialog();

                    // Nothing else to do if no guild, so just close the program.
                    parent.Close();
                }
            }

        }

    }


    public class PlayerList : ObservableCollection<Player>
    {

    }
}
