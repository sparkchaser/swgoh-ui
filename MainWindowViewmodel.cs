using goh_ui.Views;
using goh_ui.Viewmodels;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using goh_ui.Models;
using System.Threading;

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

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
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

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
        public bool CanExecute(object parameter) => buttonTest != null ? buttonTest.Invoke() : true;
        public void Execute(object parameter) => action?.Invoke(parameter);
    }

    public class MainWindowViewmodel : DependencyObject
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


        #region Dependency properties

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

        /// <summary> Whether to unlock tools that require certain data to be available. </summary>
        public bool UnlockTools
        {
            get { return (bool)GetValue(UnlockToolsProperty); }
            set { SetValue(UnlockToolsProperty, value); }
        }
        public static readonly DependencyProperty UnlockToolsProperty = _dp<bool>("UnlockTools");

        /// <summary> Whether to unlock the main "Fetch Data" button. </summary>
        public bool UnlockFetch
        {
            get { return (bool)GetValue(UnlockFetchProperty); }
            set { SetValue(UnlockFetchProperty, value); }
        }
        public static readonly DependencyProperty UnlockFetchProperty = _dp<bool>("UnlockFetch");

        #endregion


        /// <summary> List of guild members. </summary>
        public PlayerList Members { get; set; } = new PlayerList();
        

        /// <summary> Event invoked when the viewmodel wants to close the program. </summary>
        public event EventHandler ExitEvent;

        #region Button/menu handlers

        /// <summary> Handler for the 'get guild info' button. </summary>
        public SimpleCommand GetGuildCommand { get; private set; }

        public SimpleCommand ExitCommand { get; private set; }
        public SimpleCommand ExportCommand { get; private set; }
        public SimpleCommand ImportCommand { get; private set; }
        public SimpleCommand RefreshDataCommand { get; private set; }
        public SimpleCommand RosterCommand { get; private set; }
        public SimpleCommand WhoHasCommand { get; private set; }
        public SimpleCommand SquadCheckerCommand { get; private set; }
        public SimpleCommand PitReportCommand { get; private set; }
        public SimpleCommand AllianceCommand { get; private set; }
        public SimpleCommand ZetasCommand { get; private set; }
        public SimpleCommand ShowAbout { get; private set; }

        #endregion

        #region File/folder paths

        /// <summary> Directory where the program stores settings and cached data. </summary>
        private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "swgoh-ui");

        /// <summary> File where misc. game metadata is cached. </summary>
        private static readonly string GameDataFile = Path.Combine(SettingsDirectory, "game_data.json");

        /// <summary> File where cached program settings are stored. </summary>
        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "settings.json");

        #endregion

        private ApiWrapper api;

        private GuildInfo guild;

        private GameData gameData;

        private ProgramSettings settings;

        private ProgramState state = ProgramState.UNKNOWN;

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
            RefreshDataCommand = new SimpleCommand(DoRefreshData);
            RosterCommand = new SimpleCommand(DoRoster);
            WhoHasCommand = new SimpleCommand(DoWhoHas);
            SquadCheckerCommand = new SimpleCommand(DoSquadChecker);
            PitReportCommand = new SimpleCommand(DoPitReport);
            AllianceCommand = new SimpleCommand(DoAllianceReport);
            ZetasCommand = new SimpleCommand(DoZetas);
            ShowAbout = new SimpleCommand(() => AboutWindow.Display(this.parent));

            api = new ApiWrapper();

            // Load settings and metadata from disk
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);

            settings = ProgramSettings.LoadOrCreate(SettingsFile);
            gameData = GameData.LoadOrCreate(GameDataFile);
            DebugMessage($"Game Data is {(gameData.HasData() ? "present" : "missing")}, {(gameData.IsOutdated() ? "outdated" : "up-to-date")}");

            UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);

            // Pull user settings from last time
            Username = settings.username;
            UserId = settings.userid;
            AllyCode = settings.allycode;
        }

        #region Track and manage program state

        /// <summary>
        /// Description of the program's current state in the data loading process.
        /// </summary>
        private enum ProgramState
        {
            UNKNOWN = 0,
            READY,
            NO_DATA_AVAILABLE,
            LOGGING_IN,
            GETTING_GUILD_DATA,
            GETTING_GUILD_MEMBER_DATA,
            GETTING_PLAYER_DATA,
            GETTING_MISC_DATA
        }

        /// <summary> Change the program's current state and update state-dependent values. </summary>
        /// <param name="new_state">New program state.</param>
        private void UpdateProgramState(ProgramState new_state)
        {
            // Update status bar message
            switch(new_state)
            {
                case ProgramState.NO_DATA_AVAILABLE:
                    CurrentActivity = "No data available";
                    break;
                case ProgramState.READY:
                    CurrentActivity = "Ready";
                    break;
                case ProgramState.LOGGING_IN:
                    CurrentActivity = "Logging in";
                    break;
                case ProgramState.GETTING_GUILD_DATA:
                    CurrentActivity = "Fetching guild information";
                    break;
                case ProgramState.GETTING_GUILD_MEMBER_DATA:
                    CurrentActivity = "Fetching member details";
                    break;
                case ProgramState.GETTING_PLAYER_DATA:
                    CurrentActivity = "Fetching player details";
                    break;
                case ProgramState.GETTING_MISC_DATA:
                    CurrentActivity = "Fetching game data";
                    break;
                default:
                    new_state = ProgramState.UNKNOWN;
                    break;
            }

            // Update GUI enable/disable flags
            UnlockTools = new_state == ProgramState.READY;
            UnlockFetch = new_state == ProgramState.READY || new_state == ProgramState.NO_DATA_AVAILABLE;

            state = new_state;
        }

        #endregion

        /// <summary> Display an error message in a dialog box. </summary>
        /// <param name="message">Message to display.</param>
        private void ShowError(string message)
        {
            DebugMessage(message);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary> Indicates whether all guild and player data has been successfully retrieved. </summary>
        public bool IsAllDataAvailable()
        {
            if (guild == null)
                return false;

            if (Members == null)
                return false;

            if (rawPlayerInfo == null)
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
            UpdateProgramState(ProgramState.READY);

            // Build roster
            if (gameData.HasData())
            {
                ComputeTruePower(rawPlayerInfo, gameData.RelicMultipliers);
                BuildRoster(rawPlayerInfo, gameData.Titles);
            }
        }

        /// <summary> Invalidate our misc. game data and re-download it. </summary>
        private async void DoRefreshData()
        {
            DebugMessage("Game metadata update needed");
            ProgramState old_status = state;

            // Log in, if we aren't already
            if (!api.IsLoggedIn)
            {
                if (await DoLogin() == false)
                {
                    UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                    return;
                }
            }

            // Re-fetch data
            UpdateProgramState(ProgramState.GETTING_MISC_DATA);
            gameData = await UpdateGameData();

            if (!gameData.HasData() || gameData.IsOutdated())
            {
                UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                return;
            }

            // Cache game metadata
            GameData.Store(gameData, GameDataFile);

            // Re-build rosters
            ComputeTruePower(rawPlayerInfo, gameData.RelicMultipliers);
            BuildRoster(rawPlayerInfo, gameData.Titles);

            // Report status
            DebugMessage("Game data update complete");
            if (IsAllDataAvailable())
                UpdateProgramState(ProgramState.READY);
            else
                UpdateProgramState(old_status);
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

            var vm = new RosterViewmodel(guild, Members, gameData.Units);
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

            var vm = new UnitLookupViewmodel(Members, gameData.Units);
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

            var vm = new SquadFinderViewmodel(Members, gameData.Units);
            var view = new SquadFinderView(vm) { Owner = parent };
            view.ShowDialog();
        }

        /// <summary> Display challenge-mode Pit raid report. </summary>
        private void DoPitReport()
        {
            // Ensure we have all the data we need
            if (!IsAllDataAvailable())
            {
                ShowError("Guild data has not yet been successfully retrieved.");
                return;
            }

            var vm = new PitReportViewmodel(guild, Members);
            var view = new PitReportView(vm) { Owner = parent };
            view.ShowDialog();
        }

        /// <summary> Summary overview for an alliance of guilds. </summary>
        private void DoAllianceReport()
        {
            if (!api.IsLoggedIn)
            {
                ShowError("You must successfully connect to the web service before using this tool.");
                return;
            }

            var view = new AllianceView(api)
            {
                Owner = parent
            };
            view.AllyCodeList = settings.alliance.Select(x => new AllyCode(x)).ToList();
            view.ShowDialog();

            var vm = view.DataContext as AllianceViewModel;
            settings.alliance = vm.AllyCodes.Select(x => x.Value).ToArray();
            ProgramSettings.Store(settings, SettingsFile);
        }

        /// <summary> Display zeta recommendations. </summary>
        private void DoZetas()
        {
            if (gameData == null || gameData.Zetas == null)
            {
                ShowError("Game data has not yet been successfully retrieved.");
                return;
            }

            var view = new ZetaListView(gameData.Zetas) { Owner = parent };
            view.ShowDialog();
        }

        #endregion


        /// <summary> Log into the swgoh.help service and pull complete guild data. </summary>
        /// <remarks>TODO: refactor and break up.</remarks>
        private async void PullData()
        {
            if (!api.IsLoggedIn)
            {
                if (await DoLogin() == false)
                    return;
            }

            // If we successfully logged in, store these settings to re-use next time
            settings.username = Username;
            settings.userid = UserId;
            settings.allycode = AllyCode;
            ProgramSettings.Store(settings, SettingsFile);

            UpdateProgramState(ProgramState.GETTING_GUILD_DATA);
            GuildInfo resp = null;
            bool success = false;

            // Reuse existing information if it's relatively recent.
            // The server doesn't refresh too often, so don't set time window too small.
            if (guild == null || guild.Updated < DateTimeOffset.Now.AddHours(-8))
            {
                // Fetch guild information
                DebugMessage($"Guild Info: Start");
                try
                {
                    resp = await api.GetGuildInfo(api.AllyCode);
                    success = true;
                }
                catch (Exception e)
                {
                    ShowError($"Error fetching guild info:\n{e.Message}");
                }
                DebugMessage($"Guild Info: End");

                // Update UI and VM with results
                if (!success || resp == null)
                {
                    guild = null;
                    Members.Clear();
                    rawPlayerInfo = null;
                    PlayerName = "";
                    GuildName = "";
                    UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                    return;
                }
                guild = resp;
            }

            // NOTE: It would be better to pull the player's info first and check if they were in a guild.
            //       The API is so slow, though, that it saves a noticible amount of time to blindly pull
            //       guild info first and try to catch the error.  If this becomes unmanageably awkward,
            //       change it to work the other way.
            if (guild != null)
            {
                // Player is in a guild, pull roster info and unlock full UI
                PlayerName = guild.roster.Where(p => p.allyCode == api.AllyCode.Value).FirstOrDefault().name;
                GuildName = guild.name;

                if (rawPlayerInfo == null || rawPlayerInfo.First().Updated < DateTimeOffset.Now.AddHours(-8) || rawPlayerInfo.Count != guild.members)
                {
                    // Fetch player info
                    Members.Clear();
                    var codes = guild.roster.Select(r => new AllyCode((uint)r.allyCode));
                    var pdata = await UpdatePlayerData(codes);

                    if (pdata == null)
                    {
                        rawPlayerInfo = null;
                        return;
                    }

                    rawPlayerInfo = pdata;
                }

                if (!gameData.HasData() || gameData.IsOutdated())
                {
                    DebugMessage("Game metadata update needed");
                    UpdateProgramState(ProgramState.GETTING_MISC_DATA);
                    gameData = await UpdateGameData();

                    if (!gameData.HasData() || gameData.IsOutdated())
                    {
                        UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                        return;
                    }

                    // Cache game metadata
                    GameData.Store(gameData, GameDataFile);
                }

                // Build roster
                ComputeTruePower(rawPlayerInfo, gameData.RelicMultipliers);
                BuildRoster(rawPlayerInfo, gameData.Titles);

                UpdateProgramState(ProgramState.READY);
                DebugMessage("Ready");
            }
            else
            {
                // Player doesn't seem to be in a guild, try pulling individual player information
                UpdateProgramState(ProgramState.GETTING_PLAYER_DATA);
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
                    var vm = new PlayerDetailViewmodel(p);
                    var win = new PlayerDetailView(vm) { Owner = parent };
                    win.ShowDialog();

                    // Nothing else to do if no guild, so just close the program.
                    parent.Close();
                }
            }

        }

        /// <summary> Log into the web server and get an authentication token. </summary>
        /// <returns>True on success, false otherwise.</returns>
        private async Task<bool> DoLogin()
        {
            // Verify that all necessary information has been provided
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(AllyCode))
            {
                ShowError("Please provide all credentials.");
                return false;
            }

            // Set up API helper
            api.UpdateCredentials(Username, Password, UserId, AllyCode);
            if (!api.AllInputsProvided)
            {
                ShowError("Credentials not provided or invalid");
                return false;
            }

            // Try to log in
            UpdateProgramState(ProgramState.LOGGING_IN);
            DebugMessage($"Log In: Start");
            var result = false;
            try
            {
                result = await api.LogIn();
            }
            catch (Exception e)
            {
                ShowError($"Error:\n{e.Message}");
            }
            DebugMessage($"Log In: End");

            LoggedIn = result;
            if (!LoggedIn)
            {
                UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                return false;
            }

            return true;
        }

        /// <summary> Download detailed information for the players in a guild. </summary>
        /// <returns>A list of player information on success, null otherwise.</returns>
        private async Task<List<PlayerInfo>> UpdatePlayerData(IEnumerable<AllyCode> codes)
        {
            // Fetch player info
            UpdateProgramState(ProgramState.GETTING_GUILD_MEMBER_DATA);
            DebugMessage($"Player Info: Start");

            ConcurrentBag<PlayerInfo> pinfo = new ConcurrentBag<PlayerInfo>();

            // Fetch no more than 5 players' worth of data at a time to help avoid timeouts (can take 4-6 sec per player)
            // Also, limit number of simultaneous requests to 3 to avoid overloading the server.
            var chunks = codes.Select((item, idx) => new { item, idx }).GroupBy(x => x.idx / 5).Select(x => x.Select(y => y.item));
            var throttle = new SemaphoreSlim(3, 3);
            var tasks = chunks.Select(async chunk =>
            {
                await throttle.WaitAsync();
                try
                {
                    DebugMessage($"Fetching new group of players [{chunk.First()}]");
                    List<PlayerInfo> these_players;
                    try
                    {
                        these_players = await api.GetPlayerInfo(chunk);
                    }
                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        ShowError($"Error deserializing JSON:\n{e.Message}");
                        return;
                    }
                    catch (Exception e)
                    {
                        ShowError($"Error fetching members:\n{e.Message}");
                        return;
                    }

                    if (these_players != null)
                        foreach (var p in these_players)
                            pinfo.Add(p);
                    DebugMessage($"Finished group [{chunk.First()}]");
                }
                finally
                {
                    throttle.Release();
                }
            });
            await Task.WhenAll(tasks);
            DebugMessage($"Player Info: End");

            // Update UI and VM with results
            if (pinfo.Count != codes.Count())
            {
                UpdateProgramState(ProgramState.NO_DATA_AVAILABLE);
                return null;
            }

            return pinfo.ToList();
        }

        /// <summary> Download misc. game metadata. </summary>
        /// <returns>Up-to-date game data on success, default value on error.</returns>
        private async Task<GameData> UpdateGameData()
        {
            GameData data = new GameData();

            // Build task to fetch title metadata
            var title_task = api.GetTitleInfo();
            var tt = title_task.ContinueWith((task) =>
            {
                DebugMessage($"Title Data: End");
                if (task.IsFaulted)
                {
                    task.Exception.Handle(e =>
                    {
                        if (e is Newtonsoft.Json.JsonReaderException || e is Newtonsoft.Json.JsonSerializationException)
                            ShowError($"Error deserializing JSON:\n{e.Message}");
                        else
                            ShowError($"Error fetching titles:\n{e.Message}");
                        return true;
                    });

                    return;
                }

                data.Titles = task.Result.ToArray();
            });

            // Build task to fetch relic power metadata
            var relic_task = api.GetRelicMetadata();
            var rt = relic_task.ContinueWith((task) =>
            {
                DebugMessage($"Relic Data: End");
                if (task.IsFaulted)
                {
                    task.Exception.Handle(e =>
                    {
                        if (e is Newtonsoft.Json.JsonReaderException || e is Newtonsoft.Json.JsonSerializationException)
                            ShowError($"Error deserializing JSON:\n{e.Message}");
                        else
                            ShowError($"Error fetching relic metadata:\n{e.Message}");
                        return true;
                    });

                    return;
                }

                data.RelicMultipliers = task.Result;
            });

            // Build task to fetch detailed character metadata
            var units_task = api.GetUnitDetails();
            var ut = units_task.ContinueWith((task) =>
            {
                DebugMessage($"Unit Data: End");
                if (task.IsFaulted)
                {
                    task.Exception.Handle(e =>
                    {
                        if (e is Newtonsoft.Json.JsonReaderException || e is Newtonsoft.Json.JsonSerializationException)
                            ShowError($"Error deserializing JSON:\n{e.Message}");
                        else
                            ShowError($"Error fetching unit metadata:\n{e.Message}");
                        return true;
                    });

                    return;
                }

                data.Units = task.Result;
            });

            // Build task to fetch zeta recommendations
            var zeta_task = api.GetZetaHints();
            var zt = zeta_task.ContinueWith((task) =>
            {
                DebugMessage($"Zeta Data: End");
                if (task.IsFaulted)
                {
                    task.Exception.Handle(e =>
                    {
                        if (e is Newtonsoft.Json.JsonReaderException || e is Newtonsoft.Json.JsonSerializationException)
                            ShowError($"Error deserializing JSON:\n{e.Message}");
                        else
                            ShowError($"Error fetching zeta hints:\n{e.Message}");
                        return true;
                    });

                    return;
                }

                data.Zetas = task.Result;
            });


            // Run tasks in parallel and wait for them to complete
            DebugMessage($"Game Data: Start");
            try
            {
                await Task.WhenAll(new Task[] { title_task, relic_task, units_task, zeta_task });
                await Task.WhenAll(new Task[] { tt, rt, ut, zt });
            }
            catch (Exception e)
            {
                ShowError($"Error fetching game data:\n{e.Message}");
                return new GameData();
            }

            // Update timestamp so object will register as complete and up-to-date
            data.Updated = DateTime.Now;

            return data;
        }


        /// <summary> Process a guild's roster and compute the true GP for each character. </summary>
        /// <param name="guild_roster">List of guild members.</param>
        /// <param name="relic_bonuses">Data from the "galactic power per tier" game data table.</param>
        private static void ComputeTruePower(List<PlayerInfo> guild_roster, int[] relic_bonuses)
        {
            if (guild_roster == null || relic_bonuses == null || relic_bonuses.Length < 7)
                return;

            // Compute what the game would show for each unit's power
            foreach (var player in guild_roster)
            {
                foreach (var character in player.roster)
                {
                    character.TruePower = character.gp;
                    if (character.combatType == Character.COMBATTYPE_CHARACTER && character.gear >= 13 && character.relic.currentTier > 2)
                    {
                        if ((character.relic.currentTier - 3) < relic_bonuses.Length)
                            character.TruePower += (long)Math.Round(relic_bonuses[character.relic.currentTier - 3] * GP_PER_RELIC_SCALE_FACTOR);
                    }
                }
            }
        }

        /// <summary> Populate <see cref="Members"/> with data from a guild's roster. </summary>
        /// <param name="guild_roster"> List of guild members. </param>
        /// <param name="titles"> (Optional) Metadata for decoding player titles. </param>
        private void BuildRoster(List<PlayerInfo> guild_roster, IEnumerable<TitleInfo> titles)
        {
            if (guild_roster == null)
                return;

            Members.Clear();
            foreach (var player in guild_roster)
            {
                Player p = new Player(player);

                // Decode title
                if (titles != null)
                {
                    string selected_title = player.titles.selected;
                    if (!string.IsNullOrWhiteSpace(selected_title) && titles.Any(t => t.id == selected_title))
                    {
                        p.CurrentTitle = titles.First(t => t.id == selected_title).name;
                    }
                }

                Members.Add(p);
            }
        }


        /// <summary> Print a message to the debugger console (debug builds only). </summary>
        /// <param name="message">Message to print.</param>
        private void DebugMessage(string message)
        {
#if DEBUG
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            System.Diagnostics.Debug.WriteLine($"{timestamp}: {message}");
#endif
        }
    }


    public class PlayerList : ObservableCollection<Player>
    {

    }
}
