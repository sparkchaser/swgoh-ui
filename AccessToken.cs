using System;
using System.Collections.Generic;
using System.Text;

namespace goh_ui
{
    /// <summary>
    /// Helper class for managing swgoh.help's web API's access tokens.
    /// </summary>
    public struct AccessToken : IEquatable<AccessToken>
    {
        /// <param name="token">Access token value.</param>
        /// <param name="expires_in">Time (seconds) until expiry.</param>
        public AccessToken(string token, int expires_in)
        {
            this.token = token;
            if (expires_in >= 0)
                Expiration = DateTime.Now.AddSeconds(expires_in * 0.90); // give some buffer for sanity
            else
                Expiration = new DateTime(1970, 1, 1);
        }

        /// <summary> The underlying access token. </summary>
        private readonly string token;

        /// <summary> Access token to use with the web API. </summary>
        public string Token
        {
            get
            {
                if (HasExpired)
                    throw new ExpiredTokenException();
                return token;
            }
        }

        /// <summary> Time when this token will expire. </summary>
        public DateTime Expiration { get; }

        /// <summary> Whether or not this token has expired. </summary>
        public bool HasExpired => DateTime.Now >= Expiration;

        /// <summary> Indicates whether this access token is valid (initialized and not yet expired). </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(token) && !HasExpired;


        #region IEquatable methods

        public bool Equals(AccessToken at) => token.Equals(at.token) && Expiration.Equals(at.Expiration);

        #endregion

        /// <summary> Token equivalent of a null value.  An invalid token. </summary>
        public static AccessToken NullToken { get; } = new AccessToken("", -999999);
    }

    /// <summary>
    /// Exception thrown when an access token has expired.
    /// </summary>
    public class ExpiredTokenException : Exception
    {

    }
}
