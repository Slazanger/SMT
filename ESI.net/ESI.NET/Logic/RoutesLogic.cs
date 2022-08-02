using ESI.NET.Enumerations;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class RoutesLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;

        public RoutesLogic(HttpClient client, EsiConfig config) { _client = client; _config = config; }

        /// <summary>
        /// /route/{origin}/{destination}/
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="flag"></param>
        /// <param name="avoid"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Map(
            int origin, 
            int destination, 
            RoutesFlag flag = RoutesFlag.Shortest, 
            int[] avoid = null, 
            int[] connections = null)
        {
            var parameters = new List<string>() { $"flag={flag.ToEsiValue()}" };

            if (avoid != null)
                parameters.Add($"&avoid={string.Join(",", avoid)}");

            if (connections != null)
                parameters.Add($"&connections={string.Join(",", connections)}");

            var response = await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/route/{origin}/{destination}/",
                replacements: new Dictionary<string, string>()
                {
                    { "origin", origin.ToString() },
                    { "destination", destination.ToString() }
                },
                parameters: parameters.ToArray());

            return response;
        }
    }
}