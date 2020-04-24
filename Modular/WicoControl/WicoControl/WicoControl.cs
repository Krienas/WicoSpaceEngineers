﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {

        public class WicoControl
        {
            public float fMaxWorldMps = 100f;

            bool _bDebug = false;

            #region MODES
            const string MODECHANGETAG = "[WICOMODECHANGE]";
            int _iMode = -1;
            int _iState = -1;
            public int IMode
            {
                get
                {
                    return _iMode;
                }

                set
                {
                    SetMode(value);
                    //                    _iMode = value;
                }
            }

            public int IState
            {
                get
                {
                    return _iState;
                }

                set
                {
                    SetState(value);
                    //                    _iState = value;
                }
            }

            List<Action<int, int, int, int>> ControlChangeHandlers = new List<Action<int, int, int, int>>();
            List<Action> ModeAfterInitHandlers = new List<Action>();

            public const int MODE_IDLE = 0;

            public const int MODE_DOCKING = 30;
            public const int MODE_DOCKED = 40;
            public const int MODE_LAUNCH = 50; // space launch

            public const int MODE_LAUNCHPREP = 100; // oribital launch prep
            public const int MODE_ORBITALLAUNCH = 120;
            public const int MODE_DESCENT = 150; // descend into space and stop NN meters above surface
            public const int MODE_ORBITALLAND = 151; // land from orbit
            public const int MODE_HOVER = 170;
            public const int MODE_LANDED = 180;

            public const int MODE_MINE = 500;
            public const int MODE_GOTOORE = 510;
            public const int MODE_BORESINGLE = 520;

            public const int MODE_EXITINGASTEROID = 590;


            public const int MODE_STARTNAV = 600; // start the navigation operations
            public const int MODE_GOINGTARGET = 650;
            public const int MODE_NAVNEXTTARGET = 670; // go to the next target
            public const int MODE_ARRIVEDTARGET = 699; // we have arrived at target


            public const int MODE_DOSCANS = 900; // Start scanning

            public const int MODE_ATTENTION = 9999;

            public void SetMode(int theNewMode, int theNewState = 0)
            {
                // do nothing if we are already in that mode
                if (_iMode == theNewMode)
                    return;
//                thisProgram.ErrorLog("Set M=" + theNewMode + " S=" + theNewState+" OM="+IMode+" OS="+_iState);

                // possible optimization.. make modules register for what modes they care about...
                string sData = "";
                sData += _iMode.ToString() + "\n";
                sData += _iState.ToString() + "\n";
                sData += theNewMode.ToString() + "\n";
                sData += theNewState.ToString() + "\n";

                SendToAllSubscribers(MODECHANGETAG, sData);
                HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                _iMode = theNewMode;
                _iState = theNewState;
                WantOnce();
            }

            public void SetState(int theNewState)
            {
                // not synced..
//                thisProgram.ErrorLog("Set S=" + theNewState);

                _iState = theNewState;
            }
            public bool AddControlChangeHandler(Action<int, int, int, int> handler)
            {
                if (!ControlChangeHandlers.Contains(handler))
                    ControlChangeHandlers.Add(handler);
                return true;
            }
            void HandleModeChange(int fromMode, int fromState, int toMode, int toState)
            {
                foreach (var handler in ControlChangeHandlers)
                {
                    handler(fromMode, fromState, toMode, toState);
                }
            }

            public bool AddModeInitHandler(Action handler)
            {
                if (!ModeAfterInitHandlers.Contains(handler))
                    ModeAfterInitHandlers.Add(handler);
                return true;
            }
            public void ModeAfterInit(MyIni theIni)
            {
                _iState = theIni.Get("WicoControl", "State").ToInt32(_iState);
                _iMode = theIni.Get("WicoControl", "Mode").ToInt32(_iMode);

//                thisProgram.ErrorLog("MAI:M=" + _iMode.ToString() + " S=" + _iState.ToString());
//                thisProgram.ErrorLog(thisProgram.Storage);

                foreach (var handler in ModeAfterInitHandlers)
                {
                    handler();
                }
            }

            void SaveHandler(MyIni theIni)
            {
//                thisProgram.ErrorLog("wicocontrol save handler");
//                thisProgram.ErrorLog("WCSH:M=" + _iMode.ToString() + " S=" + _iState.ToString());
                theIni.Set("WicoControl", "Mode", _iMode);
                theIni.Set("WicoControl", "State", _iState);
            }

            #endregion

            #region Updates
            bool bWantOnce = false;
            bool bWantFast = false;
            bool bWantMedium = false;
            bool bWantSlow = false;

            public void ResetUpdates()
            {
                bWantOnce = false;
                bWantFast = false;
                bWantMedium = false;
                bWantSlow = false;
            }
            public void WantOnce()
            {
                bWantOnce = true;
            }
            public void WantFast()
            {
                bWantFast = true;
            }
            public void WantMedium()
            {
                bWantMedium = true;
            }
            public void WantSlow()
            {
                bWantSlow = true;
            }
            public UpdateFrequency GenerateUpdate()
            {
                UpdateFrequency desired = 0;
                if (bWantOnce) desired |= UpdateFrequency.Once;
                if (bWantFast) desired |= UpdateFrequency.Update1;
                if (bWantMedium) desired |= UpdateFrequency.Update10;
                if (bWantSlow) desired |= UpdateFrequency.Update100;
                return desired;
            }
            #endregion

            Program thisProgram;
            readonly TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
            public WicoControl(Program program)
            {
                thisProgram = program;

                WicoControlInit();
            }

            /// <summary>
            /// List of Wico PB blocks on local construct
            /// </summary>
            List<long> _WicoMainSubscribers = new List<long>();
            bool bIAmMain = true; // assume we are main

            // WIco Main/Config stuff
            readonly string WicoMainTag = "WicoTagMain";
            readonly string YouAreSub = "YOUARESUB";
            readonly string UnicastTagTrigger = "TRIGGER";
            readonly string UnicastAnnounce = "IAMWICO";

            public void WicoControlInit()
            {
                // Wico Configuration system
                _WicoMainSubscribers.Clear();

                // send a messge to all local 'Wico' PBs to get configuration.  
                // This will be used to determine the 'master' PB and to know who to send requests to
                thisProgram.IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);


                thisProgram.wicoIGC.AddPublicHandler(WicoMainTag, WicoControlMessagehandler);
                thisProgram.wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);

                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);

                // ModeAfterInit gets called by main
                thisProgram.AddSaveHandler(SaveHandler);

            }
            public bool IamMain()
            {
                return bIAmMain;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument,MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (myCommandLine != null && myCommandLine.ArgumentCount > 1)
                {
                    if (myCommandLine.Argument(0) == "setmode")
                    {
                        int toMode = 0;
                        bool bOK = int.TryParse(myCommandLine.Argument(1), out toMode);
                        if (bOK)
                        {
                            SetMode(toMode);
                            WantOnce();
                        }
                    }
                }

            }

            public void SendToAllSubscribers(string tag, string argument)
            {
                foreach (var submodule in _WicoMainSubscribers)
                {
                    if (submodule == thisProgram.Me.EntityId) continue; // skip ourselves if we are in the list.
                    thisProgram.IGC.SendUnicastMessage(submodule, tag, argument);
                }
            }

            /// <summary>
            /// Broadcast handler for Wico Control Messages
            /// </summary>
            /// <param name="msg"></param>
            public void WicoControlMessagehandler(MyIGCMessage msg)
            {
                var tag = msg.Tag;

                var src = msg.Source;
                if (tag == WicoMainTag)
                {
                    if (msg.Data is string)
                    {
                        string data = (string)msg.Data;
                        if (data == "Configure")
                        {
                            thisProgram.IGC.SendUnicastMessage(src, UnicastAnnounce, "");
                        }
                    }
                }
            }

            /// <summary>
            /// Wico Unicast Handler for Wico Main
            /// </summary>
            /// <param name="msg"></param>
            public void WicoConfigUnicastListener(MyIGCMessage msg)
            {
                var tag = msg.Tag;
                var src = msg.Source;
                if (tag == YouAreSub)
                {
                    bIAmMain = false;
                }
                else if (tag == UnicastAnnounce)
                {
                    // another block announces themselves as one of our collective
                    if (_WicoMainSubscribers.Contains(src))
                    {
                        // already in the list
                    }
                    else
                    {
                        // not in the list
//                        _program.Echo("Adding new");
                        _WicoMainSubscribers.Add(src);
                    }
                    bIAmMain = true; // assume we are the main module
                    foreach (var other in _WicoMainSubscribers)
                    {
                        // if somebody has a lower ID, use them instead.
                        if (other < thisProgram.Me.EntityId)
                        {
                            bIAmMain = false;
//                            _program.Echo("Found somebody lower");
                        }
                    }
                }
                else if (tag == UnicastTagTrigger)
                {
                    // we are being informed that we were wanted to run for some reason (misc)
                    thisProgram.Echo("Trigger Received" + msg.Data);
                }
                else if (tag == MODECHANGETAG)
                {
                    string[] aLines = ((string)msg.Data).Split('\n');
                    // 0=old mode 1=old state. 2=new mode 3=new state
                    int theNewMode = Convert.ToInt32(aLines[2]);
                    int theNewState = Convert.ToInt32(aLines[3]);

                    if(_iMode != theNewMode)
                        HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                    _iMode = theNewMode;
                    _iState = theNewState;
                }
                // TODO: add more messages as needed
            }

            public void SetDebug(bool bDebug)
            {
                _bDebug = bDebug;
            }

            public void AnnounceState()
            {
                if (_bDebug)
                {
                    thisProgram.Echo("Me=" + thisProgram.Me.EntityId.ToString("X"));
                    thisProgram.Echo("Subscribers=" + _WicoMainSubscribers.Count());
                }
                if (bIAmMain) thisProgram.Echo("MAIN. Mode=" + IMode.ToString() + " S=" + IState.ToString());
                else thisProgram.Echo("SUB. Mode=" + IMode.ToString() + " S=" + IState.ToString());
            }

        }

    }
}
