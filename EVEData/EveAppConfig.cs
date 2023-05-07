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
        public const string ClientID = "Client ID here";

        /// <summary>
        /// Callback URL for eve
        /// </summary>
        public const string CallbackURL = @"http://localhost:8762/callback/";

        /// <summary>
        /// SMT Version
        /// </summary>
        public const string SMT_VERSION = "SMT_120";

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