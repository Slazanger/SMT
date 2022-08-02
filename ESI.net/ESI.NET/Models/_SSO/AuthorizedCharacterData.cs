using System;

namespace ESI.NET.Models.SSO
{
    public class AuthorizedCharacterData
    {
        public int CharacterID { get; set; }
        public string CharacterName { get; set; }
        public DateTime ExpiresOn { get; set; }
        public string Scopes { get; set; }
        public string TokenType { get; set; }
        public string CharacterOwnerHash { get; set; }

        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public int AllianceID { get; set; }
        public int CorporationID { get; set; }
        public int FactionID { get; set; }
    }
}
