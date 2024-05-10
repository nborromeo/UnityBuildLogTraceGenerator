namespace Unity.Profiling.BuildLogAnalyzer.ArgsParsers
{
    public class SpriteTextureGenerationArgsParser : IMarkerArgsParser
    {
        private const int AtlasNameOffset = 19;

        public string GetArgs(Marker marker)
        {
            //TODO: This is the next atlas name, not the current one, fix this if possible
            ref var message = ref marker.GetMessage(1);
            if (message.IndexOf("Processing Atlas : ", BuildLogParser.MessageInitIndex) < 0)
            {
                return string.Empty;
            }

            var initNameIndex = BuildLogParser.MessageInitIndex + AtlasNameOffset;
            return $", \"atlasName\":\"{message.Substring(initNameIndex, message.Length - initNameIndex)}\"";
        }
    }
}