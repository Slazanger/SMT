using ESI.NET.Models.Contacts;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class ContactsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;

        private readonly int character_id, corporation_id, alliance_id;

        public ContactsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (_data != null)
            {
                character_id = _data.CharacterID;
                corporation_id = _data.CorporationID;
                alliance_id = _data.AllianceID;
            }
        }

        /// <summary>
        /// /characters/{character_id}/contacts/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contact>>> ListForCharacter(int page = 1)
            => await Execute<List<Contact>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/contacts/",
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
        /// /corporations/{corporation_id}/contacts/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contact>>> ListForCorporation(int page = 1)
            => await Execute<List<Contact>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/contacts/",
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
        /// /alliances/{alliance_id}/contacts/
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Contact>>> ListForAlliance(int page = 1)
            => await Execute<List<Contact>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/alliances/{alliance_id}/contacts/",
                replacements: new Dictionary<string, string>()
                {
                    { "alliance_id", alliance_id.ToString() }
                },
                parameters: new string[]
                {
                    $"page={page}"
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/contacts/
        /// </summary>
        /// <param name="contact_ids"></param>
        /// <param name="standing"></param>
        /// <param name="label_ids"></param>
        /// <param name="watched"></param>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Add(int[] contact_ids, decimal standing, int[] label_ids = null, bool? watched = null)
        {
            var body = contact_ids;

            var parameters = new List<string>() { $"standing={standing}" };

            if (label_ids != null)
                parameters.Add($"label_ids={string.Join(",", label_ids)}");

            if (watched != null)
                parameters.Add($"watched={watched}");

            return await Execute<int[]>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/contacts/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: parameters.ToArray(),
                body: body,
                token: _data.Token);
        }

        /// <summary>
        /// /characters/{character_id}/contacts/
        /// </summary>
        /// <param name="contact_id"></param>
        /// <param name="standing"></param>
        /// <param name="label_id"></param>
        /// <param name="watched"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> Update(int[] contact_ids, decimal standing, int[] label_ids = null, bool? watched = null)
        {
            var body = contact_ids;

            var parameters = new List<string>() { $"standing={standing}" };

            if (label_ids != null)
                parameters.Add($"label_ids={string.Join(",", label_ids)}");

            if (watched != null)
                parameters.Add($"watched={watched}");

            return await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/characters/{character_id}/contacts/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: parameters.ToArray(),
                body: body,
                token: _data.Token);
        }

        /// <summary>
        /// /characters/{character_id}/contacts/
        /// </summary>
        /// <param name="contact_ids"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> Delete(int[] contact_ids)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/characters/{character_id}/contacts/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: new string[]
                {
                    $"contact_ids={string.Join(",", contact_ids)}"
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/contacts/labels/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Label>>> LabelsForCharacter()
            => await Execute<List<Label>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/contacts/labels/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/contacts/labels/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Label>>> LabelsForCorporation()
            => await Execute<List<Label>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/contacts/labels/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /alliances/{alliance_id}/contacts/labels/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Label>>> LabelsForAlliance()
            => await Execute<List<Label>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/alliances/{alliance_id}/contacts/labels/",
                replacements: new Dictionary<string, string>()
                {
                    { "alliance_id", alliance_id.ToString() }
                },
                token: _data.Token);
    }
}