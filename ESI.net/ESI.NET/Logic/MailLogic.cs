using ESI.NET.Models.Mail;
using ESI.NET.Models.SSO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static ESI.NET.EsiRequest;

namespace ESI.NET.Logic
{
    public class MailLogic
    {
        private readonly HttpClient _client;
        private readonly EsiConfig _config;
        private readonly AuthorizedCharacterData _data;
        private readonly int character_id;

        public MailLogic(HttpClient client, EsiConfig config, AuthorizedCharacterData data = null)
        {
            _client = client;
            _config = config;
            _data = data;

            if (data != null)
                character_id = data.CharacterID;
        }

        /// <summary>
        /// /characters/{character_id}/mail/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<Header>>> Headers(long[] labels = null, int last_mail_id = 0)
        {
            var parameters = new List<string>();

            if (labels != null)
                parameters.Add($"labels={string.Join(",", labels)}");

            if (last_mail_id > 0)
                parameters.Add($"last_mail_id={last_mail_id}");

            var response = await Execute<List<Header>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/mail/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                parameters: parameters.ToArray(),
                token: _data.Token);

            return response;
        }

        /// <summary>
        /// /characters/{character_id}/mail/
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="approved_cost"></param>
        /// <returns></returns>
        public async Task<EsiResponse<int>> New(object[] recipients, string subject, string body, int approved_cost = 0)
            => await Execute<int>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/mail/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                body: new
                {
                    recipients,
                    subject,
                    body,
                    approved_cost
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/labels/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<LabelCounts>> Labels()
            => await Execute<LabelCounts>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/mail/labels/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/labels/
        /// </summary>
        /// <param name="name"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public async Task<EsiResponse<long>> NewLabel(string name, string color)
            => await Execute<long>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Post, "/characters/{character_id}/mail/labels/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                body: new
                {
                    name,
                    color
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/labels/{label_id}/
        /// </summary>
        /// <param name="label_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<string>> DeleteLabel(long label_id)
            => await Execute<string>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/characters/{character_id}/mail/labels/{label_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "label_id", label_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/lists/
        /// </summary>
        /// <returns></returns>
        public async Task<EsiResponse<List<MailingList>>> MailingLists()
            => await Execute<List<MailingList>>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/mail/lists/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/{mail_id}/
        /// </summary>
        /// <param name="mail_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Message>> Retrieve(int mail_id)
            => await Execute<Message>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Get, "/characters/{character_id}/mail/{mail_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "mail_id", mail_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// /characters/{character_id}/mail/{mail_id}/
        /// </summary>
        /// <param name="mail_id"></param>
        /// <param name="is_read"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Message>> Update(int mail_id, bool? is_read = null, int[] labels = null)
            => await Execute<Message>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Put, "/characters/{character_id}/mail/{mail_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "mail_id", mail_id.ToString() }
                },
                body: BuildUpdateObject(is_read, labels),
                token: _data.Token);
        
        /// <summary>
        /// /characters/{character_id}/mail/{mail_id}/
        /// </summary>
        /// <param name="mail_id"></param>
        /// <returns></returns>
        public async Task<EsiResponse<Message>> Delete(int mail_id)
            => await Execute<Message>(_client, _config, RequestSecurity.Authenticated, RequestMethod.Delete, "/characters/{character_id}/mail/{mail_id}/",
                replacements: new Dictionary<string, string>()
                {
                    { "character_id", character_id.ToString() },
                    { "mail_id", mail_id.ToString() }
                },
                token: _data.Token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="is_read"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        private static dynamic BuildUpdateObject(bool? is_read, int[] labels = null)
        {
            dynamic body = null;

            if (is_read != null && labels == null)
                body = new { is_read };
            else if (is_read == null && labels != null)
                body = new { labels };
            else
                body = new { is_read, labels };
            return body;
        }
    }
}