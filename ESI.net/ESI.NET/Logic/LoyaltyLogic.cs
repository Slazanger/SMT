using ESI.NET.Models.Loyalty;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class LoyaltyLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public LoyaltyLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /loyalty/stores/{corporation_id}/offers/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Offer>>> Offers(int corporation_id)
            => await Execute<List<Offer>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/loyalty/stores/{corporation_id}/offers/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                });

        /// <summary>
        /// /characters/{character_id}/loyalty/points/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Points>>> Points()
            => await Execute<List<Points>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/loyalty/points/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);
    }
}