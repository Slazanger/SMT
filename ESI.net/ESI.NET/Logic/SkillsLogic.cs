using ESI.NET.Models.Skills;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class SkillsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public SkillsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /characters/{character_id}/attributes/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Attributes>> Attributes()
            => await Execute<Attributes>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/attributes/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/skills/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<SkillDetails>> List()
            => await Execute<SkillDetails>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/skills/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/skillqueue/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<SkillQueueItem>>> Queue()
            => await Execute<List<SkillQueueItem>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/skillqueue/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);
    }
}