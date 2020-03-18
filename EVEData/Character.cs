//-----------------------------------------------------------------------
// EVE AnomManager
//-----------------------------------------------------------------------

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
            CorporationID = -1;
            AllianceID = -1;
            ID = -1;
        }

        /// <summary>
        /// Gets or sets the ID of the Alliance this character is in
        /// </summary>
        public long AllianceID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Corp this character is in
        /// </summary>
        public long CorporationID { get; set; }

        /// <summary>
        /// Gets or sets the ID used by ESI for this character
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Character
        /// </summary>
        public string Name { get; set; }
    }
}