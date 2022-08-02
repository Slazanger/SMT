using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;
using model = ESI.NET.Models.Opportunities;

namespace ESI.NET.Logic
{
    public class OpportunitiesLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;
        
        public OpportunitiesLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /opportunities/groups/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Groups()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/opportunities/groups/");

        /// <summary>
        /// /opportunities/groups/{group_id}/
        /// </summary>
        /// <param name="group_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<model.Group>> Group(int group_id)
            => await Execute<model.Group>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/opportunities/groups/{group_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "group_id", group_id.ToString() }
                });

        /// <summary>
        /// /opportunities/tasks/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Tasks()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/opportunities/tasks/");

        /// <summary>
        /// /opportunities/tasks/{task_id}/
        /// </summary>
        /// <param name="task_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<model.Task>> Task(int task_id)
            => await Execute<model.Task>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/opportunities/tasks/{task_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "task_id", task_id.ToString() }
                });

        /// <summary>
        /// /characters/{character_id}/opportunities/
        /// </summary>
        /// <param name="character_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<model.CompletedTask>>> CompletedTasks()
            => await Execute<List<model.CompletedTask>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/opportunities/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);
    }
}