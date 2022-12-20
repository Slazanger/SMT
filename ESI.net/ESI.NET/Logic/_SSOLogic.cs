using ESI.NET.Enumerations;
using ESI.NET.Models.Character;
using ESI.NET.Models.SSO;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ESI.NET
{
    public class SsoLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly string _clientKey;
        private readonly string _ssoUrl;

        private static Random random = new Random();

        public SsoLogic(HttpClient client, EsiConfig config)
        {
            _client = client;
            _config = config;
            switch (_config.DataSource)
            {
                case DataSource.Tranquility:
                    _ssoUrl = "login.eveonline.com";
                    break;
                case DataSource.Serenity:
                    _ssoUrl = "login.evepc.163.com";
                    break;
            }
            _clientKey = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.ClientId}:{config.SecretKey}"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="state"></param>
        /// <param name="code_challenge">All hashing/encryption will be done automatically. Just provide the code.</param>
        /// <param name=""></param>
        /// <returns></returns>
        public string CreateAuthenticationUrl(List<string> scope = null, string state = null, string challengeCode = null)
        {
            var url = $"https://{_ssoUrl}/v2/oauth/authorize/?response_type=code&redirect_uri={Uri.EscapeDataString(_config.CallbackUrl)}&client_id={_config.ClientId}";

            if (scope != null)
                url = $"{url}&scope={string.Join("+", scope.Distinct().ToList())}";

            if (state != null)
                url = $"{url}&state={state}";

            if (challengeCode != null)
            {
                url = $"{url}&code_challenge_method=S256";

                using (var sha256 = SHA256.Create())
                {
                    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(challengeCode)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(base64));
                    var code_challenge = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

                    url = $"{url}&code_challenge={code_challenge}";
                }
            }

            return url;
        }
        
        public static string GenerateChallengeCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// SSO Token helper
        /// </summary>
        /// <param name="grantType"></param>
        /// <param name="code">The authorization_code or the refresh_token</param>
        /// <param name="codeChallenge">Provide the same value that was provided for codeChallenge in CreateAuthenticationUrl(). All hashing/encryption will be done automatically. Just provide the code.</param>
        /// <returns></returns>
        public async Task<SsoToken> GetToken(GrantType grantType, string code, string codeChallenge = null)
        {
            var body = $"grant_type={grantType.ToEsiValue()}";
            if (grantType == GrantType.AuthorizationCode)
            {
                body += $"&code={code}";

                if (codeChallenge != null)
                {
                    var bytes = Encoding.ASCII.GetBytes(codeChallenge);
                    var base64 = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    body += $"&code_verifier={base64}&client_id={_config.ClientId}";
                }
            }   
            else if (grantType == GrantType.RefreshToken)
            {
                body += $"&refresh_token={Uri.EscapeDataString(code)}";

                if(codeChallenge != null)
                    body += $"&client_id={_config.ClientId}";
            }

            HttpContent postBody = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
            if(codeChallenge == null)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _clientKey);
                _client.DefaultRequestHeaders.Host = _ssoUrl;
            }

            var response = await _client.PostAsync($"https://{_ssoUrl}/v2/oauth/token", postBody);
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string message = "unknown";
                if(!string.IsNullOrEmpty(content))
                {
                    message = JsonConvert.DeserializeAnonymousType(content, new { error_description = string.Empty }).error_description;
                }
                throw new ArgumentException(message);
            }

            var token = JsonConvert.DeserializeObject<SsoToken>(content);

            return token;
        }

        /// <summary>
        /// SSO Token revokation helper
        /// ESI will invalidate the provided refreshToken
        /// </summary>
        /// <param name="code">refresh_token to revoke</param>
        /// <returns></returns>
        public async Task RevokeToken(string code)
        {
            var body = $"token_type_hint={GrantType.RefreshToken.ToEsiValue()}";
            body += $"&token={Uri.EscapeDataString(code)}";

            HttpContent postBody = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _clientKey);

            var response = await _client.PostAsync($"https://{_ssoUrl}/v2/oauth/revoke", postBody);
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var error = JsonConvert.DeserializeAnonymousType(content, new { error_description = string.Empty }).error_description;
                throw new ArgumentException(error);
            }
        }

        /// <summary>
        /// Verifies the Character information for the provided Token information.
        /// While this method represents the oauth/verify request, in addition to the verified data that ESI returns, this object also stores the Token and Refresh token
        /// and this method also uses ESI retrieves other information pertinent to making calls in the ESI.NET API. (alliance_id, corporation_id, faction_id)
        /// You will need a record in your database that stores at least this information. Serialize and store this object for quick retrieval and token refreshing.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<AuthorizedCharacterData> Verify(SsoToken token)
        {
            AuthorizedCharacterData authorizedCharacter = new AuthorizedCharacterData();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Get the eve online JWT to validate against
                var jwtksUrl = $"https://{_ssoUrl}/oauth/jwks";
                var response = await _client.GetAsync(jwtksUrl).Result.Content.ReadAsStringAsync();
                var jwks = new JsonWebKeySet(response);
                var jwk = jwks.Keys.First();

                SecurityToken validatedToken;

                // Validate the token
                TokenValidationParameters tokenValidationParams = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuer = _ssoUrl,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwk,
                    ClockSkew = TimeSpan.FromSeconds(2), // CCP's servers seem slightly ahead (~1s)
                };
                tokenHandler.ValidateToken(token.AccessToken, tokenValidationParams, out validatedToken);

                JwtSecurityToken jwtValidatedToken = validatedToken as JwtSecurityToken;

                var subjectClaim = jwtValidatedToken.Claims.SingleOrDefault(c => c.Type == "sub").Value;
                var nameClaim = jwtValidatedToken.Claims.SingleOrDefault(c => c.Type == "name").Value;
                var ownerClaim = jwtValidatedToken.Claims.SingleOrDefault(c => c.Type == "owner").Value;
                
                var returnedScopes = jwtValidatedToken.Claims.Where(c => c.Type == "scp");
                var scopesClaim = string.Join(" ", returnedScopes.Select(s => s.Value));

                authorizedCharacter.RefreshToken = token.RefreshToken;
                authorizedCharacter.Token = token.AccessToken;
                authorizedCharacter.CharacterName = nameClaim;
                authorizedCharacter.CharacterOwnerHash = ownerClaim;
                authorizedCharacter.CharacterID = int.Parse(subjectClaim.Split(':').Last());
                authorizedCharacter.ExpiresOn = jwtValidatedToken.ValidTo;
                authorizedCharacter.Scopes = scopesClaim;

                // Get more specifc details about authorized character to be used in API calls that require this data about the character
                var url = $"{_config.EsiUrl}latest/characters/affiliation/?datasource={_config.DataSource.ToEsiValue()}";
                var body = new StringContent(JsonConvert.SerializeObject(new int[] { authorizedCharacter.CharacterID }), Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var characterResponse = await client.PostAsync(url, body).ConfigureAwait(false);

                if (characterResponse.StatusCode == HttpStatusCode.OK)
                {
                    EsiResponse<List<Affiliation>> affiliations = new EsiResponse<List<Affiliation>>(characterResponse, "Post|/character/affiliations/");
                    var characterData = affiliations.Data.First();

                    authorizedCharacter.AllianceID = characterData.AllianceId;
                    authorizedCharacter.CorporationID = characterData.CorporationId;
                    authorizedCharacter.FactionID = characterData.FactionId;
                }
            }
            catch
            {
                // validation failed
            }

            return authorizedCharacter;
        }
    }
}
