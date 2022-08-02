using ESI.NET.Models.PlanetaryInteraction;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class PlanetaryInteractionLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id, corporation_id;

        public PlanetaryInteractionLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
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
        /// /characters/{character_id}/planets/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Planet>>> Colonies()
            => await Execute<List<Planet>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/planets/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/planets/{planet_id}/
        /// </summary>
        /// <param name="planet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<ColonyLayout>> ColonyLayout(int planet_id)
            => await Execute<ColonyLayout>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/planets/{planet_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "planet_id", planet_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /corporations/{corporation_id}/customs_offices/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<CustomsOffice>>> CorporationCustomsOffices()
            => await Execute<List<CustomsOffice>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/corporations/{corporation_id}/customs_offices/",
                replacements: new Dictionary<string, string>()
                {
                    { "corporation_id", corporation_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /universe/schematics/{schematic_id}/
        /// </summary>
        /// <param name="schematic_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Schematic>> SchematicInformation(int schematic_id)
            => await Execute<Schematic>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/schematics/{schematic_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "schematic_id", schematic_id.ToString() }
                });
    }
}