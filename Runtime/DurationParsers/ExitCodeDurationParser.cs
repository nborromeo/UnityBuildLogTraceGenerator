using System;

namespace Unity.Profiling.BuildLogAnalyzer.DurationParsers
{
    public class ExitCodeDurationParser : IDurationParser
    {
        public double ParseUs(string message)
        {
            double duration = 0;

            var minuteIndex = message.IndexOf("m:");
            var secondIndex = message.IndexOf("s");

            if (minuteIndex >= 0)
            {
                var secondStartIndex = minuteIndex + 2;
                duration += int.Parse(message.AsSpan(0, minuteIndex)) * 60 * 1000000;
                duration += int.Parse(message.AsSpan(secondStartIndex, message.Length - secondStartIndex - 1)) *
                            1000000;
            }
            else
            {
                var msStartIndex = secondIndex + 1;
                duration += int.Parse(message.AsSpan(0, secondIndex)) * 1000000;
                if (message.EndsWith("ms"))
                {
                    duration += int.Parse(message.AsSpan(msStartIndex, message.Length - msStartIndex - 2)) * 1000;
                }
            }

            return duration;
        }
    }
}