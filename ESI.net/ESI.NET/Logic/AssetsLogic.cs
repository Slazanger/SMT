using ESI.NET.Models.Assets;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class AssetsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public AssetsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /characters/{character_id}/assets/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Item>>> ForCharacter(int page = 1)
            => await Execute<List<Item>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/assets/",
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
        /// /characters/{character_id}/assets/locations/
        /// </summary>
        /// <param name="item_ids"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ItemLocation>>> LocationsForCharacter(List<long> item_ids)
            => await Execute<List<ItemLocation>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/assets/locations/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                body: item_ids.ToArray(),
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/assets/names/
        /// </summary>
        /// <param name="item_ids"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ItemName>>> NamesForCharacter(List<long> item_ids)
            => await Execute<List<ItemName>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/assets/names/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                body: item_ids.ToArray(),
                token: _data.Token);


        /// <summary>
        /// /corporations/{corporation_id}/assets/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Item>>> ForCorporation(int page = 1)
            => await Execute<List<Item>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/assets/",
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
        /// /corporations/{corporation_id}/assets/locations/
        /// </summary>
        /// <param name="item_ids"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ItemLocation>>> LocationsForCorporation(List<long> item_ids)
            => await Execute<List<ItemLocation>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/corporations/{corporation_id}/assets/locations/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                body: item_ids.ToArray(),
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/assets/names/
        /// </summary>
        /// <param name="item_ids"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ItemName>>> NamesForCorporation(List<long> item_ids)
            => await Execute<List<ItemName>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/corporations/{corporation_id}/assets/names/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                body: item_ids.ToArray(),
                token: _data.Token);
    }
}