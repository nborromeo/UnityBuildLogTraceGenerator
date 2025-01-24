namespace Unity.Profiling.BuildLogAnalyzer.NameParsers
{
    public class NameAfterCharacter : IMarkerNameParser
    {
        public char Character = ':';
        
        public string GetName(Marker marker)
        {
            var indexOfCharacter = marker.InitMessage.IndexOf(Character, marker.InitMessageInitIndex) + 1;
            return marker.InitMessage.Substring(indexOfCharacter, marker.InitMessage.Length - indexOfCharacter);
        }
    }
}