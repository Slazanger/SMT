using System.Text;
using SMT.EVEData;
using Xunit;

namespace EVEData.Tests
{
    /// <summary>
    /// LogFolderMonitor and LogFileTail on a temporary folder, with log files written the way the EVE client writes
    /// them. The monitor takes the time from its caller, so the tests move the clock themselves and never wait.
    /// </summary>
    public sealed class LogFolderMonitorTests : IDisposable
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        private readonly string folder = Path.Combine(Path.GetTempPath(), "SMT.Tests", Guid.NewGuid().ToString("N"));
        private readonly List<EveLogWriter> writers = new List<EveLogWriter>();
        private readonly List<string> errors = new List<string>();
        private readonly LogFolderMonitor monitor;
        private DateTime now = Start;

        public LogFolderMonitorTests()
        {
            Directory.CreateDirectory(folder);
            monitor = new LogFolderMonitor(folder, name => true);
        }

        public void Dispose()
        {
            monitor.Dispose();
            foreach(EveLogWriter writer in writers)
            {
                writer.Dispose();
            }

            Directory.Delete(folder, true);
        }

        // otherwise every start of SMT would report the day's old intel again
        [Fact]
        public void WhatAFileHeldBeforeItWasFollowedIsSkipped()
        {
            EveLogWriter intel = ChatLog("Intel");
            intel.Line("[ 2026.11.09 12:00:01 ] Pilot > written before the monitor started");

            Assert.Empty(Poll());

            intel.Line("[ 2026.11.09 12:00:02 ] Pilot > one");
            intel.Line("[ 2026.11.09 12:00:03 ] Pilot > zwei, über Local");

            Assert.Equal(new[] { "[ 2026.11.09 12:00:02 ] Pilot > one", "[ 2026.11.09 12:00:03 ] Pilot > zwei, über Local" }, Poll());
            Assert.Empty(Poll());
        }

        // the client writes in pieces: half a line must not be matched for systems as if it were the whole report
        [Fact]
        public void ALineIsHeldBackUntilItsLineBreakIsWritten()
        {
            EveLogWriter intel = ChatLog("Intel");
            Poll();

            intel.Begin("[ 2026.11.09 12:00:03 ] Pilot > half");
            Assert.Empty(Poll());

            intel.Continue(" and the rest\r\n");
            Assert.Equal(new[] { "[ 2026.11.09 12:00:03 ] Pilot > half and the rest" }, Poll());
        }

        [Fact]
        public void ALineLongerThanTheReadBufferArrivesWhole()
        {
            EveLogWriter intel = ChatLog("Intel");
            Poll();

            string longLine = "[ 2026.11.09 12:00:11 ] Pilot > " + new string('x', 40000);
            intel.Line(longLine);

            Assert.Equal(new[] { longLine }, Poll());
        }

        // a character that logs in while SMT runs: its first lines count, its header is not log text
        [Fact]
        public void AFileThatAppearsLaterIsReadFromJustAfterItsHeader()
        {
            Poll();

            EveLogWriter game = Track(new EveLogWriter(LogPath(null, Start), chat: false, "Game Pilot"));
            game.Line("[ 2026.11.09 12:00:08 ] (combat) 250 to Jörmungandr");

            Assert.Equal(new[] { "[ 2026.11.09 12:00:08 ] (combat) 250 to Jörmungandr" }, Poll());

            Assert.True(LogFileTail.TryReadHeader(game.FilePath, out string listener, out long headerEnd));
            Assert.Equal("Game Pilot", listener);
            Assert.Equal(Encoding.UTF8.GetByteCount(game.Header("Game Pilot")), headerEnd);
        }

        // the same login, caught in the moment between the client creating the file and writing to it
        [Fact]
        public void AFileFoundEmptySkipsItsHeaderOnceItIsWritten()
        {
            Poll();

            EveLogWriter local = Track(new EveLogWriter(LogPath("Local", Start), chat: true, listener: null));
            Assert.Empty(Poll());

            local.WriteHeader("Local Pilot");
            local.Line("[ 2026.11.09 12:00:10 ] EVE System > Channel changed to Local : Jita");

            Assert.Equal(new[] { "[ 2026.11.09 12:00:10 ] EVE System > Channel changed to Local : Jita" }, Poll());
            Assert.Equal("Local Pilot", Assert.Single(monitor.Files).Listener);
        }

        // EVE never deletes its logs: without this SMT would hold every log of the last years open
        [Fact]
        public void ASessionTooOldToStillBeWrittenIsNotFollowed()
        {
            Track(new EveLogWriter(LogPath("Yesterday", Start.AddHours(-27)), chat: true, "Test Pilot"));
            Track(new EveLogWriter(LogPath("LastNight", Start.AddHours(-20)), chat: true, "Test Pilot"));

            Poll();

            Assert.StartsWith("LastNight_", Assert.Single(monitor.Files).FileName);
        }

