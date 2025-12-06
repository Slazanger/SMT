using System.ComponentModel;

namespace SMT.EVEData
{
    /// <summary>
    /// Represents an Infrastructure Hub Upgrade
    /// </summary>
    public class InfrastructureUpgrade : INotifyPropertyChanged
    {
        private int m_SlotNumber;
        private string m_UpgradeName;
        private int m_Level;
        private bool m_IsOnline;

        /// <summary>
        /// Slot number (1-based)
        /// </summary>
        public int SlotNumber
        {
            get { return m_SlotNumber; }
            set
            {
                m_SlotNumber = value;
                OnPropertyChanged("SlotNumber");
            }
        }

        /// <summary>
        /// Name of the upgrade (e.g., "Major Threat Detection Array", "Cynosural Navigation")
        /// </summary>
        public string UpgradeName
        {
            get { return m_UpgradeName; }
            set
            {
                m_UpgradeName = value;
                OnPropertyChanged("UpgradeName");
            }
        }

        /// <summary>
        /// Level of the upgrade (1-3, or 0 for upgrades without levels)
        /// </summary>
        public int Level
        {
            get { return m_Level; }
            set
            {
                m_Level = value;
                OnPropertyChanged("Level");
            }
        }

        /// <summary>
        /// Whether the upgrade is online
        /// </summary>
        public bool IsOnline
        {
            get { return m_IsOnline; }
            set
            {
                m_IsOnline = value;
                OnPropertyChanged("IsOnline");
            }
        }

        /// <summary>
        /// Full display name including level if applicable
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (Level > 0)
                {
                    return $"{UpgradeName} {Level}";
                }
                return UpgradeName;
            }
        }

        /// <summary>
        /// Status text
        /// </summary>
        public string Status
        {
            get { return IsOnline ? "Online" : "Offline"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public override string ToString()
        {
            return $"{SlotNumber}\t{DisplayName}\t{Status}";
        }
    }
}
