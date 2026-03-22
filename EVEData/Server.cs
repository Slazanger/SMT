using System.ComponentModel;
using ElapsedEventHandler = System.Timers.ElapsedEventHandler;
using Timer = System.Timers.Timer;

namespace EVEData;

public class Server : INotifyPropertyChanged
{
    private int m_numPlayers;
    private string m_serverVersion;

    private DateTime m_serverTime;

    public Server()
    {
        // EVE Time is basically UTC time
        ServerTime = DateTime.UtcNow;

        var timer = new Timer(10000);
        timer.Elapsed += new ElapsedEventHandler(UpdateServerTime);
        ;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; set; }

    public int NumPlayers
    {
        get => m_numPlayers;

        set
        {
            m_numPlayers = value;
            OnPropertyChanged("NumPlayers");
        }
    }

    public DateTime ServerTime
    {
        get => m_serverTime;
        set
        {
            m_serverTime = value;
            OnPropertyChanged("ServerTime");
        }
    }

    public string ServerVersion
    {
        get => m_serverVersion;
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
        var handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(name));
    }
}