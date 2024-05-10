namespace Unity.Profiling.BuildLogAnalyzer
{
    public interface IMarkerNameParser
    {
        public string GetName(Marker marker);
    }
}