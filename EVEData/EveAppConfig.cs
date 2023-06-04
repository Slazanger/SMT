//-----------------------------------------------------------------------
// EVE App Config
//-----------------------------------------------------------------------

namespace SMT.EVEData
{
    public class EveAppConfig
    {
        /// <summary>
        /// Client ID from the EVE Developer setup
        /// </summary>
        public const string ClientID = "4215e8d73ab54a959a412cac0a8f3711";

        /// <summary>
        /// Callback URL for eve
        /// </summary>
        public const string CallbackURL = @"http://localhost:8762/callback/";

        /// <summary>
        /// SMT Version
        /// </summary>
        public const string SMT_VERSION = "SMT_120";

        /// <summary>
        /// SMT Version Tagline
        /// </summary>
        public const string SMT_TITLE = "Hydrated Chromium Oxide";

        /// <summary>
        /// Folder to store all of the data from
        /// </summary>
        public static readonly string StorageRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\";

        /// <summary>
        /// Folder to store all of the data from
        /// </summary>
        public static readonly string VersionStorage = StorageRoot + $"{SMT_VERSION}\\";



    }
}