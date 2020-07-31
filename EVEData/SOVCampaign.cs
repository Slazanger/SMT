using System;
using System.ComponentModel;

namespace SMT.EVEData
{
    public class SOVCampaign : INotifyPropertyChanged
    {
        private double m_AttackersScore;

        public double AttackersScore
        {
            get
            {
                return m_AttackersScore;
            }
            set
            {
                m_AttackersScore = value;
                OnPropertyChanged("AttackersScore");
            }
        }

        private double m_DefendersScore;

        public double DefendersScore
        {
            get
            {
                return m_DefendersScore;
            }
            set
            {
                m_DefendersScore = value;
                OnPropertyChanged("DefendersScore");
            }
        }

        public int CampaignID { get; set; }
        public long DefendingAllianceID { get; set; }
        public string DefendingAllianceName { get; set; }
        public string System { get; set; }
        public string Region { get; set; }
        public string Type { get; set; }
        public string State { get; set; }

        private bool m_isActive;

        public bool IsActive
        {
            get
            {
                return m_isActive;
            }
            set
            {
                m_isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        public DateTime StartTime { get; set; }

        private TimeSpan m_TimeToStart;

        public TimeSpan TimeToStart
        {
            get
            {
                return m_TimeToStart;
            }
            set
            {
                m_TimeToStart = value;
                OnPropertyChanged("TimeToStart");
            }
        }

        private bool m_Valid;

        public bool Valid
        {
            get
            {
                return m_Valid;
            }
            set
            {
                m_Valid = value;
                OnPropertyChanged("Valid");
            }
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
    }
}