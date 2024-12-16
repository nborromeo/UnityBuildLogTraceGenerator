using System;

namespace Unity.Profiling.BuildLogAnalyzer.ArgsParsers
{
    public class SpriteTextureGenerationArgsParser : IMarkerArgsParser
    {
        private const int AtlasNameOffset = 19;

        public string GetArgs(Marker marker)
        {
            //TODO: This is the next atlas name, not the current one, fix this if possible
            ref var message = ref marker.GetMessage(1);
            var initIndex = marker.GetMessageInitIndex(1);
            if (message.IndexOf("Processing Atlas : ", initIndex, StringComparison.Ordinal) < 0)
            {
                return string.Empty;
            }

            var initNameIndex = initIndex + AtlasNameOffset;
            return $", \"atlasName\":\"{message.Substring(initNameIndex, message.Length - initNameIndex)}\"";
        }
    }
}