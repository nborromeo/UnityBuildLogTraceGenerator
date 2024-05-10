namespace Unity.Profiling.BuildLogAnalyzer
{
    public interface IMarkerArgsParser
    {
        public string GetArgs(Marker marker);
    }
}