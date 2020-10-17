//-----------------------------------------------------------------------
// Jump Bridge
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    /// <summary>
    /// A Player owned link between systems
    /// </summary>
    public class JumpBridge : INotifyPropertyChanged
    {

        public static readonly string SaveVersion = "01";


        /// <summary>
        /// Initializes a new instance of the <see cref="JumpBridge" /> class.
        /// </summary>
        public JumpBridge()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JumpBridge" /> class.
        /// </summary>
        /// <param name="f">From</param>
        /// <param name="t">To</param>
        public JumpBridge(string f, string t)
        {
            From = f;
            To = t;
        }

        private bool m_Disabled;

        [XmlIgnoreAttribute]
        public bool Disabled
        {
            get
            {
                return m_Disabled;
            }
            set
            {
                m_Disabled = value;

                OnPropertyChanged("Disabled");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the starting System
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// InGame Structure ID
        /// </summary>
        public long FromID { get; set; }

        /// <summary>
        /// Gets or sets the ending System
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// InGame Structure ID
        /// </summary>
        public long ToID { get; set; }

        public override string ToString()
        {
            return $"{From} <==> {To}";
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}