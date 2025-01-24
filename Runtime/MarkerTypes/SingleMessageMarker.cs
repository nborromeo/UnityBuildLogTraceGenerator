using System;

namespace Unity.Profiling.BuildLogAnalyzer.MarkerTypes
{
    public class SingleMessageMarker : MarkerType
    {
        public const int DurationUsReduction = 1000;
        
        public string preDurationText;
        public string postDurationText;
        public int muliplier = 1000000;
        public IDurationParser durationParser;

        public SingleMessageMarker()
        {
            formatOverride =
                "{{\"name\": \"{0}\", \"ph\": \"X\", \"dur\": {1}, \"tid\": \"{7}\", \"ts\": {2}, \"pid\": {6},  \"args\": {{\"approxInit\":{3}, \"end\":{4}{5}}}}}";
        }

        protected override bool CreateMarker(ref string message, long usSinceStart, int line, out Marker marker)
        {
            //TODO: Yes, i know, there might be a fancier way to do this with regex, but im lazy now :P
            var durationInit = message.IndexOf(preDurationText) + preDurationText.Length;
            var durationEnd = string.IsNullOrEmpty(postDurationText)
                ? message.Length
                : message.IndexOf(postDurationText, durationInit);

            if (durationInit < 0 || durationEnd < 0)
            {
                marker = null;
                return false;
            }

            var durationUs = durationParser?.ParseUs(message.Substring(durationInit, durationEnd - durationInit)) ??
                           double.Parse(message.AsSpan(durationInit, durationEnd - durationInit)) * muliplier;

            durationUs -= DurationUsReduction;

            base.CreateMarker(ref message, usSinceStart, line, out marker);
            marker.EndLine = line;
            marker.DurationTimeUs = (long) durationUs;
            marker.StartTimeUs -= marker.DurationTimeUs;
            marker.InitLine = BuildLogParser.Current.CloserLineToUsTime(marker.StartTimeUs);
            marker.Close();
            return true;
        }
    }
}