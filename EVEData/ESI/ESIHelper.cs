using System.Diagnostics;
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
            return true;
        }
    }
}
