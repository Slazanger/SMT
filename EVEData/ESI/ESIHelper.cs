using System.Diagnostics;
using System.Net;
using ESI.NET;

namespace SMT.EVEData
{
    public class ESIHelpers
    {
        public static bool ValidateESICall<T>(EsiResponse<T> esiR)
        {
            if(esiR.Data == null)
            {
                Debug.WriteLine("ESI data Null");
                return false;
            }

            if (esiR.StatusCode == HttpStatusCode.OK || esiR.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }

            Debug.WriteLine("ESI Error : " + esiR.Message + " Error Limit Remaining : " + esiR.ErrorLimitRemain);
            return false;
        }
    }
}