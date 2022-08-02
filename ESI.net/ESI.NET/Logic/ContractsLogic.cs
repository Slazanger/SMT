using ESI.NET.Models.Contracts;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class ContractsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public ContractsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /contracts/public/{region_id}/
        /// </summary>
        /// <param name="region_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contract>>> Contracts(int region_id, int page = 1)
            => await Execute<List<Contract>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/contracts/public/{region_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "region_id", region_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                });

        /// <summary>
        /// /contracts/public/items/{contract_id}/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ContractItem>>> ContractItems(int contract_id, int page = 1)
            => await Execute<List<ContractItem>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/contracts/public/items/{contract_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                });

        /// <summary>
        /// "/contracts/public/bids/{contract_id}/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bid>>> ContractBids(int contract_id, int page = 1)
            => await Execute<List<Bid>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/contracts/public/bids/{contract_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                });

        /// <summary>
        /// /characters/{character_id}/contracts/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contract>>> CharacterContracts(int page = 1)
            => await Execute<List<Contract>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/contracts/",
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
        /// /characters/{character_id}/contracts/{contract_id}/items/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ContractItem>>> CharacterContractItems(int contract_id, int page = 1)
            => await Execute<List<ContractItem>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/contracts/{contract_id}/items/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/contracts/{contract_id}/bids/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bid>>> CharacterContractBids(int contract_id, int page = 1)
            => await Execute<List<Bid>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/contracts/{contract_id}/bids/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/contracts/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contract>>> CorporationContracts(int page = 1)
            => await Execute<List<Contract>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/contracts/",
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
        /// /corporations/{corporation_id}/contracts/{contract_id}/items/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ContractItem>>> CorporationContractItems(int contract_id, int page = 1)
            => await Execute<List<ContractItem>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/contracts/{contract_id}/items/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() },
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/contracts/{contract_id}/bids/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bid>>> CorporationContractBids(int contract_id, int page = 1)
            => await Execute<List<Bid>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/contracts/{contract_id}/bids/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() },
                    { "contract_id", contract_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);
    }
}