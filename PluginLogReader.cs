using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rainmeter;
using PluginLogReader.Tools;

/*
[Rainmeter]author=sinterlkaasupdate=1000 		; in milisecondsbackgroundmode=1[Metadata]name=LogReaderversion=0.0.1[Variables]font_name=Trebuchet MSfont_size=9; Colorsfont_color=128,128,128,255color_graph_bg=0,0,0,0color_hline=0,0,0transparant=0,0,0,0[mXchat]Measure=PluginPlugin=Plugins\LogReader.dllType=XchatLines=20StripIrcCoding=truePath=C:\HexChat\scrollback\Network\#channel.txt[MeterDisplay]Meter=STRINGMeasureName=mXchatFontFace=#font_name#FontSize=#font_size#FontColor=#font_color#AntiAlias=1Text="%1"
 */

namespace PluginLogReader
{
    internal class Measure
    {
        public enum MeasureType
        {
            Xchat,  // XChat log type
            Mirc,   // mIRC log type
            Text    // Plain text
        }
        public MeasureType Type = MeasureType.Text;
        public FileSystemWatcher Watcher;
        private bool _fileChanged, _fswValid;
        public bool FileChanged { 
            get
            {
                return !_fswValid || _fileChanged;
            }
        }
        public string Path;
        public double Lines;
        public int LineLength;
        public bool StripIrcCoding;
        public bool IsValid;

        private StringBuilder _content;

        #region Initialize
        /// <summary>
        /// Called when a measure is created (i.e. when Rainmeter is launched or when a skin is refreshed). Initialize your measure object here.
        /// </summary>
        internal Measure()
        {
            _content = new StringBuilder();
            Lines = 15;
            LineLength = 100;
        }
        #endregion

        #region Load/Reload Settings
        /// <summary>
        ///  Called when the measure settings are to be read directly after Initialize. 
        ///  If DynamicVariables=1 is set on the measure, Reload is called on every update cycle (usually once per second). 
        ///  Read and store measure settings here. To set a default maximum value for the measure, assign to maxValue.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        /// <param name="maxValue">Max Value</param>
        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            IsValid = false;

            try
            {
                // Check if file exists
                var path = api.ReadString("Path", "");
                if (!File.Exists(path))
                {
                    Log(API.LogType.Error, "Path=" + path + " does not exists");
                    return;
                }
                Path = path;

                // Initialize FileSystemWatcher
                var directory = System.IO.Path.GetDirectoryName(path);
                var filename = System.IO.Path.GetFileName(path);
                if (directory == null || filename == null)
                    _fswValid = false;
                else
                {
                    Watcher = new FileSystemWatcher(directory, filename);
                    Watcher.Changed += WatcherChanged;
                    _fileChanged = true;
                    Log(msg:"Using FileSystemWatcher");
                }

                var strip = api.ReadString("StripIrcCoding", "false").ToLower();
                StripIrcCoding = strip.Equals("true");

                var lines = api.ReadDouble("Lines", 15);
                Lines = lines;

                var longLines = api.ReadInt("MaxLineLength", 100);
                LineLength = longLines;

                var type = api.ReadString("Type", "").ToLower();
                switch (type)
                {
                    case "xchat":
                        Type = MeasureType.Xchat;
                        break;
                    case "mirc":
                        Type = MeasureType.Mirc;
                        break;
                    case "text":
                        Type = MeasureType.Text;
                        break;
                    default:
                        Log(API.LogType.Error, "Type=" + type + " not valid");
                        return;
                }

                IsValid = true;
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, "Exception: " + ex.Message);
                Log(API.LogType.Debug, "Trace: " + ex.StackTrace);
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Called on every update cycle (usually once per second). 
        /// </summary>
        /// <returns>Return the numerical value for the measure here.</returns>
        internal double Update()
        {
            try
            {
                if (!IsValid || !FileChanged) return 0;

                if (!File.Exists(Path)) return 0;

                var content = new StringBuilder();
                var lineReader = new ReverseLineReader(Path);
                for (var i = Lines;i >= 0; i--)
                {
                    if (i > lineReader.Count()) break;
                    var line = lineReader.ElementAt((int) i);

                    if (StripIrcCoding)
                        line = Regex.Replace(line, @"[\x02\x1F\x0F\x16]|\x03(\d\d?(,\d\d?)?)?", String.Empty,
                                             RegexOptions.Compiled);

                    switch (Type)
                    {
                        case MeasureType.Xchat:
                            line = TextTools.XchatTimeStamp(line);
                            break;
                        case MeasureType.Mirc:
                            break;
                        case MeasureType.Text:
                            break;
                    }

                    // Check if we need to break long lines
                    var length = line.Length;
                    var oldline = line;
                    var pos = 0;
                    var breakingLine = (line.Length > LineLength);
                    while (length > 0)
                    {
                        if (breakingLine)
                        {
                            if (length > LineLength)
                            {
                                line = oldline.Substring(pos, LineLength);
                                length = length - line.Length;
                                pos += line.Length;
                            }
                            else if (pos > 0)
                            {
                                if (pos > oldline.Length)
                                    pos = oldline.Length;
                                line = oldline.Substring(pos, oldline.Length - pos);
                                length = 0;
                            }
                        }
                        else
                            length = 0;

                        content.AppendLine(line);
                    }
                }
                _content = content;
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, "Exception: " + ex.Message);
                Log(API.LogType.Debug, "Trace: " + ex.StackTrace);
            }
            _fileChanged = false;
            return DateTime.Now.Ticks;
        }
        #endregion

        #region Get String
        /// <summary>
        /// Called on-demand (in other words, may be called multiple times or not at all during a update cycle). 
        /// Return the string value for the measure here. Do not process data or consume CPU time in this function. 
        /// In most cases, you should process data, assign the result to a string, and return it in GetString. 
        /// If the plugin returns only numerical values, do not implement GetString. Check the PluginSystemVersion in the SDK for an example.
        /// </summary>
        /// <returns>string value</returns>
        internal string GetString()
        {
            return _content.ToString();
        }
        #endregion

        #region Execute Bang
        /// <summary>
        /// Called by Rainmeter when a !CommandMeasure bang is sent to the measure. 
        /// This can be used to change some data within the measure, or to interact with another application. 
        /// A good example of using this function can be found in the NowPlaying plugin.
        /// </summary>
        /// <param name="args">String containing the arguments to parse.</param>
        internal void ExecuteBang(string args)
        {
        }
        #endregion

        public void Log(API.LogType logType = API.LogType.Notice, string msg = "")
        {
            API.Log(logType, "PluginLogReader.dll: " + msg);
        }

        // File Change watcher
        void WatcherChanged(object sender, FileSystemEventArgs e)
        {
            _fileChanged = true;
        }
    }
}
