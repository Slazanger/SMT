//-----------------------------------------------------------------------
// EVE AnomManager
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// EVE Character
    /// </summary>
    public class Character
    {


        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class
        /// </summary>
        public Character()
        {
            CorporationID = 0;
            AllianceID = 0;
        }

        /// <summary>
        /// Gets or sets the Name of the Character
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID used by ESI for this character
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Corp this character is in
        /// </summary>
        public long CorporationID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Alliance this character is in
        /// </summary>
        public long AllianceID { get; set; }
    }
}