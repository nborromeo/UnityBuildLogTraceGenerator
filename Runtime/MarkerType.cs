namespace Unity.Profiling.BuildLogAnalyzer
{
    public class MarkerType
    {
        public const string JsonFormat =
            "{{\"name\": \"{0}\", \"ph\": \"X\", \"dur\": {1}, \"tid\": 1, \"ts\": {2}, \"pid\": 1,  \"args\": {{\"init\":{3}, \"end\":{4}{5}}}}}";

        public string name;
        public string openText;
        public string[] mustHave;
        public string[] closeTexts;
        public IMarkerArgsParser argsParser;
        public IMarkerNameParser nameParser;
        public string formatOverride;

        public string Format => formatOverride ?? JsonFormat;

        public bool TryCreateMarker(ref string message, long usSinceStart, int line, out Marker marker)
        {
            marker = null;

            if (!message.StartsWith(openText))
            {
                return false;
            }

            if (mustHave != null)
            {
                foreach (var mustHaveText in mustHave)
                {
                    if (!message.Contains(mustHaveText))
                    {
                        return false;
                    }
                }
            }

            return CreateMarker(ref message, usSinceStart, line, out marker);
        }

        protected virtual bool CreateMarker(ref string message, long usSinceStart, int line, out Marker marker)
        {
            marker = new Marker(this) {StartTimeUs = usSinceStart, InitLine = line};
            return true;
        }

        public bool ShouldCloseMarker(ref string message)
        {
            for (int i = 0; i < closeTexts.Length; i++)
            {
                if (message.StartsWith(closeTexts[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}