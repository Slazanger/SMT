using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace ESI.NET
{
    public class EsiResponse<T>
    {
        public EsiResponse(HttpResponseMessage response, string path)
        {
            try
            {
                StatusCode = response.StatusCode;
                Endpoint = path.Split('|')[1];

                if (response.Headers.Contains("X-ESI-Request-ID"))
                    RequestId = Guid.Parse(response.Headers.GetValues("X-ESI-Request-ID").First());

                if (response.Headers.Contains("X-Pages"))
                    Pages = int.Parse(response.Headers.GetValues("X-Pages").First());

                if (response.Headers.Contains("ETag"))
                    ETag = response.Headers.GetValues("ETag").First().Replace("\"", string.Empty);

                if (response.Content.Headers.Contains("Expires"))
                    Expires = DateTime.Parse(response.Content.Headers.GetValues("Expires").First());

                if (response.Content.Headers.Contains("Last-Modified"))
                    LastModified = DateTime.Parse(response.Content.Headers.GetValues("Last-Modified").First());

                if (response.Headers.Contains("X-Esi-Error-Limit-Remain"))
                    ErrorLimitRemain = int.Parse(response.Headers.GetValues("X-Esi-Error-Limit-Remain").First());

                if (response.Headers.Contains("X-Esi-Error-Limit-Reset"))
                    ErrorLimitReset = int.Parse(response.Headers.GetValues("X-Esi-Error-Limit-Reset").First());

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.Created)
                    {
                        if ((result.StartsWith("{") && result.EndsWith("}")) || result.StartsWith("[") && result.EndsWith("]"))
                            Data = JsonConvert.DeserializeObject<T>(result);
                        else
                            Message = result;
                    }
                    else if (response.StatusCode == HttpStatusCode.NotModified)
                        Message = "Not Modified";
                    else
                        Message = JsonConvert.DeserializeAnonymousType(result, new { error = string.Empty }).error;
                }
                else if (response.StatusCode == HttpStatusCode.NoContent)
                    Message = _noContentMessage[path];

            }
            catch (Exception ex)
            {
                Message = response.Content.ReadAsStringAsync().Result;
                Exception = ex;
            }
            
        }

        public Guid RequestId { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Endpoint { get; set; }
        public string Version { get; set; } = "latest";
        public DateTime? Expires { get; set; }
        public DateTime? LastModified { get; set; }
        public string ETag { get; set; }
        public int? ErrorLimitRemain { get; set; }
        public int? ErrorLimitReset { get; set; }
        public int? Pages { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public Exception Exception { get; set; }

        private readonly ImmutableDictionary<string, string> _noContentMessage = new Dictionary<string, string>()
        {
            //Calendar
            {"Put|/characters/{character_id}/calendar/{event_id}/", "Event updated"},

            //Contacts
            {"Put|/characters/{character_id}/contacts/", "Contacts updated"},
            {"Delete|/characters/{character_id}/contacts/", "Contacts deleted"},

            //Corporations
            {"Put|/corporations/{corporation_id}/structures/{structure_id}/", "Structure vulnerability window updated"},

            //Fittings
            {"Delete|/characters/{character_id}/fittings/{fitting_id}/", "Fitting deleted"},

            //Fleets
            {"Put|/fleets/{fleet_id}/", "Fleet updated"},
            {"Post|/fleets/{fleet_id}/members/", "Fleet invitation sent"},
            {"Delete|/fleets/{fleet_id}/members/{member_id}/", "Fleet member kicked"},
            {"Put|/fleets/{fleet_id}/members/{member_id}/", "Fleet invitation sent"},
            {"Delete|/fleets/{fleet_id}/wings/{wing_id}/", "Wing deleted"},
            {"Put|/fleets/{fleet_id}/wings/{wing_id}/", "Wing renamed"},
            {"Delete|/fleets/{fleet_id}/squads/{squad_id}/", "Squad deleted"},
            {"Put|/fleets/{fleet_id}/squads/{squad_id}/", "Squad renamed"},

            //Mail
            {"Post|/characters/{character_id}/mail/", "Mail created"},
            {"Post|/characters/{character_id}/mail/labels/", "Label created"},
            {"Delete|/characters/{character_id}/mail/labels/{label_id}/", "Label deleted"},
            {"Put|/characters/{character_id}/mail/{mail_id}/", "Mail updated"},
            {"Delete|/characters/{character_id}/mail/{mail_id}/", "Mail deleted"},

            //User Interface
            {"Post|/ui/openwindow/marketdetails/", "Open window request received"},
            {"Post|/ui/openwindow/contract/", "Open window request received"},
            {"Post|/ui/openwindow/information/", "Open window request received"},
            {"Post|/ui/autopilot/waypoint/", "Open window request received"},
            {"Post|/ui/openwindow/newmail/", "Open window request received"}
        }.ToImmutableDictionary();
    }
}