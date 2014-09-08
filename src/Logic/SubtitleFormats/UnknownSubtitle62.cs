﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UnknownSubtitle62 : SubtitleFormat
    {

        //  338:  00:24:34.00  00:24:37.10   [51]
        private static Regex regexTimeCodes = new Regex(@"^\d+:\s+\d\d:\d\d:\d\d\.\d\d\s+\d\d:\d\d:\d\d\.\d\d\s+\[\d+\]$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".txt"; }
        }

        public override string Name
        {
            get { return "Unknown 62"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();

            var sb = new StringBuilder();
            foreach (string line in lines)
                sb.AppendLine(line);

            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            string format = "{0}:  {1}  {2}   [{3}]";
            var sb = new StringBuilder();
            int count = 1;
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                sb.AppendLine(string.Format(format, count, EncodeTimeCode(p.StartTime), EncodeTimeCode(p.EndTime), p.Text.Length));
                sb.AppendLine(p.Text);
                sb.AppendLine();
                count++;
            }
            return sb.ToString().Trim();
        }

        private static string EncodeTimeCode(TimeCode time)
        {
            return string.Format("{0:00}:{1:00}:{2:00}.{3:00}", time.Hours, time.Minutes, time.Seconds, MillisecondsToFramesMaxFrameRate(time.Milliseconds));
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;
            bool expectStartTime = true;
            var p = new Paragraph();
            subtitle.Paragraphs.Clear();
            foreach (string line in lines)
            {
                string s = line.Trim().Replace("*", string.Empty);
                var match = regexTimeCodes.Match(s);
                if (match.Success)
                {
                    string[] parts = s.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(p.Text))
                            {
                                subtitle.Paragraphs.Add(p);
                                p = new Paragraph();
                            }
                            p.StartTime = DecodeTimeCode(parts[1]);
                            p.EndTime = DecodeTimeCode(parts[2]);
                            expectStartTime = false;
                        }
                        catch (Exception exception)
                        {
                            _errorCount++;
                            System.Diagnostics.Debug.WriteLine(exception.Message);
                        }
                    }
                }
                else if (line.Trim().Length == 0)
                {
                    if (p != null)
                    {
                        if (p.StartTime.TotalMilliseconds == 0 && p.EndTime.TotalMilliseconds == 0)
                            _errorCount++;
                        else
                            subtitle.Paragraphs.Add(p);
                        p = new Paragraph();
                    }
                }
                else if (line.Trim().Length > 0 && !expectStartTime)
                {
                    p.Text = (p.Text + Environment.NewLine + line).Trim();
                    if (p.Text.Length > 500)
                    {
                        _errorCount += 10;
                        return;
                    }
                    while (p.Text.Contains(Environment.NewLine + " "))
                        p.Text = p.Text.Replace(Environment.NewLine + " ", Environment.NewLine);
                }
            }
            if (!string.IsNullOrEmpty(p.Text))
                subtitle.Paragraphs.Add(p);

            subtitle.RemoveEmptyLines();
            subtitle.Renumber(1);
        }

        private static TimeCode DecodeTimeCode(string part)
        {
            string[] parts = part.Split(".:".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //00:00:07:12
            string hour = parts[0];
            string minutes = parts[1];
            string seconds = parts[2];
            string frames = parts[3];

            return new TimeCode(int.Parse(hour), int.Parse(minutes), int.Parse(seconds), FramesToMillisecondsMax999(int.Parse(frames)));
        }

    }
}
