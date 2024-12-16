using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Codice.Client.BaseCommands.Merge.IncomingChanges;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEngine;

namespace Unity.Profiling.BuildLogAnalyzer
{
    public partial class BuildLogParser
    {
        private const int LongMessagesOffset = 10; //Small offset to ensure long messages fit inside other scopes
        private const string JsonFormatOpen = "{\"traceEvents\": [";
        private const string JsonFormatClose = "]}";

        public static BuildLogParser Current { get; private set; }
        

        private readonly List<Marker> _openMarkers = new();
        private readonly List<Marker> _closedMarkers = new();

        public string[] Lines { get; private set; }
        public long[] LinesTimeUs { get; private set; }
        public int CurrentLine { get; private set; }

        public long MinMarkerDurationUs { get; }

        public BuildLogParser(long minDurationUs = 200000)
        {
            MinMarkerDurationUs = minDurationUs;
        }

        public void Analyze(string filePath, string outputPath)
        {
            var usSinceStart = 0L;
            var hasInitialTime = false;
            var initialLineTime = new DateTime();
            
            Current = this;
            Lines = File.ReadAllLines(filePath);
            LinesTimeUs = new long[Lines.Length];
            
            for (CurrentLine = 0; CurrentLine < Lines.Length; CurrentLine++)
            {
                var line = Lines[CurrentLine];
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

                LinesTimeUs[CurrentLine] = usSinceStart;

                var lastUsSinceStart = usSinceStart;
                usSinceStart = (long) currentLineTime.Subtract(initialLineTime).TotalMilliseconds * 1000;
                var message = line.Substring(currentMessageInitIndex, line.Length - currentMessageInitIndex);
                var createdOrClosedMarker = CloseOpenMarkers(ref message, usSinceStart, CurrentLine + 1);
                createdOrClosedMarker |= CreateNewMarkers(ref message, usSinceStart, CurrentLine + 1);

                if (!createdOrClosedMarker)
                {
                    DetectLongMessage(usSinceStart, lastUsSinceStart, CurrentLine + 1);
                }
            }

            CloseAllOpenMarkers(usSinceStart, Lines.Length);
            AddGlobalMarker(usSinceStart, Lines.Length);
            OutputJson(outputPath);
            ClearResources();
        }
        protected virtual bool TryParseDateTime(ref string line, int messageIndex, out DateTime time)
        {
            //return DateTime.TryParse(line.AsSpan(0, messageIndex), out time);

            if (line.Length < 1 || line[0] != '[')
            {
                time = new DateTime();
                return false;
            }

            time = new DateTime(2024, 12, 16,
                int.Parse(line.AsSpan(1, 2)),
                int.Parse(line.AsSpan(4, 2)),
                int.Parse(line.AsSpan(7, 2)));
            
            return true;
        }

        public virtual int GetMessageInitIndex(ref string line)
        {
            //return 29;
            
            const int colonIndex = 13;
            return line.Contains("[Step") ? line.IndexOf(']', colonIndex) + 2 : colonIndex;
        }

        private bool CloseOpenMarkers(ref string message, long usSinceStart, int line)
        {
            var markerClosed = false;
            for (var i = 0; i < _openMarkers.Count; i++)
            {
                var openMarker = _openMarkers[i];
                if (openMarker.LogAndCheckFinish(ref message, usSinceStart, line))
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
                _closedMarkers.Add(marker);
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
                _closedMarkers.Add(new Marker(LongMessageMarkerType)
                {
                    StartTimeUs = lastUsSinceStart + LongMessagesOffset,
                    DurationTimeUs = usTimeSinceLastMessage - LongMessagesOffset,
                    InitLine = line - 1,
                    EndLine = line
                });
            }
        }

        private void CloseAllOpenMarkers(long usSinceStart, int lastLine)
        {
            foreach (var openMarker in _openMarkers)
            {
                openMarker.Close(usSinceStart, lastLine);
                CloseMarker(openMarker);
            }

            _openMarkers.Clear();
        }

        private void AddGlobalMarker(long usSinceStart, int lastLine)
        {
            _closedMarkers.Add(new Marker(GlobalMarkerType)
            {
                DurationTimeUs = usSinceStart,
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
    }
}