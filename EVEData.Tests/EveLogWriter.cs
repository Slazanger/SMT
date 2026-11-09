using System.Text;

namespace EVEData.Tests
{
    /// <summary>
    /// Writes a log file the way the EVE client does: created once, held open with sharing and appended to. Chat logs
    /// are UTF-16 with a byte order mark and a U+FEFF in front of every line; game logs are UTF-8 without either.
    /// </summary>
    internal sealed class EveLogWriter : IDisposable
    {
        private const char LineMark = (char)0xFEFF;
        private const string Dashes = "---------------------------------------------------------------";

        private readonly StreamWriter writer;
        private readonly bool chat;

        /// <param name="listener">The character in the header; null to leave the new file empty.</param>
        public EveLogWriter(string path, bool chat, string listener)
        {
            this.chat = chat;
            FilePath = path;
            writer = new StreamWriter(
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete),
                chat ? new UnicodeEncoding(false, true) : new UTF8Encoding(false))
            {
                AutoFlush = true,
            };

            if(listener != null)
            {
                WriteHeader(listener);
            }
        }

        public string FilePath { get; }

        /// <summary>
        /// The header as written to a file: what a reader has to skip.
        /// </summary>
        public string Header(string listener)
        {
            if(chat)
            {
                return $"{LineMark}\r\n{LineMark}\r\n        {Dashes}\r\n{LineMark}\r\n" +
                       "          Channel ID:      -1\r\n" +
                       "          Channel Name:    Test\r\n" +
                       $"          Listener:        {listener}\r\n" +
                       "          Session started: 2026.11.09 12:00:00\r\n" +
                       $"        {Dashes}\r\n";
            }

            return $"{Dashes}\r\n  Gamelog\r\n  Listener: {listener}\r\n  Session Started: 2026.11.09 12:00:00\r\n{Dashes}\r\n";
        }

        public void WriteHeader(string listener)
        {
            writer.Write(Header(listener));
        }

        /// <summary>
        /// One complete line.
        /// </summary>
        public void Line(string text)
        {
            writer.Write(Start(text) + "\r\n");
        }

        /// <summary>
        /// The start of a line, without its line break.
        /// </summary>
        public void Begin(string text)
        {
            writer.Write(Start(text));
        }

        /// <summary>
        /// More of the line begun, as it is.
        /// </summary>
        public void Continue(string text)
        {
            writer.Write(text);
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        private string Start(string text)
        {
            return chat ? LineMark + text : text;
        }
    }
}
