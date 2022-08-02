using ESI.NET.Enumerations;
using ESI.NET.Models.Calendar;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class CalendarLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public CalendarLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /characters/{character_id}/calendar/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<CalendarItem>>> Events()
            => await Execute<List<CalendarItem>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/calendar/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/calendar/{event_id}/
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Event>> Event(int event_id)
            => await Execute<Event>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/calendar/{event_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "event_id", event_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/calendar/{event_id}/
        /// </summary>
        /// <param name="event_id"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Event>> Respond(int event_id, EventResponse eventResponse)
            => await Execute<Event>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/characters/{character_id}/calendar/{event_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "event_id", event_id.ToString() }
                },
                body: new
                {
                    response = eventResponse.ToEsiValue()
                },
                token: _data.Token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contract_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<List<Response>>> Responses(int event_id)
            => await Execute<List<Response>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/calendar/{event_id}/attendees/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "event_id", event_id.ToString() }
                },
                token: _data.Token);
    }
}