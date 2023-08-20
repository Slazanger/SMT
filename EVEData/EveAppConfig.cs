﻿//-----------------------------------------------------------------------
// EVE App Config
//-----------------------------------------------------------------------

using System.IO;

namespace SMT.EVEData
{
    public class EveAppConfig
    {
        #region Fields

        /// <summary>
        /// Callback URL for eve
        /// </summary>
        public const string CallbackURL = @"http://localhost:8762/callback/";

        /// <summary>
        /// Client ID from the EVE Developer setup
        /// </summary>
        public const string ClientID = "ID Goes here.. ";

        /// <summary>
        /// SMT Version Tagline
        /// </summary>
        public const string SMT_TITLE = "Hydrated Chromium Oxide";

        /// <summary>
        /// SMT Version
        /// </summary>
        public const string SMT_VERSION = "SMT_Avalonia";

        /// <summary>
        /// Folder to store all of the data from
        /// </summary>
        public static readonly string StorageRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMTx");

        /// <summary>
        /// Folder to store all of the data from
        /// </summary>
        public static readonly string VersionStorage = Path.Combine(StorageRoot, $"{SMT_VERSION}");


        #endregion Fields
    }
}