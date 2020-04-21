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
                if (e.Response.ReasonPhrase != "None") // Don't error if player not in guild
                    DisplayError(e.Response.ReasonPhrase, "Guild Info");
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
                DisplayError(e.Response.ReasonPhrase, "Player Info");
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
                DisplayError(e.Response.ReasonPhrase, "Title Info");
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
                DisplayError(e.Response.ReasonPhrase, "Relic Info");
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
            var result = await client.SendAsync(msg);
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
