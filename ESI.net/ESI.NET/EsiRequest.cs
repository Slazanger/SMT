using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ESI.NET
{
    internal static class EsiRequest
    {
        internal static string ETag;

        public static async Task<EsiResponse<T>> Execute<T>(HttpClient client, EsiConfig config, RequestSecurity security, RequestMethod method, string endpoint, Dictionary<string, string> replacements = null, string[] parameters = null, object body = null, string token = null)
        {
            client.DefaultRequestHeaders.Clear();

            var path = $"{method.ToString()}|{endpoint}";

            if (replacements != null)
                foreach (var property in replacements)
                    endpoint = endpoint.Replace($"{{{property.Key}}}", property.Value);

            var url = $"{config.EsiUrl}latest{endpoint}?datasource={config.DataSource.ToEsiValue()}";

            //Attach token to request header if this endpoint requires an authorized character
            if (security == RequestSecurity.Authenticated)
            {
                if (token == null)
                    throw new ArgumentException("The request endpoint requires SSO authentication and a Token has not been provided.");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (ETag != null)
            {
                client.DefaultRequestHeaders.Add("If-None-Match", $"\"{ETag}\"");
                ETag = null;
            }

            //Attach query string parameters
            if (parameters != null)
                url += $"&{string.Join("&", parameters)}";

            //Serialize post body data
            HttpContent postBody = null;
            if (body != null)
                postBody = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            //Get response from client based on request type
            //This is also where body variables will be created and attached as necessary
            HttpResponseMessage response = null;
            switch (method)
            {
                case RequestMethod.Delete:
                    response = await client.DeleteAsync(url).ConfigureAwait(false);
                    break;

                case RequestMethod.Get:
                    response = await client.GetAsync(url).ConfigureAwait(false);
                    break;

                case RequestMethod.Post:
                    response = await client.PostAsync(url, postBody).ConfigureAwait(false);
                    break;

                case RequestMethod.Put:
                    response = await client.PutAsync(url, postBody).ConfigureAwait(false);
                    break;
            }
            
            //Output final object
            var obj = new EsiResponse<T>(response, path);
            return obj;
        }

        public enum RequestSecurity
        {
            Public,
            Authenticated
        }

        public enum RequestMethod
        {
            Delete,
            Get,
            Post,
            Put
        }
    }
}