using Dock.Model.Mvvm.Controls;
using SMT.EVEData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SMTx.ViewModels.Tools
{
    public class GameLogFeedViewModel : Tool
    {
        private double m_textSize;

        public double TextSize
        {
            get
            {
                if (m_textSize <= 0)
                    return 12;
                else
                    return m_textSize;
            }
            set
            {
                if (value > 0)
                {
                    SetProperty(ref m_textSize, value);
                }
            }
        }

        public ObservableCollection<GameLogData> GameLogData { get; init; }

        public GameLogFeedViewModel()
        {
            GameLogData = new ObservableCollection<GameLogData>();
        }

        public GameLogFeedViewModel(IEnumerable<GameLogData> gameLogData)
        {
            GameLogData = new ObservableCollection<GameLogData>(gameLogData);
        }

        public void ClearGameLog()
        {
            GameLogData.Clear();
        }

        public void AddToGameLog(GameLogData logEntry)
        {
            GameLogData.Add(logEntry);
        }

        public void AddToGameLog(IEnumerable<GameLogData> logEntries)
        {
            if (logEntries != null)
            {
                foreach (var logEntry in logEntries)
                {
                    GameLogData.Add(logEntry);
                }
            }
        }
    }
}