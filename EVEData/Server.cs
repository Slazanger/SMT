using System.ComponentModel;
using Timer = System.Timers.Timer;
using ElapsedEventHandler = System.Timers.ElapsedEventHandler;

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


            Timer timer = new Timer(10000);
            timer.Elapsed += new ElapsedEventHandler(UpdateServerTime); ;
            timer.AutoReset = true;
            timer.Enabled = true;

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