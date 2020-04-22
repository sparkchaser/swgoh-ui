using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace goh_ui
{
    /// <summary>
    /// Wrapper around the swgoh.help web APIs.
    /// </summary>
    public class ApiWrapper : IDisposable
    {

        #region Public properties

        public string Username { get; set; }
        public string Password { get; set; }
        public uint UserId { get; set; } = 0;
        public AllyCode AllyCode { get; set; } = AllyCode.None;

        #endregion

        #region Internal fields

        /// <summary> Authentication token to use with web API. </summary>
        private AccessToken token =  AccessToken.NullToken;

        /// <summary> Client for sending HTTP requests. </summary>
        private HttpClient client = null;

        #endregion

        #region Internal constants

        private static readonly string URL_BASE = "https://api.swgoh.help/";
        private static readonly string URL_SIGNIN = "auth/signin";
        private static readonly string URL_PLAYER = "swgoh/players";
        private static readonly string URL_GUILDS = "swgoh/guilds";
        private static readonly string URL_DATA = "swgoh/data";

        #endregion

        public ApiWrapper()
        {

        }

        /// <summary> Load new API credentials, and automatically invalidate tokens if anything changed. </summary>
        public void UpdateCredentials(string username, string password, string user_id, string ally_code)
        {
            bool dirty = false;

            if (username != Username)
            {
                Username = username;
                dirty = true;
            }

            if (password != Password)
            {
                Password = password;
                dirty = true;
            }

            if (uint.TryParse(user_id, out uint val))
            {
                if (val != UserId)
                {
                    UserId = val;
                    dirty = true;
                }
            }

            AllyCode ac = new AllyCode(ally_code);
            if (ac != AllyCode)
            {
                AllyCode = ac;
                dirty = true;
            }

            // Invalidate existing tokens if anything changed
            if (dirty)
            {
                token = new AccessToken("", -9999);
            }
        }

        /// <summary> Indicates whether all necessary inputs have been provided. </summary>
        public bool AllInputsProvided
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                    return false;
                if (UserId == 0)
                    return false;
                if (AllyCode == AllyCode.None)
                    return false;

                return true;
            }
        }

        /// <summary> Indicates whether the user is logged in and has a valid API token. </summary>
        public bool IsLoggedIn => token.IsValid;


        #region API functions

        /// <summary> Log into the server and get an access token. </summary>
        /// <returns>true if logged in successfully, false otherwise.</returns>
        public async Task<bool> LogIn()
        {
            if (!AllInputsProvided)
                throw new InvalidOperationException("Login information has not been provided.");

            // Set up HTTP Client, if needed
            if (client == null)
            {
                client = new HttpClient()
                {
                    BaseAddress = new Uri(URL_BASE)
                };
            }

            // Build request
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username",Username),
                new KeyValuePair<string, string>("password",Password),
                new KeyValuePair<string, string>("grant_type","password"),
                new KeyValuePair<string, string>("client_id",UserId.ToString()),
                new KeyValuePair<string, string>("client_secret","ABC") // TODO: generate random secret
            });

            // Send request
            var result = await client.PostAsync(URL_SIGNIN, content);
            if (!result.IsSuccessStatusCode)
            {
                // TODO: handle errors
                System.Windows.MessageBox.Show(result.ReasonPhrase, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            // Parse response
            string resp = await result.Content.ReadAsStringAsync();
            var r = JsonConvert.DeserializeObject<LoginResponse>(resp);
            token = new AccessToken(r.access_token, r.expires_in);
            DebugMessage("Logged in with access token: " + r.access_token);
            
            return true;
        }

        /// <summary> Fetch information about a guild. </summary>
        /// <param name="code">Ally code of someone in the guild.</param>
        public async Task<GuildInfo> GetGuildInfo(AllyCode code)
        {
            string resp;
            try
            {
                resp = await MakeApiRequest(new PlayerGuildInfoCommand() { allycodes = new string[] { code.Value.ToString() } }, URL_GUILDS);
            }
            catch (ApiErrorException e)
            {
                if (e.Response == null || e.Response.ReasonPhrase != "None") // Don't error if player not in guild
                    DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Guild Info");
                return null;
            }

            GuildInfo[] retval = JsonConvert.DeserializeObject<GuildInfo[]>(resp);
            if (retval == null || retval.Length == 0)
            {
                return null;
            }

            return retval[0];
        }

        /// <summary> Fetch information about a player. </summary>
        /// <param name="code">Player's ally code.</param>
        public async Task<List<PlayerInfo>> GetPlayerInfo(IEnumerable<AllyCode> codes)
        {
            string resp;
            try
            {
                resp = await MakeApiRequest(new PlayerGuildInfoCommand() { allycodes = codes.Select(c => c.Value.ToString()).ToArray() }, URL_PLAYER);
            }
            catch (ApiErrorException e)
            {
                DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Player Info");
                return null;
            }

            PlayerInfo[] retval = JsonConvert.DeserializeObject<PlayerInfo[]>(resp);
            if (retval == null || retval.Length == 0)
            {
                return null;
            }

            return new List<PlayerInfo>(retval);
        }

        /// <summary> Fetch metadata mapping IDs to player titles. </summary>
        public async Task<List<TitleInfo>> GetTitleInfo()
        {
            string resp;
            try
            {
                resp = await MakeApiRequest(new GameDataCommand() { collection = "playerTitleList" }, URL_DATA);
            }
            catch (ApiErrorException e)
            {
                DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Title Info");
                return null;
            }

            TitleInfo[] retval = JsonConvert.DeserializeObject<TitleInfo[]>(resp);
            if (retval == null || retval.Length == 0)
            {
                return null;
            }

            return new List<TitleInfo>(retval);
        }

        /// <summary> Fetch relic tier GP bonus info. </summary>
        public async Task<int[]> GetRelicMetadata()
        {
            string resp;
            try
            {
                var payload = new GameDataCommand() { collection = "tableList" };
                payload.match = new Dictionary<string, object>()
                {
                    { "id", "galactic_power_per_relic_tier" }
                };
                resp = await MakeApiRequest(payload, URL_DATA);
            }
            catch (ApiErrorException e)
            {
                DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Relic Info");
                return new int[] { };
            }

            DataTable[] retval = JsonConvert.DeserializeObject<DataTable[]>(resp);
            if (retval == null || retval.Length == 0)
            {
                return new int[] { };
            }

            return retval.First().rowList
                         .Select(r => int.TryParse(r.value, out int val) ? val : -1)
                         .OrderBy(x => x)
                         .ToArray();
        }

        /// <summary> Retrieve misc. data about each defined character. </summary>
        /// <remarks> This data is not included in what gets returned for a player's roster. </remarks>
        public async Task<UnitDetails[]> GetUnitDetails()
        {
            string resp;
            try
            {
                // This table contains an enormous amount of information.
                // To avoid downloading hundreds of MB of data, filter down to
                // only one version of each player-unlockable character, then
                // limit the fields returned to the few that we can actually use.
                var payload = new GameDataCommand() { collection = "unitsList" };
                payload.match = new Dictionary<string, object>()
                {
                    { "obtainable", true },
                    { "rarity", 7 }
                };
                payload.project = new Dictionary<string, object>()
                {
                    { "baseId", 1 },
                    { "forceAlignment", 1 },
                    { "combatType", 1 },
                    { "categoryIdList", 1 },
                    { "nameKey", 1 }
                };
                resp = await MakeApiRequest(payload, URL_DATA);
            }
            catch (ApiErrorException e)
            {
                DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Relic Info");
                return new UnitDetails[] { };
            }

            UnitDetails[] units = JsonConvert.DeserializeObject<UnitDetails[]>(resp);
            if (units == null || units.Length == 0)
            {
                return new UnitDetails[] { };
            }

            // Now fetch the 'categoryList' table
            resp = null;
            try
            {
                var payload = new GameDataCommand() { collection = "categoryList" };
                resp = await MakeApiRequest(payload, URL_DATA);
            }
            catch (ApiErrorException e)
            {
                DisplayError(e.Response == null ? e.Message : e.Response.ReasonPhrase, "Relic Info");
                return new UnitDetails[] { };
            }

            Category[] categories = JsonConvert.DeserializeObject<Category[]>(resp);
            if (categories == null || categories.Length == 0)
            {
                return new UnitDetails[] { };
            }

            // Map "categoryIdList" values to localized strings
            foreach (var unit in units)
            {
                List<string> newtags = new List<string>();
                foreach (string cat in unit.categoryIdList)
                {
                    // Ignore "self-tags", they're not interesting to us
                    if (cat.StartsWith("selftag_"))
                        continue;

                    var match = categories.FirstOrDefault(c => c.id == cat);
                    if (match != null)
                    {
                        //if (!match.visible) // disable for now, this info might be interesting
                        //    continue;
                        if (match.descKey != "Placeholder" && !match.descKey.Contains("_"))
                            newtags.Add(match.descKey);
                        // Add manual translation for interesting hidden categories with no official description
                        else if (missing_category_translations.ContainsKey(match.id))
                            newtags.Add(missing_category_translations[match.id]);
                    }
                }
                unit.categoryIdList = newtags.Distinct().OrderBy(c => c).ToArray();
            }

            return units;
        }

        /// <summary> Category-to-description mappings that are missing from the official game data. </summary>
        private static readonly Dictionary<string, string> missing_category_translations = new Dictionary<string, string>()
        {
            { "summoned_unit", "Summoned" },
            { "shipclass_fighter", "Fighter" },
            { "shipclass_freighter", "Freighter" },
            { "shipclass_capitalship", "Capital Ship" },
            { "shipclass_corvette", "Corvette" },
            { "shipclass_dreadnaught", "Dreadnaught" },
            { "cannot_evade", "Cannot Evade" },
            { "suppress_death", "Suppress Death" },
            { "percent_health_resistant", "Percent Health Resistant" },
            { "reduced_massive_damage", "Reduced Massive Damage" },
            { "category_pacifist", "Pacifist" },
            { "no_recovery", "No Recovery" },
            { "role_none", "No Role" },
            { "affiliation_galactic_republic_jedi", "Galactic Republic Jedi" },
            { "affiliation_501st_clone", "501st Clone Trooper" },
            { "affiliation_separatist_droid", "Separatist Droid" },
            { "affiliation_fomilitary", "First Order Military" },
            { "affiliation_foexecutionsquad", "First Order Execution Squad" },
            { "affiliation_foties", "First Order TIEs" },
            { "affiliation_kylos", "Kylo" },
            { "affiliation_reys", "Rey" },
            { "affiliation_resistancexwings", "Resistance X-Wings" },
            { "affiliation_dathbros", "Brothers of Dathomir" },
            { "affiliation_forcelightning", "Force Lightning" },
            { "affiliation_sithlord", "Sith Lord" },
            { "affiliation_palp", "Palpatine" },
            { "affiliation_order66", "Order 66" },
            { "affiliation_doubleblade", "Double-bladed Lightsaber" },
            { "affiliation_sabacc", "Sabacc" },
            { "affiliation_crimsondawn", "Crimson Dawn" },
            { "affiliation_smuggled", "Smuggled" },
            { "affiliation_rebfalconcrew", "Rebel Falcon Crew" },
            { "affiliation_resfalconcrew", "Resistance Falcon Crew" },
            { "affiliation_prisfalconcrew", "Original Falcon Crew" },
            { "unit_xwing", "X-Wing" },
            { "territory_hoth_hero_rebel", "Hoth Rebel Hero" },
            { "territory_hoth_empire", "Hoth Empire Hero" },
            { "territory_geonosis_separatist", "Geonosis Separatist Hero" },
            { "territory_geonosis_republic", "Geonosis Republic Hero" },
            { "territory_light_platoon", "TB Platoons - Light" },
            { "territory_dark_platoon", "TB Platoons - Dark" },
            { "gac_rebelpilot", "Rebel Pilot" },
            { "gac_tiepilot", "TIE Pilot" },
            { "gac_millenniumco-pilot", "Millennium Falcon Co-Pilot" },
            { "gac_droidpilot", "Droid Pilot" },
            { "gac_tie", "TIE" },
            { "gac_wingmate", "Luke's Wingmate" },
            { "gac_trench", "Trench Run" }
        };


        /// <summary> POST a request to the web API and return the result as a string. </summary>
        /// <param name="payload">Command payload (will be serialized to JSON).</param>
        /// <param name="requestUri">Relative URI to send request to.</param>
        /// <returns>Contents of the HTTP response, as a string.</returns>
        /// <exception cref="InvalidOperationException">User is not currently logged in.</exception>
        /// <exception cref="ApiErrorException">API request failed.</exception>
        private async Task<string> MakeApiRequest(object payload, string requestUri)
        {
            if (!token.IsValid)
            {
                throw new InvalidOperationException("Must be logged in to do that.");
            }

            // Build payload
            string data = JsonConvert.SerializeObject(payload);

            // Build request
            var msg = new HttpRequestMessage(HttpMethod.Post, requestUri);
            msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            msg.Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");

            // Send request
            HttpResponseMessage result;
            try
            {
                result = await client.SendAsync(msg);
            }
            catch (TaskCanceledException e)
            {
                throw new ApiErrorException(null, "Request timeout", e);
            }

            if (!result.IsSuccessStatusCode)
            {
                throw new ApiErrorException(result);
            }

            return await result.Content.ReadAsStringAsync();
        }

        /// <summary> Display an error message in a popup window. </summary>
        /// <param name="message">Error message</param>
        /// <param name="title">Window title</param>
        private void DisplayError(string message, string title) => System.Windows.MessageBox.Show(message, $"Error - {title}", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

        #endregion

        #region IDisposable methods

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        ~ApiWrapper()
        {
            Dispose();
        }

        #endregion


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

    /// <summary>
    /// Represents an error returned by the web API.
    /// </summary>
    public class ApiErrorException : Exception
    {
        /// <summary> HTTP response that triggered this exception. </summary>
        public HttpResponseMessage Response { get; private set; }

        public ApiErrorException(HttpResponseMessage response) : base(response.ReasonPhrase)
        {
            Response = response;
        }

        public ApiErrorException(HttpResponseMessage response, string message) : base(message)
        {
            Response = response;
        }

        public ApiErrorException(HttpResponseMessage response, string message, Exception innerException) : base(message, innerException)
        {
            Response = response;
        }
    }
}
