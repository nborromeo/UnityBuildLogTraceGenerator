namespace Unity.Profiling.BuildLogAnalyzer.ArgsParsers
{
    public class SimpleArgsParser : IMarkerArgsParser
    {
        public string argName;
        public int startIndex;
        public int length = -1;

        public string GetArgs(Marker marker)
        {
            ref var message = ref marker.InitMessage;
            var argLength = length >= 0 ? length : message.Length;
            return $", \"{argName}\":\"{message.Substring(startIndex, argLength - startIndex).Replace('\\', ' ').Replace('\'', ' ').Replace('\"', ' ')}\"";
        }
    }
}