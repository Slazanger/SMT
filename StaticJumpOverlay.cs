using System.ComponentModel;
using System.Windows.Media;

namespace SMT
{
    public class StaticJumpOverlay : INotifyPropertyChanged
    {
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string m_Name;

        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
            }
        }

        private string m_System;

        public string System
        {
            get
            {
                return m_System;
            }
            set
            {
                m_System = value;
                OnPropertyChanged("System");
            }
        }

        private Color m_OverlayColour;

        public Color OverlayColour
        {
            get
            {
                return m_OverlayColour;
            }
            set
            {
                m_OverlayColour = value;
                OnPropertyChanged("OverlayColour");
            }
        }

        private bool m_Active;

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
                OnPropertyChanged("Active");
            }
        }

        public EVEData.EveManager.JumpShip JumpShip { get; set; }
    }
}