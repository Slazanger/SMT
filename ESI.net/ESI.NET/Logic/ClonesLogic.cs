using ESI.NET.Models.Clones;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class ClonesLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public ClonesLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /characters/{character_id}/clones/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Clones>> List()
            => await Execute<Clones>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/clones/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/implants/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Implants()
            => await Execute<int[]>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/implants/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);
    }
}