using System.Net;
using ESI.NET;

namespace SMT.EVEData
{
    public class ESIHelpers
    {
        public static bool ValidateESICall<T>(EsiResponse<T> esiR)
        {
            if (esiR.StatusCode == HttpStatusCode.OK || esiR.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }

            Console.WriteLine("ESI Error : " + esiR.Message + " Error Limit Remaining : " + esiR.ErrorLimitRemain);
            return false;
        }
    }
}