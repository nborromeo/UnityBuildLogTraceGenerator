namespace Unity.Profiling.BuildLogAnalyzer.NameParsers
{
    public class SpriteOperationNameParser : IMarkerNameParser
    {
        private const int OperationNameOffset = 30;
        private const int OperationNameEndOffset = 4;

        public string GetName(Marker marker)
        {
            ref var message = ref marker.EndMessage;
            var opNameStartIndex = marker.EndMessageInitIndex + OperationNameOffset;
            var opNameEndIndex = message.IndexOf('\"', opNameStartIndex) - OperationNameEndOffset;
            return "Sprite Op: " + message.Substring(opNameStartIndex, opNameEndIndex - opNameStartIndex);
        }
    }
}