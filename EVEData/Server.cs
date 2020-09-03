using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace SMT.EVEData
{
    public class Server : INotifyPropertyChanged
    {
        private int m_numPlayers;
        private string m_serverVersion;

        private DateTime m_serverTime;

        public Server()
        {
            // EVE Time is basically UTC time
            ServerTime = DateTime.UtcNow;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10000);
            timer.Tick += new EventHandler(UpdateServerTime);
            timer.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        public int NumPlayers
        {
            get
            {
                return m_numPlayers;
            }

            set
            {
                m_numPlayers = value;
                OnPropertyChanged("NumPlayers");
            }
        }

        public DateTime ServerTime
        {
            get
            {
                return m_serverTime;
            }
            set
            {
                m_serverTime = value;
                OnPropertyChanged("ServerTime");
            }
        }

        public string ServerVersion
        {
            get
            {
                return m_serverVersion;
            }
            set
            {
                m_serverVersion = value;
                OnPropertyChanged("ServerVersion");
            }
        }

        public void UpdateServerTime(object sender, EventArgs e)
        {
            ServerTime = DateTime.UtcNow;
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