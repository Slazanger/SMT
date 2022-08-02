using ESI.NET.Models.Industry;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class IndustryLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public IndustryLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /industry/facilities/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Facility>>> Facilities()
            => await Execute<List<Facility>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/industry/facilities/");

        /// <summary>
        /// /industry/systems/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<SolarSystem>>> SolarSystemCostIndices()
            => await Execute<List<SolarSystem>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/industry/systems/");

        /// <summary>
        /// /characters/{character_id}/industry/jobs/
        /// </summary>
        /// <param name="include_completed"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Job>>> JobsForCharacter(bool include_completed = false)
            => await Execute<List<Job>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/industry/jobs/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: new string[]
                {
                    $"include_completed={include_completed}"
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mining/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Entry>>> MiningLedger(int page = 1)
            => await Execute<List<Entry>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/mining/",
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
        /// /corporation/{corporation_id}/mining/observers/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Observer>>> Observers(int page = 1)
            => await Execute<List<Observer>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporation/{corporation_id}/mining/observers/",
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
        /// /corporation/{corporation_id}/mining/observers/{observer_id}/
        /// </summary>
        /// <param name="observer_id"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ObserverInfo>>> ObservedMining(long observer_id, int page = 1)
            => await Execute<List<ObserverInfo>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporation/{corporation_id}/mining/observers/{observer_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() },
                    { "observer_id", observer_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/industry/jobs/
        /// </summary>
        /// <param name="include_completed"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Job>>> JobsForCorporation(bool include_completed = false, int page = 1)
            => await Execute<List<Job>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/industry/jobs/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                parameters: new string[]
                {
                    $"include_completed={include_completed}",
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporation/{corporation_id}/mining/extractions/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Extraction>>> Extractions()
            => await Execute<List<Extraction>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporation/{corporation_id}/mining/extractions/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                token: _data.Token);
    }
}