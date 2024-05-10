namespace Unity.Profiling.BuildLogAnalyzer
{
    public interface IDurationParser
    {
        double ParseUs(string message);
    }
}