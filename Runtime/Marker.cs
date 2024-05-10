using System.Text;

namespace Unity.Profiling.BuildLogAnalyzer
{
    public class Marker
    {
        private BuildLogParser Parser => BuildLogParser.Current;

        public MarkerType Type { get; }
        public long InitLine { get; set; }
        public long EndLine { get; set; }
        public long StartTimeUs { get; set; }
        public long DurationTimeUs { get; set; }
        public bool Closed { get; private set; }

        public bool HasInitMessage => InitLine >= 0;
        public bool HasEndMessage => EndLine >= 0;
        public long MessageLine => HasInitMessage ? InitLine : EndLine;
        public ref string InitMessage => ref Parser.Lines[InitLine - 1];
        public ref string EndMessage => ref Parser.Lines[EndLine - 1];
        public ref string GetMessage(int indexOffset) => ref Parser.Lines[MessageLine - 1 + indexOffset];

        public Marker(MarkerType type)
        {
            Type = type;
        }

        public bool LogAndCheckFinish(ref string message, long usSinceStart, int line)
        {
            if (!Type.ShouldCloseMarker(ref message))
            {
                return false;
            }

            Close(usSinceStart, line);
            return true;
        }

        public void Close(long usSinceStart, int line)
        {
            EndLine = line;
            DurationTimeUs = usSinceStart - StartTimeUs;
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

            sb.Append(string.Format(Type.Format, name ?? Type.name, DurationTimeUs, StartTimeUs, InitLine, EndLine,
                args ?? string.Empty));
        }
    }
}