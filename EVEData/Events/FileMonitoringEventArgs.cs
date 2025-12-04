//-----------------------------------------------------------------------
// File Monitoring Event Arguments
//-----------------------------------------------------------------------

#nullable enable

namespace SMT.EVEData.Events
{
    /// <summary>
    /// Event args for intel file (chat log) changes
    /// </summary>
    public class IntelFileChangedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string ChannelName { get; }
        public bool IsLocalChat { get; }
        public List<string> NewLines { get; }

        public IntelFileChangedEventArgs(string filePath, string fileName, string channelName, bool isLocalChat, List<string> newLines)
        {
            FilePath = filePath;
            FileName = fileName;
            ChannelName = channelName;
            IsLocalChat = isLocalChat;
            NewLines = newLines;
        }
    }

    /// <summary>
    /// Event args for game log file changes
    /// </summary>
    public class GameLogFileChangedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string CharacterName { get; }
        public List<string> NewLines { get; }

        public GameLogFileChangedEventArgs(string filePath, string fileName, string characterName, List<string> newLines)
        {
            FilePath = filePath;
            FileName = fileName;
            CharacterName = characterName;
            NewLines = newLines;
        }
    }
}
