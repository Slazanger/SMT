using ESI.NET.Models.Sovereignty;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class SovereigntyLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;

        public SovereigntyLogic(HttpClient client, EsiConfig config) { _client = client; _config = config; }

        /// <summary>
        /// /sovereignty/campaigns/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Campaign>>> Campaigns()
            => await Execute<List<Campaign>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/sovereignty/campaigns/");

        /// <summary>
        /// /sovereignty/map/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<SystemSovereignty>>> Systems()
            => await Execute<List<SystemSovereignty>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/sovereignty/map/");

        /// <summary>
        /// /sovereignty/structures/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Structure>>> Structures()
            => await Execute<List<Structure>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/sovereignty/structures/");
    }
}