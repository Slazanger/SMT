using ESI.NET.Models.Status;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class StatusLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;

        public StatusLogic(HttpClient client, EsiConfig config) { _client = client; _config = config; }

        public async Task<EsiResponse<Status>> Retrieve()
            => await Execute<Status>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/status/");
    }
}