using ESI.NET.Models.SSO;
using ESI.NET.Models.Universe;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class UniverseLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;

        public UniverseLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;
        }

        /// <summary>
        /// /universe/bloodlines/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Bloodline>>> Bloodlines()
            => await Execute<List<Bloodline>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/bloodlines/");

        /// <summary>
        /// /universe/categories/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Categories()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/categories/");

        /// <summary>
        /// /universe/categories/{category_id}/
        /// </summary>
        /// <param name="category_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Category>> Category(int category_id)
            => await Execute<Category>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/categories/{category_id}/", replacements: new Dictionary<string, string>()
            {
                { "category_id", category_id.ToString() }
            });

        /// <summary>
        /// /universe/constellations/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Constellations()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/constellations/");

        /// <summary>
        /// /universe/constellations/{constellation_id}/
        /// </summary>
        /// <param name="constellation_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Constellation>> Constellation(int constellation_id)
            => await Execute<Constellation>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/constellations/{constellation_id}/", replacements: new Dictionary<string, string>()
            {
                { "constellation_id", constellation_id.ToString() }
            });

        /// <summary>
        /// /universe/factions/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Faction>>> Factions()
            => await Execute<List<Faction>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/factions/");

        /// <summary>
        /// /universe/graphics/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Graphics()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/graphics/");

        /// <summary>
        /// /universe/graphics/{graphic_id}/
        /// </summary>
        /// <param name="graphic_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Graphic>> Graphic(int graphic_id)
            => await Execute<Graphic>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/graphics/{graphic_id}/", replacements: new Dictionary<string, string>()
            {
                { "graphic_id", graphic_id.ToString() }
            });

        /// <summary>
        /// /universe/groups/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Groups()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/groups/");

        /// <summary>
        /// /universe/groups/{group_id}/
        /// </summary>
        /// <param name="group_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Group>> Group(int group_id)
            => await Execute<Group>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/groups/{group_id}/", replacements: new Dictionary<string, string>()
            {
                { "group_id", group_id.ToString() }
            });

        /// <summary>
        /// /universe/moons/{moon_id}/
        /// </summary>
        /// <param name="moon_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Moon>> Moon(int moon_id)
            => await Execute<Moon>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/moons/{moon_id}/", replacements: new Dictionary<string, string>()
            {
                { "moon_id", moon_id.ToString() }
            });

        /// <summary>
        /// /universe/names/
        /// </summary>
        /// <param name="any_ids">The ids to resolve; Supported IDs for resolving are: Characters, Corporations, Alliances, Stations, Solar Systems, Constellations, Regions, Types.</param>
        /// <returns></returns>
        public async Task<EsiResponse<List<ResolvedInfo>>> Names(List<long> any_ids)
            => await Execute<List<ResolvedInfo>>(_client, _config, RequestSecurity.Public, RequestMethod.Post, "/universe/names/", body: any_ids.ToArray());

        /// <summary>
        /// /universe/ids/
        /// </summary>
        /// <param name="names">Resolve a set of names to IDs in the following categories: agents, alliances, characters, constellations, corporations factions, inventory_types, regions, stations, and systems. Only exact matches will be returned. All names searched for are cached for 12 hours.</param>
        /// <returns></returns>
        public async Task<EsiResponse<IDLookup>> IDs(List<string> names)
            => await Execute<IDLookup>(_client, _config, RequestSecurity.Public, RequestMethod.Post, "/universe/ids/", body: names.ToArray());

        /// <summary>
        /// /universe/planets/{planet_id}/
        /// </summary>
        /// <param name="planet_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Planet>> Planet(int planet_id)
            => await Execute<Planet>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/planets/{planet_id}/", replacements: new Dictionary<string, string>()
            {
                { "planet_id", planet_id.ToString() }
            });

        /// <summary>
        /// /universe/races/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Race>>> Races()
            => await Execute<List<Race>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/races/");

        /// <summary>
        /// /universe/regions/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Regions()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/regions/");

        /// <summary>
        /// /universe/regions/{region_id}/
        /// </summary>
        /// <param name="region_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Region>> Region(int region_id)
            => await Execute<Region>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/regions/{region_id}/", replacements: new Dictionary<string, string>()
            {
                { "region_id", region_id.ToString() }
            });

        /// <summary>
        /// /universe/stations/{station_id}/
        /// </summary>
        /// <param name="station_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Station>> Station(int station_id)
            => await Execute<Station>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/stations/{station_id}/", replacements: new Dictionary<string, string>()
            {
                { "station_id", station_id.ToString() }
            });

        /// <summary>
        /// /universe/structures/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<long[]>> Structures()
            => await Execute<long[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/structures/");

        /// <summary>
        /// /universe/structures/{structure_id}/
        /// </summary>
        /// <param name="structure_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Structure>> Structure(long structure_id)
            => await Execute<Structure>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/universe/structures/{structure_id}/", replacements: new Dictionary<string, string>()
            {
                { "structure_id", structure_id.ToString() }
            }, token: _data.Token);

        /// <summary>
        /// /universe/systems/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Systems()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/systems/");

        /// <summary>
        /// /universe/systems/{system_id}/
        /// </summary>
        /// <param name="system_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<SolarSystem>> System(int system_id)
            => await Execute<SolarSystem>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/systems/{system_id}/", replacements: new Dictionary<string, string>()
            {
                { "system_id", system_id.ToString() }
            });

        /// <summary>
        /// /universe/types/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<int[]>> Types()
            => await Execute<int[]>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/types/");

        /// <summary>
        /// /universe/types/{type_id}/
        /// </summary>
        /// <param name="type_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Type>> Type(int type_id)
            => await Execute<Type>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/types/{type_id}/", replacements: new Dictionary<string, string>()
            {
                { "type_id", type_id.ToString() }
            });

        /// <summary>
        /// /universe/stargates/{stargate_id}/
        /// </summary>
        /// <param name="stargate_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Stargate>> Stargate(int stargate_id)
            => await Execute<Stargate>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/stargates/{stargate_id}/", replacements: new Dictionary<string, string>()
            {
                { "stargate_id", stargate_id.ToString() }
            });

        /// <summary>
        /// /universe/system_jumps/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Jumps>>> Jumps()
            => await Execute<List<Jumps>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/system_jumps/");

        /// <summary>
        /// /universe/system_kills/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Kills>>> Kills()
            => await Execute<List<Kills>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/system_kills/");

        /// <summary>
        /// /universe/stars/{star_id}/
        /// </summary>
        /// <param name="star_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Star>> Star(int star_id)
            => await Execute<Star>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/stars/{star_id}/", replacements: new Dictionary<string, string>()
            {
                { "star_id", star_id.ToString() }
            });

        /// <summary>
        /// /universe/ancestries/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Ancestry>>> Ancestries()
            => await Execute<List<Ancestry>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/ancestries/");

        /// <summary>
        /// /universe/asteroid_belts/{asteroid_belt_id}/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Ancestry>>> AsteroidBelt(int asteroid_belt_id)
            => await Execute<List<Ancestry>>(_client, _config, RequestSecurity.Public, RequestMethod.Get, "/universe/asteroid_belts/{asteroid_belt_id}/", replacements: new Dictionary<string, string>()
            {
                { "asteroid_belt_id", asteroid_belt_id.ToString() }
            });
    }
}
