using System.ComponentModel;
using System.Windows.Media;

namespace SMT
{
    public class StaticJumpOverlay : INotifyPropertyChanged
    {
        private bool m_Active;

        private string m_Name;

        private Color m_OverlayColour;

        private string m_System;

        public event PropertyChangedEventHandler PropertyChanged;

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