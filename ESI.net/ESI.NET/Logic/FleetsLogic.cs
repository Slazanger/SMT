using ESI.NET.Enumerations;
using ESI.NET.Models.Fleets;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class FleetsLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public FleetsLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /fleets/{fleet_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Settings>> Settings(long fleet_id)
            => await Execute<Settings>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/fleets/{fleet_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="motd"></param>
        /// <param name="is_free_move"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> UpdateSettings(long fleet_id, string motd = null, bool? is_free_move = null)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/fleets/{fleet_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                body: BuildUpdateSettingsObject(motd, is_free_move),
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/fleet/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<FleetInfo>> FleetInfo()
            => await Execute<FleetInfo>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/fleet/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/members/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Member>>> Members(long fleet_id)
            => await Execute<List<Member>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/fleets/{fleet_id}/members/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/members/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="character_id"></param>
        /// <param name="role"></param>
        /// <param name="wing_id"></param>
        /// <param name="squad_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> InviteCharacter(long fleet_id, int character_id, FleetRole role, long wing_id = 0, long squad_id = 0)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/fleets/{fleet_id}/members/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                body: BuildFleetInviteObject(character_id, role, wing_id, squad_id),
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/members/{member_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="member_id"></param>
        /// <param name="role"></param>
        /// <param name="wing_id"></param>
        /// <param name="squad_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> MoveCharacter(long fleet_id, int member_id, FleetRole role, long wing_id = 0, long squad_id = 0)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/fleets/{fleet_id}/members/{member_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() },
                    { "member_id", member_id.ToString() }
                },
                body: BuildFleetInviteObject(character_id, role, wing_id, squad_id),
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/members/{member_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="member_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> KickCharacter(long fleet_id, int member_id)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/fleets/{fleet_id}/members/{member_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() },
                    { "member_id", member_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/wings/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Wing>>> Wings(long fleet_id)
            => await Execute<List<Wing>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/fleets/{fleet_id}/wings/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/wings/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<NewWing>> CreateWing(long fleet_id)
            => await Execute<NewWing>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/fleets/{fleet_id}/wings/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/wings/{wing_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="wing_id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> RenameWing(long fleet_id, long wing_id, string name)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/fleets/{fleet_id}/wings/{wing_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() },
                    { "wing_id", wing_id.ToString() }
                },
                body: new
                {
                    name
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/wings/{wing_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="wing_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> DeleteWing(long fleet_id, long wing_id)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/fleets/{fleet_id}/wings/{wing_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() },
                    { "wing_id", wing_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/wings/{wing_id}/squads/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="wing_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<NewSquad>> CreateSquad(long fleet_id, long wing_id)
            => await Execute<NewSquad>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/fleets/{fleet_id}/wings/{wing_id}/squads/",
                replacements: new Dictionary<string, string>()
                {
                    { "fleet_id", fleet_id.ToString() },
                    { "wing_id", wing_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/squads/{squad_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="squad_id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> RenameSquad(long fleet_id, long squad_id, string name)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/fleets/{fleet_id}/squads/{squad_id}/", replacements: new Dictionary<string, string>()
            {
                { "fleet_id", fleet_id.ToString() },
                { "squad_id", squad_id.ToString() }
            }, body: new
            {
                name
            }, token: _data.Token);

        /// <summary>
        /// /fleets/{fleet_id}/squads/{squad_id}/
        /// </summary>
        /// <param name="fleet_id"></param>
        /// <param name="squad_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> DeleteSquad(long fleet_id, long squad_id)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/fleets/{fleet_id}/squads/{squad_id}/", replacements: new Dictionary<string, string>()
            {
                { "fleet_id", fleet_id.ToString() },
                { "squad_id", squad_id.ToString() }
            }, token: _data.Token);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motd"></param>
        /// <param name="is_free_move"></param>
        /// <returns></returns>
        private static dynamic BuildUpdateSettingsObject(string motd, bool? is_free_move)
        {
            dynamic body = null;

            if (motd != null)
                body = new { motd };
            if (is_free_move != null)
                body = new { is_free_move };
            if (motd != null && is_free_move != null)
                body = new { motd, is_free_move };

            return body;
        }

        /// <summary>
        /// Dynamically builds the required structure for a fleet invite or move
        /// </summary>
        /// <param name="character_id"></param>
        /// <param name="role"></param>
        /// <param name="wing_id"></param>
        /// <param name="squad_id"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private static dynamic BuildFleetInviteObject(int character_id, FleetRole role, long wing_id, long squad_id)
        {
            dynamic body = null;

            if (role == FleetRole.FleetCommander)
                body = new { character_id, role = role.ToEsiValue() };

            else if (role == FleetRole.WingCommander)
                body = new { character_id, role = role.ToEsiValue(), wing_id };

            else if (role == FleetRole.SquadCommander || role == FleetRole.SquadMember)
                body = new { character_id, role = role.ToEsiValue(), wing_id, squad_id };

            return body;
        }
    }
}