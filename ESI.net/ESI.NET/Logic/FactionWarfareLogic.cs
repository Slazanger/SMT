using ESI.NET.Models.FactionWarfare;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class FactionWarfareLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public FactionWarfareLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
            {
                corporation_id = data.CorporationID;
                character_id = data.CharacterID;
            }
        }

        /// <summary>
        /// /fw/wars/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<War>>> List()
            => await Execute<List<War>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/wars/");

        /// <summary>
        /// /fw/stats/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Stat>>> Stats()
            => await Execute<List<Stat>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/stats/");

        /// <summary>
        /// /fw/systems/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<FactionWarfareSystem>>> Systems()
            => await Execute<List<FactionWarfareSystem>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/systems/");

        /// <summary>
        /// fw/leaderboards/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Leaderboards<FactionTotal>>> Leaderboads()
            => await Execute<Leaderboards<FactionTotal>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/leaderboards/");

        /// <summary>
        /// /fw/leaderboards/corporations/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Leaderboards<CorporationTotal>>> LeaderboardsForCorporations()
            => await Execute<Leaderboards<CorporationTotal>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/leaderboards/corporations/");

        /// <summary>
        /// /fw/leaderboards/characters/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Leaderboards<CharacterTotal>>> LeaderboardsForCharacters()
            => await Execute<Leaderboards<CharacterTotal>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/fw/leaderboards/characters/");

        /// <summary>
        /// /corporations/{corporation_id}/fw/stats/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Stat>> StatsForCorporation()
            => await Execute<Stat>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/fw/stats/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/fw/stats/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<Stat>> StatsForCharacter()
            => await Execute<Stat>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/fw/stats/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);
    }
}