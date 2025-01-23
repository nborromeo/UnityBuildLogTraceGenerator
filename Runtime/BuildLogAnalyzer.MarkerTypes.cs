using System.Collections.Generic;
using Unity.Profiling.BuildLogAnalyzer.ArgsParsers;
using Unity.Profiling.BuildLogAnalyzer.DurationParsers;
using Unity.Profiling.BuildLogAnalyzer.MarkerTypes;
using Unity.Profiling.BuildLogAnalyzer.NameParsers;

namespace Unity.Profiling.BuildLogAnalyzer
{
    public partial class BuildLogParser
    {
        private static MarkerType GlobalMarkerType = new() {name = "Entire Log"};
        private static MarkerType LongMessageMarkerType = new() {name = "LongMessage"};

        public static readonly List<MarkerType> MarkerTypes = new()
        {
            new()
            {
                name = "Unity Process",
                openText = "Unity Editor version:",
                closeStartingTexts = new[] {"Memory Statistics:"}
            },
            new SingleMessageMarker
            {
                name = "Script Compilation",
                openText = "AssetDatabase: script compilation time:",
                preDurationText = "time: ",
                postDurationText = "s"
            },
            new SingleMessageMarker
            {
                name = "AssetDb Refresh",
                openText = "Asset Pipeline Refresh (id=",
                preDurationText = "Total: ",
                postDurationText = " seconds"
            },
            new SingleMessageMarker
            {
                name = "Asset Import",
                openText = "Start importing ",
                mustHave = new[] {"-> (artifact id:"},
                preDurationText = "') in ",
                postDurationText = " seconds",
                argsParser = new AssetImporterArgsParser()
            },
            new()
            {
                name = "Asset Import",
                openText = "Start importing ",
                closeStartingTexts = new[] {" -> (artifact id: "},
                argsParser = new AssetImporterArgsParser()
            },
            new SingleMessageMarker
            {
                name = "Sprite Operation",
                openText = "Sprite Atlas Operation : ",
                preDurationText = "\" took ",
                postDurationText = " sec",
                argsParser = new SpriteTextureGenerationArgsParser(),
                nameParser = new SpriteOperationNameParser()
            },
            new SingleMessageMarker
            {
                name = "External Proc",
                openText = "ExitCode: ",
                preDurationText = "Duration: ",
                durationParser = new ExitCodeDurationParser()
            },
            new()
            {
                name = "Domain Reload",
                openText = "Begin MonoManager ReloadAssembly",
                closeStartingTexts = new[] {"Domain Reload Profiling: "}
            },
            new()
            {
                name = "Addressables Build",
                openText = "DisplayProgressbar: Processing Addressable Group",
                closeStartingTexts = new[]
                {
                    "Addressable content build failure (duration : ",
                    "Addressable content successfully built (duration : "
                }
            },
            new()
            {
                name = "Scene load",
                openText = "Opening scene ",
                closeStartingTexts = new[]
                {
                    "\tTotal Operation Time:   ", 
                    "Problem detected while opening the Scene file",
                    "Loaded scene "
                },
            },
            new()
            {
                name = "Build Process",
                openText = "BuildPlayer: start building",
                closeStartingTexts = new[] {"Build Finished, Result:"},
            },
            new()
            {
                name = "Archive and compress bundles",
                openText = "DisplayProgressbar: Archive And Compress Bundles",
                closeStartingTexts = new[] {"DisplayProgressbar: "}
            },
            new()
            {
                name = "Task",
                openText = "Starting task ",
                closeStartingTexts = new[] {"Finished task"},
                argsParser = new SimpleArgsParser { argName = "task", startIndex = 42}
            },
        };
    }
}