using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unity.Profiling.BuildLogAnalyzer
{
    public partial class BuildLogParser
    {
        private const int LongMessagesOffset = 10; //Small offset to ensure long messages fit inside other scopes
        private const string JsonFormatOpen = "{\"traceEvents\": [";
        private const string JsonFormatClose = "]}";

        public static BuildLogParser Current { get; private set; }

        private int _maxTopMarkers;
        private int _lastValidLine;
        private readonly List<Marker> _openMarkers = new();
        private readonly List<Marker> _closedMarkers = new();

        public List<Marker> TopMarkers { get; }
        public string[] Lines { get; private set; }
        public long[] LinesTimeUs { get; private set; }
        public int CurrentLine { get; private set; }
        public int CurrentLinePid { get; private set; }
        public long MinMarkerDurationUs { get; }

        public BuildLogParser(long minDurationUs = 200000, int maxTopMarkers = 50)
        {
            _maxTopMarkers = maxTopMarkers;
            MinMarkerDurationUs = minDurationUs;
            TopMarkers = new List<Marker>(maxTopMarkers);
        }

        public void Analyze(string filePath, string outputPath)
        {
            var usSinceStartPerPid = new long[50]; //TODO: Move to a property?
            var hasInitialTime = false;
            var initialLineTime = new DateTime();

            _lastValidDatePerPid.Clear();
            _lastValidLine = 0;
            Current = this;
            Lines = File.ReadAllLines(filePath);
            LinesTimeUs = new long[Lines.Length];

            for (CurrentLine = 0; CurrentLine < Lines.Length; CurrentLine++)
            {
                var line = Lines[CurrentLine];
                CurrentLinePid = GetPID(ref line);
                var currentMessageInitIndex = GetMessageInitIndex(ref line);
                if (!TryParseDateTime(ref line, currentMessageInitIndex, out var currentLineTime))
                {
                    continue;
                }

                if (!hasInitialTime)
                {
                    hasInitialTime = true;
                    initialLineTime = currentLineTime;
                }
                
                var usSinceStart = usSinceStartPerPid[CurrentLinePid];
                var lastUsSinceStart = usSinceStart;
                usSinceStart = (long)currentLineTime.Subtract(initialLineTime).TotalMilliseconds * 1000;
                usSinceStartPerPid[CurrentLinePid] = usSinceStart;
                LinesTimeUs[CurrentLine] = usSinceStart;
                
                var message = line.Substring(currentMessageInitIndex, line.Length - currentMessageInitIndex);
                var createdOrClosedMarker = CloseOpenMarkers(ref message, usSinceStartPerPid, CurrentLine + 1);
                createdOrClosedMarker |= CreateNewMarkers(ref message, usSinceStart, CurrentLine + 1);

                if (!createdOrClosedMarker)
                {
                    DetectLongMessage(usSinceStart, lastUsSinceStart, CurrentLine + 1);
                }

                _lastValidLine = CurrentLine;
            }

            CloseAllOpenMarkers(usSinceStartPerPid, Lines.Length);
            AddGlobalMarker(usSinceStartPerPid, Lines.Length);
            OutputJson(outputPath);
            ClearResources();
        }

        private bool CloseOpenMarkers(ref string message, long[] usSinceStartPerPid, int line)
        {
            var markerClosed = false;
            for (var i = 0; i < _openMarkers.Count; i++)
            {
                var openMarker = _openMarkers[i];
                if (openMarker.LogAndCheckFinish(ref message, usSinceStartPerPid, line))
                {
                    markerClosed = true;
                    _openMarkers.RemoveAt(i--);
                    CloseMarker(openMarker);
                }
            }

            return markerClosed;
        }

        private void CloseMarker(Marker marker)
        {
            if (marker.DurationTimeUs > MinMarkerDurationUs)
            {
                AddClosedMarker(marker);
            }
        }

        private void AddClosedMarker(Marker marker)
        {
            _closedMarkers.Add(marker);
            var index = 0;

            while (true)
            {
                if (index >= TopMarkers.Count)
                {
                    if (TopMarkers.Count < _maxTopMarkers)
                    {
                        TopMarkers.Add(marker);
                    }

                    return;
                }

                var currentMarker = TopMarkers[index];
                if (currentMarker.DurationTimeUs > marker.DurationTimeUs)
                {
                    index++;
                }
                else
                {
                    TopMarkers.Insert(index, marker);
                    if (TopMarkers.Count > _maxTopMarkers)
                    {
                        TopMarkers.RemoveAt(TopMarkers.Count - 1);
                    }
                    return;
                }
            }
        }

        private bool CreateNewMarkers(ref string message, long usSinceStart, int line)
        {
            foreach (var markerType in MarkerTypes)
            {
                if (!markerType.TryCreateMarker(ref message, usSinceStart, line, out var marker))
                {
                    continue;
                }

                if (marker.Closed)
                {
                    CloseMarker(marker);
                }
                else
                {
                    _openMarkers.Add(marker);
                }

                return true;
            }

            return false;
        }

        private void DetectLongMessage(long usSinceStart, long lastUsSinceStart, int line)
        {
            var usTimeSinceLastMessage = usSinceStart - lastUsSinceStart;
            if (usTimeSinceLastMessage > MinMarkerDurationUs)
            {
                AddClosedMarker(new Marker(LongMessageMarkerType)
                {
                    StartTimeUs = lastUsSinceStart + LongMessagesOffset,
                    DurationTimeUs = usTimeSinceLastMessage - LongMessagesOffset,
                    InitLine = _lastValidLine + 1,
                    EndLine = line
                });
            }
        }

        private void CloseAllOpenMarkers(long[] usSinceStartPerPid, int lastLine)
        {
            foreach (var openMarker in _openMarkers)
            {
                openMarker.Close(usSinceStartPerPid, lastLine);
                CloseMarker(openMarker);
            }

            _openMarkers.Clear();
        }

        private void AddGlobalMarker(long[] usSinceStartPerPid, int lastLine)
        {
            AddClosedMarker(new Marker(GlobalMarkerType)
            {
                DurationTimeUs = usSinceStartPerPid[0],
                EndLine = lastLine
            });
        }

        private void OutputJson(string outputPath)
        {
            var sb = new StringBuilder();
            sb.Append(JsonFormatOpen);

            for (var i = 0; i < _closedMarkers.Count; i++)
            {
                _closedMarkers[i].AppendJson(sb);
                if (i < _closedMarkers.Count - 1)
                {
                    sb.Append(',');
                }
            }

            _closedMarkers.Clear();
            sb.Append(JsonFormatClose);
            File.WriteAllText(outputPath, sb.ToString());
        }

        private void ClearResources()
        {
            Lines = null;
            LinesTimeUs = null;
            Current = null;
        }

        public int CloserLineToUsTime(long usTime)
        {
            return CloserLineToUsTime(0, CurrentLine, usTime);
        }

        private int CloserLineToUsTime(int leftBound, int rightBound, long usTime)
        {
            var halfIndex = (leftBound + rightBound) / 2;
            var halfIndexUsTime = LinesTimeUs[halfIndex];
            var rangeCount = rightBound - leftBound;

            if (halfIndexUsTime == usTime || rangeCount <= 0)
            {
                return halfIndex;
            }

            return usTime > halfIndexUsTime
                ? CloserLineToUsTime(halfIndex + 1, rightBound, usTime)
                : CloserLineToUsTime(leftBound, halfIndex - 1, usTime);
        }


        #region Methods to adapt (TODO: Move to a separate object)

        private readonly Dictionary<int, DateTime> _lastValidDatePerPid = new();
        
        protected virtual bool TryParseDateTime(ref string line, int messageIndex, out DateTime time)
        {
            //Bamboo logs
            /* (line.Length < 1)
            {
                time = new DateTime();
                return false;
            }

            var hourIndex = line.IndexOf(':') - 2;
            var minuteIndex = hourIndex + 3;
            var secondIndex = minuteIndex + 3;

            time = new DateTime(2024, 12, 16,
                int.Parse(line.AsSpan(hourIndex, 2)),
                int.Parse(line.AsSpan(minuteIndex, 2)),
                int.Parse(line.AsSpan(secondIndex, 2)));

            return true;*/
            
            if (!line.StartsWith("2025"))
            {
                if (_lastValidDatePerPid.TryGetValue(CurrentLinePid, out var lastValidDate))
                {
                    time = lastValidDate;
                    return true;
                }
                
                time = new DateTime();
                return false;
            }

            time = DateTime.Parse(line.AsSpan(0, 24));
            _lastValidDatePerPid[CurrentLinePid] = time;
            return true;
        }

        protected virtual int GetPID(ref string line)
        {
            var workerIndex = line.IndexOf("[Worker", StringComparison.Ordinal);

            if (workerIndex < 0)
            {
                return 0;
            }
            
            return int.Parse(line.AsSpan(workerIndex + 7, 1));
        }

        public virtual int GetMessageInitIndex(ref string line)
        {
            //Bamboo logs
            //return line.IndexOf(':') + 7;
            
            /*if (line.Contains("[Worker"))
            {
                return 42;
            }*/

            if (!line.StartsWith("2025"))
            {
                return 0;
            }
            
            return 32;
        }
        #endregion
    }
}