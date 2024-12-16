namespace Unity.Profiling.BuildLogAnalyzer.ArgsParsers
{
    public class AssetImporterArgsParser : IMarkerArgsParser
    {
        private const int FileNameOffset = 16;

        public string GetArgs(Marker marker)
        {
            ref var message = ref marker.EndMessage;
            var fileNameIndex = marker.EndMessageInitIndex + FileNameOffset;
            var extensionIndex = message.IndexOf('.', fileNameIndex);
            var fileNameEndIndex = message.IndexOf(" using Guid(", extensionIndex);
            return fileNameEndIndex < 0
                ? null
                : $", \"extension\":\"{message.Substring(extensionIndex, fileNameEndIndex - extensionIndex)}\", " +
                  $" \"file\":\"{message.Substring(fileNameIndex, fileNameEndIndex - fileNameIndex)}\"";
        }
    }
}