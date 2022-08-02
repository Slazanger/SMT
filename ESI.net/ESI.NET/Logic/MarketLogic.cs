using ESI.NET.Enumerations;
using ESI.NET.Models.Market;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class MarketLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public MarketLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /markets/prices/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Price>>> Prices()
            => await Execute<List<Price>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/prices/");

        /// <summary>
        /// /markets/{region_id}/orders/
        /// </summary>
        /// <param name="region_id"></param>
        /// <param name="order_type"></param>
        /// <param name="page"></param>
        /// <param name="type_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> RegionOrders(
            int region_id, 
            MarketOrderType order_type = MarketOrderType.All, 
            int page = 1, 
            int? type_id = null)
        {
            var parameters = new List<string>() { $"order_type={order_type.ToEsiValue()}" };
            parameters.Add($"page={page}");

            if (type_id != null)
                parameters.Add($"type_id={type_id}");

            var response = await Execute<List<Order>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/{region_id}/orders/",
                replacements: new Dictionary<string, string>()
                {
                    { "region_id", region_id.ToString() }
                },
                parameters: parameters.ToArray());

            return response;
        }

        /// <summary>
        /// /markets/{region_id}/history/
        /// </summary>
        /// <param name="region_id"></param>
        /// <param name="type_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Statistic>>> TypeHistoryInRegion(int region_id, int type_id)
            => await Execute<List<Statistic>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/{region_id}/history/",
                replacements: new Dictionary<string, string>()
                {
                    { "region_id", region_id.ToString() }
                },
                parameters: new string[]
                {
                    $"type_id={type_id}"
                });

        /// <summary>
        /// /markets/structures/{structure_id}/
        /// </summary>
        /// <param name="structure_id"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> StructureOrders(long structure_id, int page = 1)
            => await Execute<List<Order>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/markets/structures/{structure_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "structure_id", structure_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /markets/groups/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Groups()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/groups/");

        /// <summary>
        /// /markets/groups/{market_group_id}/
        /// </summary>
        /// <param name="market_group_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Group>> Group(int market_group_id)
            => await Execute<Group>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/groups/{market_group_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "market_group_id", market_group_id.ToString() }
                });

        /// <summary>
        /// /characters/{character_id}/orders/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> CharacterOrders()
            => await Execute<List<Order>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/orders/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/orders/history/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> CharacterOrderHistory(int page = 1)
            => await Execute<List<Order>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/orders/history/",
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
        /// /markets/{region_id}/types/
        /// </summary>
        /// <param name="region_id"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Types(int region_id, int page = 1)
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/markets/{region_id}/types/",
                replacements: new Dictionary<string, string>()
                {
                    { "region_id", region_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                });

        /// <summary>
        /// /corporations/{corporation_id}/orders/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> CorporationOrders(int page = 1)
            => await Execute<List<Order>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/orders/",
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
        /// /corporations/{corporation_id}/orders/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Order>>> CorporationOrderHistory(int page = 1)
            => await Execute<List<Order>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/orders/history/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);
    }
}