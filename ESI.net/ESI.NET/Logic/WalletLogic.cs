using ESI.NET.Models.SSO;
using ESI.NET.Models.Wallet;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class WalletLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public WalletLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /characters/{character_id}/wallet/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<decimal>> CharacterWallet()
            => await Execute<decimal>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/wallet/", replacements: new Dictionary<string, string>()
            {
                { "character_id", character_id.ToString() }
            }, token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/wallet/journal/
        /// </summary>
        /// <param name="from_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<JournalEntry>>> CharacterJournal(int page = 1)
            => await Execute<List<JournalEntry>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/wallet/journal/",
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
        /// /characters/{character_id}/wallet/transactions/
        /// </summary>
        /// <param name="from_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Transaction>>> CharacterTransactions(int page = 1)
            => await Execute<List<Transaction>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/wallet/transactions/",
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
        /// /corporations/{corporation_id}/wallets/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Wallet>>> CorporationWallets()
            => await Execute<List<Wallet>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/wallets/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/wallets/{division}/journal/
        /// </summary>
        /// <param name="division"></param>
        /// <param name="from_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<JournalEntry>>> CorporationJournal(int division, int page = 1)
            => await Execute<List<JournalEntry>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/wallets/{division}/journal/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() },
                    { "division", division.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/wallets/{division}/transactions/
        /// </summary>
        /// <param name="division"></param>
        /// <param name="from_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Transaction>>> CorporationTransactions(int division, int page = 1)
            => await Execute<List<Transaction>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/wallets/{division}/transactions/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() },
                    { "division", division.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);
    }
}