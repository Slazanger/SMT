using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Dock.Model.Mvvm.Controls;
using SMT.EVEData;

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
            EveManager.Instance.GameLogAddedEvent += EM_GameLogAddedEvent;
        }

        private void EM_GameLogAddedEvent(List<GameLogData> gll)
        {
            Dispatcher.UIThread.Invoke((Action)(() =>
            {
                List<GameLogData> removeList = new List<GameLogData>();
                List<GameLogData> addList = new List<GameLogData>();

                // remove old

                if (GameLogData.Count > 50)
                {
                    foreach (GameLogData gl in GameLogData)
                    {
                        if (!gll.Contains(gl))
                        {
                            removeList.Add(gl);
                        }
                    }

                    foreach (GameLogData gl in removeList)
                    {
                        GameLogData.Remove(gl);
                    }
                }



                // add new
                foreach (GameLogData gl in gll)
                {
                    if (!GameLogData.Contains(gl))
                    {
                        GameLogData.Insert(0, gl);
                    }
                }
            }), DispatcherPriority.Normal);
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