        // one bad log must not cost the intel from the others, nor fill SMT.log four times a second
        [Fact]
        public void AFileThatFailsIsReportedOnceAndTheOthersAreStillRead()
        {
            EveLogWriter alpha = ChatLog("Alpha");
            EveLogWriter bravo = ChatLog("Bravo");
            EveLogWriter charlie = ChatLog("Charlie");
            Poll();

            // Bravo's lines can't be handled: on two polls in a row, without having worked in between
            alpha.Line("alpha 1");
            bravo.Line("bravo 1");
            charlie.Line("charlie 1");
            Assert.Equal(new[] { "alpha 1", "charlie 1" }, Poll(failOn: "Bravo_").OrderBy(line => line));
            Assert.Equal("Bravo_", Assert.Single(errors));

            alpha.Line("alpha 2");
            bravo.Line("bravo 2");
            charlie.Line("charlie 2");
            Assert.Equal(new[] { "alpha 2", "charlie 2" }, Poll(failOn: "Bravo_").OrderBy(line => line));
            Assert.Single(errors);

            // once Bravo has worked again (here: nothing new to read), its next failure is reported again
            Assert.Empty(Poll(failOn: "Bravo_"));
            bravo.Line("bravo 3");
            Assert.Empty(Poll(failOn: "Bravo_"));
            Assert.Equal(2, errors.Count);

            bravo.Line("bravo 4");
            Assert.Equal(new[] { "bravo 4" }, Poll());
        }

        // how SMT knows at startup which system a character is in: the Local log's lines from before it was followed
        [Fact]
        public void TheLinesWrittenBeforeAFileWasFollowedCanBeReadBack()
        {
            EveLogWriter local = Track(new EveLogWriter(LogPath("Local", Start), chat: true, "Local Pilot"));
            local.Line("[ 2026.11.09 12:00:10 ] EVE System > Channel changed to Local : Jita");
            local.Line("[ 2026.11.09 12:05:00 ] EVE System > Channel changed to Local : Perimeter");
            long followedFrom = new FileInfo(local.FilePath).Length;
            local.Line("[ 2026.11.09 12:09:00 ] EVE System > Channel changed to Local : Urlen");

            Assert.True(LogFileTail.TryReadHeader(local.FilePath, out string listener, out long headerEnd));
            Assert.Equal("Local Pilot", listener);
            Assert.Equal(
                new[]
                {
                    "[ 2026.11.09 12:00:10 ] EVE System > Channel changed to Local : Jita",
                    "[ 2026.11.09 12:05:00 ] EVE System > Channel changed to Local : Perimeter",
                },
                LogFileTail.ReadLines(local.FilePath, headerEnd, followedFrom));
        }

        // the age of a session and a character's newest session both come from here; channel names may hold underscores
        [Theory]
        [InlineData("Local_20261108_123519_1117005149.txt", "2026-11-08 12:35:19")]
        [InlineData("Some_Intel_Channel_20261108_123519_1117005149.txt", "2026-11-08 12:35:19")]
        [InlineData("20261106_213457_1117005149.txt", "2026-11-06 21:34:57")]
        [InlineData("Local_copy.txt", null)]
        public void TheSessionStartIsTakenFromTheFileName(string fileName, string expectedUtc)
        {
            DateTime? expected = expectedUtc == null
                ? null
                : DateTime.SpecifyKind(DateTime.Parse(expectedUtc, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);

            Assert.Equal(expected, LogFileTail.SessionStartFromFileName(fileName));
        }

        /// <summary>
        /// A chat log with its header, in a session that started when the test did.
        /// </summary>
        private EveLogWriter ChatLog(string channel)
        {
            return Track(new EveLogWriter(LogPath(channel, Start), chat: true, "Test Pilot"));
        }

        /// <summary>
        /// Chat logs are named channel, session start, character; game logs session start and character alone.
        /// </summary>
        private string LogPath(string channel, DateTime sessionStart)
        {
            string stamp = sessionStart.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            return Path.Combine(folder, (channel == null ? string.Empty : channel + "_") + stamp + "_123456789.txt");
        }

        private EveLogWriter Track(EveLogWriter writer)
        {
            writers.Add(writer);
            return writer;
        }

        /// <summary>
        /// One poll, two seconds on so that it also looks for new files. Returns the lines handed over; a file whose
        /// name starts with <paramref name="failOn"/> fails in the handler instead.
        /// </summary>
        private List<string> Poll(string failOn = null)
        {
            now = now.AddSeconds(2);
            List<string> received = new List<string>();
            monitor.Poll(
                now,
                (tail, lines) =>
                {
                    if(failOn != null && tail.FileName.StartsWith(failOn, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("can't handle " + tail.FileName);
                    }

                    received.AddRange(lines);
                },
                (tail, exception) => errors.Add(tail.FileName.Substring(0, tail.FileName.IndexOf('_') + 1)));
            return received;
        }
    }
}
