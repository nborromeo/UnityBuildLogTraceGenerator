using System;
using System.Text;

namespace Unity.Profiling.BuildLogAnalyzer
{
    public class Marker
    {
        private long _durationTimeUs;
        private BuildLogParser Parser => BuildLogParser.Current;

        public MarkerType Type { get; }
        public long InitLine { get; set; }
        public long EndLine { get; set; }
        public long StartTimeUs { get; set; }
        public bool Closed { get; private set; }
        public int Pid { get; internal set; }

        public bool HasInitMessage => InitLine >= 0;
        public bool HasEndMessage => EndLine >= 0;
        public long MessageLine => HasInitMessage ? InitLine : EndLine;
        public ref string InitMessage => ref Parser.Lines[InitLine - 1];
        public int InitMessageInitIndex => BuildLogParser.Current.GetMessageInitIndex(ref InitMessage);
        public ref string EndMessage => ref Parser.Lines[EndLine - 1];
        public int EndMessageInitIndex => BuildLogParser.Current.GetMessageInitIndex(ref EndMessage);
        
        public ref string GetMessage(int indexOffset) => ref Parser.Lines[MessageLine - 1 + indexOffset];
        public int GetMessageInitIndex(int indexOffset) => BuildLogParser.Current.GetMessageInitIndex(ref GetMessage(indexOffset));

        public float DurationTime => DurationTimeUs / 1000000f;

        public long DurationTimeUs
        {
            get => _durationTimeUs;
            set => _durationTimeUs = Math.Max(value, BuildLogParser.MinMarkerDurationUs);
        }

        public Marker(MarkerType type)
        {
            Pid = BuildLogParser.Current.CurrentLinePid;
            Type = type;
        }

        public bool LogAndCheckFinish(ref string message, long[] usSinceStartPerPid, int line)
        {
            if (!Type.ShouldCloseMarker(ref message))
            {
                return false;
            }

            Close(usSinceStartPerPid, line);
            return true;
        }

        public void Close(long[] usSinceStartPerPid, int line)
        {
            EndLine = line;
            DurationTimeUs = usSinceStartPerPid[Pid] - StartTimeUs;
            Close();
        }

        public void Close()
        {
            Closed = true;
        }

        public void AppendJson(StringBuilder sb)
        {
            string args = null;
            if (Type.argsParser != null)
            {
                args = Type.argsParser.GetArgs(this);
            }

            string name = null;
            if (Type.nameParser != null)
            {
                name = Type.nameParser.GetName(this);
            }

            sb.Append(string.Format(
                Type.Format, 
                name ?? Type.name, 
                DurationTimeUs, 
                StartTimeUs, 
                InitLine, 
                EndLine,
                args ?? string.Empty,
                Pid,
                Type.trackId));
        }
    }
}