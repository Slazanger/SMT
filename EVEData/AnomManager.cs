//-----------------------------------------------------------------------
// EVE AnomManager
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// Anom Manager
    /// </summary>
    public class AnomManager : INotifyPropertyChanged
    {
        /// <summary>
        /// The current active System
        /// </summary>
        private AnomData activeSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnomManager" /> class
        /// </summary>
        public AnomManager()
        {
            Systems = new SerializableDictionary<string, AnomData>();
            ActiveSystem = null;
        }

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current active system
        /// </summary>
        public AnomData ActiveSystem
        {
            get
            {
                return activeSystem;
            }

            set
            {
                activeSystem = value;
                OnPropertyChanged("ActiveSystem");
            }
        }

        /// <summary>
        /// Gets or sets the System to AnomData
        /// </summary>
        public SerializableDictionary<string, AnomData> Systems { get; set; }

        /// <summary>
        /// Gets the Anom data for the specified system, creates it if it doesn't exist
        /// </summary>
        /// <param name="sysName">Name of the System</param>
        /// <returns>Anom Data</returns>
        public AnomData GetSystemAnomData(string sysName)
        {
            AnomData ret;
            if (Systems.Keys.Contains(sysName))
            {
                ret = Systems[sysName];
            }
            else
            {
                ret = new AnomData();
                ret.SystemName = sysName;
                Systems.Add(sysName, ret);
            }

            return ret;
        }

        /// <summary>
        /// On Property Changed helper
        /// </summary>
        /// <param name="name">Name of Property that changed</param>
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