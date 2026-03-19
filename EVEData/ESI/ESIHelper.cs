using System.Diagnostics;
using System.Reflection;
using EVEStandard.Models.API;

namespace SMT.EVEData
{
    public class ESIHelpers
    {
        public static bool ValidateESICall<T>(ESIModelDTO<T> esiR)
        {
            if (esiR == null || esiR.Model == null)
            {
                Debug.WriteLine("ESI data Null");
                return false;
            }
            TryUpdateEsiRateLimitFromResponse(esiR);
            return true;
        }

        /// <summary>
        /// If the response type exposes ESI rate-limit headers (e.g. X-Esi-Error-Limit-Remain/Reset or X-Ratelimit-*),
        /// updates EveManager's token bucket for the appropriate group.
        /// </summary>
        public static void TryUpdateEsiRateLimitFromResponse<T>(ESIModelDTO<T> response)
        {
            if (response == null) return;
            try
            {
                var type = response.GetType();
                int? remain = GetIntProperty(type, response, "ErrorLimitRemain")
                    ?? GetIntProperty(type, response, "RequestLimitRemain")
                    ?? GetIntProperty(type, response, "RateLimitRemaining");
                int? resetSeconds = GetIntProperty(type, response, "ErrorLimitReset")
                    ?? GetIntProperty(type, response, "RequestLimitReset")
                    ?? GetIntProperty(type, response, "RetryAfter");

                if (remain.HasValue && resetSeconds.HasValue)
                {
                    string group = GetStringProperty(type, response, "RateLimitGroup")
                        ?? GetStringProperty(type, response, "ErrorLimitGroup")
                        ?? "ErrorLimit";
                    EveManager.Instance.UpdateEsiRateLimit(group, remain.Value, resetSeconds.Value);
                }
            }
            catch
            {
                // Ignore: response type may not expose these properties
            }
        }

        private static int? GetIntProperty(Type type, object obj, string propertyName)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;
            var pt = prop.PropertyType;
            if (pt != typeof(int) && pt != typeof(int?) && !(pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>) && pt.GetGenericArguments()[0] == typeof(int)))
                return null;
            var value = prop.GetValue(obj);
            if (value == null) return null;
            if (value is int i) return i;
            return null;
        }

        private static string GetStringProperty(Type type, object obj, string propertyName)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || prop.PropertyType != typeof(string)) return null;
            return prop.GetValue(obj) as string;
        }
    }
}
