using ESI.NET.Models.Killmails;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class KillmailsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public KillmailsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
            {
                character_id = data.CharacterID;
                corporation_id = data.CorporationID;
            }
        }

        /// <summary>
        /// /characters/{character_id}/killmails/recent/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Killmail>>> ForCharacter(int page = 1)
            => await Execute<List<Killmail>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/killmails/recent/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/killmails/recent/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Killmail>>> ForCorporation(int page = 1)
            => await Execute<List<Killmail>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/killmails/recent/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /killmails/{killmail_id}/{killmail_hash}/
        /// </summary>
        /// <param name="killmail_hash">The killmail hash for verification</param>
        /// <param name="killmail_id">The killmail ID to be queried</param>
        /// <returns></returns>
        public async Task<EsiResponse<Information>> Information(string killmail_hash, int killmail_id)
            => await Execute<Information>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/killmails/{killmail_id}/{killmail_hash}/",
                replacements: new Dictionary<string, string>()
                {
                    { "killmail_id", killmail_id.ToString() },
                    { "killmail_hash", killmail_hash.ToString() }
                });
    }
}