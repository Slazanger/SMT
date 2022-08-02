using ESI.NET.Models.Bookmarks;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class BookmarksLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public BookmarksLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /characters/{character_id}/bookmarks/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bookmark>>> ForCharacter(int page = 1)
            => await Execute<List<Bookmark>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/bookmarks/",
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
        /// /characters/{character_id}/bookmarks/folders/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Folder>>> FoldersForCharacter(int page = 1)
            => await Execute<List<Folder>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/bookmarks/folders/",
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
        /// /corporations/{corporation_id}/bookmarks/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bookmark>>> ForCorporation(int page = 1)
            => await Execute<List<Bookmark>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/bookmarks/",
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
        /// /corporations/{corporation_id}/bookmarks/folders/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Folder>>> FoldersForCorporation(int page = 1)
            => await Execute<List<Folder>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/bookmarks/folders/",
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