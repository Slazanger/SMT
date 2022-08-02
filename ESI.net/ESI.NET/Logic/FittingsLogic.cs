using ESI.NET.Models.Fittings;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class FittingsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public FittingsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /characters/{character_id}/fittings/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Fitting>>> List()
            => await Execute<List<Fitting>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/fittings/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/fittings/
        /// </summary>
        /// <param name="fitting"></param>
        /// <returns></returns>
        public async Task<EsiResponse<NewFitting>> Add(object fitting)
            => await Execute<NewFitting>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/fittings/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                body: fitting,
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/fittings/{fitting_id}/
        /// </summary>
        /// <param name="fitting_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> Delete(int fitting_id)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/characters/{character_id}/fittings/{fitting_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "fitting_id", fitting_id.ToString() }
                },
                token: _data.Token);
    }
}