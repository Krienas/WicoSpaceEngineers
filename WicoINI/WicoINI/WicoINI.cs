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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /// <summary>
        /// INI Processor.  Takes a string parameter from text panel, Customdata or wherever
        /// </summary>
        public class INIHolder
        {
            Dictionary<string, string> _Sections;
            Dictionary<string, string[]> _Lines;
            Dictionary<string, Dictionary<string, string>> _Keys;

            char _sectionStart = '[';
            char _sectionEnd = ']';
            string _sHeaderText = ""; // the text BEFORE any sections.  Will be saved as-is and regenerated
            MyGridProgram _pg;

            string _sLastINI = "";

            /// <summary>
            /// Have the sections been modified?  If so, they should be written back out/saved
            /// </summary>
            public bool IsDirty { get; private set; } = false;

//            bool _IsTextInvalid = false;
//            bool _IsLinesInvalid = false;
//            bool _IsKeysInvalid = false;

            /// <summary>
            /// Constructor,  Pass MyGridProgram so it can access things like Echo()
            /// pass String to parse.
            /// </summary>
            /// <param name="pg">Allow access to things like Echo()</param>
            /// <param name="sINI">String to parse</param>
            public INIHolder(MyGridProgram pg,  string sINI)
            {
                _pg = pg;
                _Sections = new Dictionary<string, string>();
                _Lines = new Dictionary<string, string[]>();
                _Keys = new Dictionary<string, Dictionary<string, string>>();

                ParseINI(sINI);
            }

            /// <summary>
            /// Re-parse string after construction
            /// </summary>
            /// <param name="sINI">String to parse</param>
            /// <returns>number of sections found</returns>
            public int ParseINI(string sINI)
            {
                // optimize if it is the same as last time..
                sINI.TrimEnd();

                if (_sLastINI == sINI)
                {
//                    _pg.Echo("INI:Same");
                    return _Sections.Count;
                }
//                else _pg.Echo("INI: NOT SAME");


                _Sections.Clear();
                _Lines.Clear();
                _Keys.Clear();
                _sHeaderText = "";
                IsDirty = false;
                _sLastINI = sINI;

                string[] aLines = sINI.Split('\n');

 //               _pg.Echo("INI: " + aLines.Count() + " Lines to process");
                for (int iLine=0;iLine<aLines.Count();iLine++)
                {
                    string sSection = "";
                    aLines[iLine].Trim();
                    if (aLines[iLine].StartsWith(_sectionStart.ToString()))
                    {
                        string sName = "";
                        for (int iChar = 1; iChar < aLines[iLine].Length; iChar++)
                            if (aLines[iLine][iChar] == _sectionEnd)
                                break;
                            else
                                sName += aLines[iLine][iChar];
                        if (sName != "")
                        {
                            sSection = sName.ToUpper();
                        }
                        else continue; // malformed line?

                        iLine++;
                        string sText = "";
                        string[] asLines= new string[aLines.Count()-iLine]; // maximum size.
                        int iSectionLine = 0;
                        Dictionary<string, string> dKeyValue = new Dictionary<string, string>();

                        for(; iLine < aLines.Count(); iLine++)
                        {
                            aLines[iLine].Trim();
                            if(aLines[iLine].StartsWith(_sectionStart.ToString()))
                            {
                                iLine--;
                                break;
                            }
                            // TODO: Support Mult-line strings?
                            // TODO: Support comments

                            sText += aLines[iLine] + "\n";
                            asLines[iSectionLine++] = aLines[iLine];

                            if(aLines[iLine].Contains("="))
                            {
                                string[] aKeyValue = aLines[iLine].Split('=');
                                if(aKeyValue.Count()>1)
                                {
                                    string key = aKeyValue[0];
                                    string value = "";
                                    for(int i1=1;i1<aKeyValue.Count();i1++)
                                    {
                                        value += aKeyValue[i1];
                                        if (i1 < aKeyValue.Count()) value += "=";
                                    }
                                    dKeyValue.Add(key, value);
                                }
                            }
                        }
                        _Keys.Add(sSection, dKeyValue);
                        _Lines.Add(sSection, asLines);
                        _Sections.Add(sSection, sText);
                    }
                    else
                    {
                        _sHeaderText += aLines[iLine] + "\n";
                    }
                }
                return _Sections.Count;
            }

            /// <summary>
            /// Get the raw text of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>The text of the section</returns>
            public string GetSection(string section)
            {
                string sText = "";
                if (_Sections.ContainsKey(section))
                    sText = _Sections[section];
                return sText;
            }

            /// <summary>
            /// Get the parsed lines of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>string array of the lines in the section</returns>
            public string[] GetLines(string section)
            {
                string[] as1= { "" };
                if (_Lines.ContainsKey(section))
                    as1 = _Lines[section];
                _pg.Echo("GetLines(" + section + ") : " + as1.Count() + " Lines");
                return as1;
            }

            /// <summary>
            /// Returns the value of the key in the section
            /// </summary>
            /// <param name="section">the section to check</param>
            /// <param name="key">the key to look for</param>
            /// <returns>the string of the value, null if key not found</returns>
            public string GetValue(string section, string key)
            {
                string sValue = null;

                if(_Keys.ContainsKey(section))
                {
                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key))
                        sValue = dValue[key];
                }

                return sValue;
            }
            /*
            public void WriteValue(string section, string key, string value)
            {
                _IsTextInvalid = true;
                _IsLinesInvalid = true;
                IsDirty = true;
                if (_Keys.ContainsKey(section))
                {
                    Dictionary<string, string> dKeyValue = new Dictionary<string, string>();

                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key))
                        dValue[key] =value;
                    else
                        dValue.Add(key, value);
                }
                else
                {
                    Dictionary<string, string> dKeyValue = new Dictionary<string, string>();
                    dKeyValue.Add(key, value);

                    _Keys.Add(section, dKeyValue);
                }
            }
            */

            /// <summary>
            /// Modify the section to have the specified text
            /// </summary>
            /// <param name="section">the name of the section to modify</param>
            /// <param name="sText">the text to set as the new text</param>
            public void WriteSection(string section, string sText)
            {
                sText.TrimEnd(); 
                section = section.ToUpper();
                if (_Sections.ContainsKey(section))
                {
                    if (_Sections[section] != sText)
                    {
//                        _pg.Echo("INI WriteSection: Now Dirty:"+section);
                        _Sections[section] = sText;
                        IsDirty = true;
                    }
                }
                else
                {
//                    _pg.Echo("INI WriteSection: Adding new Section:" + section);
                    IsDirty = true;
                    _Sections.Add(section, sText);
                }
            }

            /// <summary>
            /// Generate the full text again. This includes any modifications that have been made
            /// </summary>
            /// <param name="bClearDirty">clear the dirty flag. Use if you are writing the text back to the original location</param>
            /// <returns>full text including ALL sections and header information</returns>
            public string GenerateINI(bool bClearDirty=true)
            {
                string sIni = "";
                sIni += _sHeaderText;
                _pg.Echo("INI Generate: " + _Sections.Count() + "sections");
                foreach (var kv in _Sections)
                {
                    sIni += _sectionStart + kv.Key + _sectionEnd + "\n";
                    sIni += kv.Value; // ends in "\n"
                }
                if(bClearDirty) IsDirty = false;
                return sIni;

            }
        }


    }
}