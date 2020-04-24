﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // 04092019 SE V1.190 text panel API changes
        // 01172018 Separate project
        // 01152018 INI settings
        // 110517 search order for the text panels
        // 2/25: Performance: only check blocks once, re-check on init.
        // use cached blocks 12/xx

        string sLoggingSection = "LOGGING";
        void LoggingInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sLoggingSection, "TextPanelReport", ref sTextPanelReport, true);
            iNIHolder.GetValue(sLoggingSection, "StatusName", ref sStatusName, true);
            iNIHolder.GetValue(sLoggingSection, "LongStatus", ref sLongStatus, true);
            iNIHolder.GetValue(sLoggingSection, "RangeReport", ref sRangeReport, true);
            iNIHolder.GetValue(sLoggingSection, "SledReport", ref ssledReport, true);
            iNIHolder.GetValue(sLoggingSection, "GPSTag", ref sGPSTextpanel, true);
        }

        // from Techniker
        //        IMyTextPanel textRangeReport = null;
        WicoLogger textRangeReport = null;
        string sRangeReport = "[RANGE]";

        //        IMyTextPanel statustextblock = null;
        WicoLogger statustextblock = null;
        string sStatusName = "Wico Craft Status";
        //        IMyTextPanel textLongStatus = null;
        WicoLogger textLongStatus = null;
        string sLongStatus = "Wico Craft Log";
        //        IMyTextPanel textPanelReport = null;
        WicoLogger textPanelReport = null;
        string sTextPanelReport = "Craft Report";

        WicoLogger gpsPanel = null;
        string sGPSTextpanel = "[GPS]";

        WicoLogger sledReport = null;
        string ssledReport = "[SMREPORT]";


        bool bLoggingInit = false;

        bool bLogMeansEcho = false;

        public class WicoLogger
        {
            Program _pg;
            string _sSearchName = "";
            // TODO: Change into Surfaces.
            List<IMyTextPanel> _textPanels = new List<IMyTextPanel>();
            string _sCurrentText = "";
            string _sOldtext = "";
            bool _bRefresh = false; // we want to get the current contents before adding more (unless "clear")

            bool _bNotCached = true;// we do NOT have the current contents.

            public WicoLogger(Program pg,string sName, bool bRefresh=false)
            {
                _pg = pg;
                _sSearchName = sName;
                _bRefresh = bRefresh;
                _bNotCached = true;
                
                _sCurrentText = "";
                _sOldtext = "";
                _textPanels.Clear();
                _textPanels= _pg.GetMeTextBlocksContains(_sSearchName);
                if (_textPanels.Count < 1)
                    _textPanels = _pg.GetTextBlocksContains(_sSearchName);
            }

            public void StatusLog(string text, bool bReverse=false)
            {
                if (text == "clear")
                {
                    _sCurrentText = "";
                    _sOldtext = "X";
                    _bNotCached = false; // we dont' care about what was.
                    return;
                }
                if(_bRefresh && _bNotCached)
                {
                    // on first call, get the current contents of text panel
                    _bNotCached = false;
                    if (_textPanels.Count > 0)
                    {
                        //V1.190
                       //_sCurrentText = _textPanels[0].GetPublicText();
                        _sCurrentText = _textPanels[0].GetText();
                        _sOldtext = "X";// it should NOT match current text.
                    }
                }
                if (bReverse)
                {
                    _sCurrentText = text + "\n" + _sCurrentText;
                }
                else _sCurrentText += text + "\n";
            }

            public void WritePanels()
            {
                if (_sOldtext != _sCurrentText)
                {
                    _bNotCached = true;
//                    _pg.Echo("Updating Panels:" + _sSearchName);
                    foreach (var t in _textPanels)
                    {
                        t.WriteText(_sCurrentText);

                        // V 1.190 Depracated 
                        //t.WritePublicText(_sCurrentText);
                    }
                    _sOldtext = _sCurrentText;
                }
//                _pg.Echo("No updates needed for " + _sSearchName);
            }
        }

        void initLogging()
        {
            statustextblock = getTextStatusBlock(true);
            textLongStatus = getTextBlock(sLongStatus,true); ;
            textPanelReport = getTextBlock(sTextPanelReport);
            /*
            if (textPanelReport == null)
            {
                List<IMyTerminalBlock> lmtb = new List<IMyTerminalBlock>();
                lmtb = GetBlocksContains<IMyTerminalBlock>(sTextPanelReport);
                if (lmtb.Count > 0)
                    textPanelReport = lmtb[0] as IMyTextPanel;
            }
            */
            textRangeReport = getTextBlock(sRangeReport);
            gpsPanel = getTextBlock(sGPSTextpanel,bIAmSubModule);
            sledReport = getTextBlock(ssledReport);
            bLoggingInit = true;
        }

        void PanelsClearAll()
        {
            if (statustextblock != null) StatusLog("clear", statustextblock);
            if (textLongStatus != null) StatusLog("clear", textLongStatus);
            if (textPanelReport != null) StatusLog("clear", textPanelReport);
            if (textRangeReport != null) StatusLog("clear", textRangeReport);
            if (gpsPanel != null) StatusLog("clear", gpsPanel);
            if (sledReport != null) StatusLog("clear", sledReport);

        }

        void UpdateAllPanels()
        {
            if (statustextblock != null) statustextblock.WritePanels();
            if (textLongStatus != null) textLongStatus.WritePanels();
            if (textPanelReport != null) textPanelReport.WritePanels();
            if (textRangeReport != null) textRangeReport.WritePanels();
            if (gpsPanel != null) gpsPanel.WritePanels();
            if (sledReport != null) sledReport.WritePanels();
        }

        WicoLogger getTextBlock(string stheName, bool bRefresh=false)
        {
            WicoLogger wicoLogger = new WicoLogger(this, stheName,bRefresh);
            /*
                        IMyTextPanel textblock = null;
                        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                        blocks = GetBlocksNamed<IMyTerminalBlock>(stheName);
                        if (blocks.Count < 1)
                        {
                            blocks = GetMeBlocksContains<IMyTextPanel>(stheName);
                            if (blocks.Count < 1)
                                blocks = GetBlocksContains<IMyTextPanel>(stheName);
                        }
                        if (blocks.Count > 1)
                            throw new OurException("Multiple status blocks found: \"" + stheName + "\"");
                        else
                            if (blocks.Count > 0)
                            textblock = blocks[0] as IMyTextPanel;
                        return textblock;
                        */
            return wicoLogger;
        }

        WicoLogger getTextStatusBlock(bool force_update = false)
        {
            if ((statustextblock != null || bLoggingInit) && !force_update) return statustextblock;
            statustextblock = getTextBlock(sStatusName);
            return statustextblock;
        }
        void StatusLog(string text, WicoLogger wLog, bool bReverse = false)
        {
            if (wLog == null) return;
            wLog.StatusLog(text, bReverse);
            /*
            if (text.Equals("clear"))
            {
                wLog.WritePublicText("");
            }
            else
            {
                if (bReverse)
                {
                    string oldtext = wLog.GetPublicText();
                    wLog.WritePublicText(text + "\n" + oldtext);
                }
                else wLog.WritePublicText(text + "\n", true);
                // block.WritePublicTitle(DateTime.Now.ToString());
            }
            wLog.ShowTextureOnScreen();
            wLog.ShowPublicTextOnScreen();
            */
        }

        void Log(string text)
        {
            StatusLog(text, getTextStatusBlock());
            if (bLogMeansEcho && text != "clear") Echo(text);
        }
        string progressBar(double percent)
        {
            int barSize = 75;
            if (percent < 0) percent = 0;
            int filledBarSize = (int)(percent * barSize) / 100;
            if (filledBarSize > barSize) filledBarSize = barSize;
            string sResult = "[" + new String('|', filledBarSize) + new String('\'', barSize - filledBarSize) + "]";
            return sResult;
        }

        //////
        void debugGPSOutput(string sName, Vector3D vPosition)
        {
            string s1;
            s1 = "GPS:" + sName + ":" + Vector3DToString(vPosition) + ":";
            StatusLog(s1, gpsPanel);
        }

        string gpsName(string ShipName, string sQual)
        {
            //NOTE: GPS Name can be a MAX of 32 total chars.
            string s;
            int iName = ShipName.Length;
            int iQual = sQual.Length;
            if (iName + iQual > 32)
            {
                if (iQual > 31) return "INVALID";
                iName = 32 - iQual;
            }
            s = ShipName.Substring(0, iName) + sQual;
            s.Replace(":", "_"); // filter out bad characters
            s.Replace(";", "_"); // filter out bad characters
            return s;

        }

        string niceDoubleMeters(double thed)
        {
            string nice = "";
            if (thed > 1000)
            {
                nice = thed.ToString("N0") + "km";
            }
            else if (thed > 10)
            {
                nice = thed.ToString("0.0") + "m";
            }
            else
            {
                nice = thed.ToString("0.000") + "m";
            }
            return nice;
        }
        /*
        Vector2 SIZE = new Vector2(128, 128);
        Vector2 POS = new Vector2(64, 80);

        void DrawStuff(IMyTextSurface surface)
        {
            surface.ContentType = ContentType.SCRIPT;

            Vector2 surfaceSize = surface.SurfaceSize;
            Vector2 halfSurfaceSize = surfaceSize * 0.5f;
            float sideLength = surfaceSize.Y;

            using (var frame = surface.DrawFrame())
            {
                // Draw velocity indicator
                MySprite sprite;

                // Background
                sprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: new Color(50, 150, 255, 255), size: SIZE);
                sprite.Position = surfaceSize * 0.5f + POS;
                frame.Add(sprite);

                MySprite sprite2;
                sprite2 = new MySprite();
                sprite2.Type = SpriteType.TEXT;
                sprite2.Data = "Example Text";
                sprite2.Color = new Color(50, 150, 255, 255);
                sprite2.Size = SIZE;
                sprite2.Position = new Vector2(1, 1);
                //                sprite2.FontId = 0000;
                frame.Add(sprite2);


            }
        }
        */

        /*
         * jTurp 
        public Dictionary<string, float> _cornerLCDs = new Dictionary<string, float>()
        {
        { "LargeBlockCorner_LCD_1", 80.64f },
        { "LargeBlockCorner_LCD_2", 80.64f },
        { "LargeBlockCorner_LCD_Flat_1", 93.6f },
        { "LargeBlockCorner_LCD_Flat_2", 93.6f },
        { "SmallBlockCorner_LCD_1", 142.56f },
        { "SmallBlockCorner_LCD_2", 142.56f },
        { "SmallBlockCorner_LCD_Flat_1", 165.92f },
        { "SmallBlockCorner_LCD_Flat_2", 165.92f }
        };
        Those are the Y values that I've found work well
        */
    }
}
 