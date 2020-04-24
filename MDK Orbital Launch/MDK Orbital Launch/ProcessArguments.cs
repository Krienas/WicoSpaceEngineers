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


        bool moduleProcessArguments(string sArgument)
        {
//            sArgResults = "";
            // string output="";
            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
            {
//                Echo("Arg=" + sArgument);
//                Echo("PassedArg=" + sPassedArgument);
                if (sPassedArgument != "" && sPassedArgument != "timer")
                {
                    Echo("Using Passed Arg=" + sPassedArgument);
                    sArgument = sPassedArgument;
                }
            }

            if (sArgument == "init")
            {
                sInitResults = "";
                init = false;
                currentInit = 0;
                doInit(); // do first pass.
                return false;
            }

            string[] args = sArgument.Trim().Split(' ');

            if (args[0] == "timer")
            {
                // do nothing for sub-module
            }
            else
            if (args[0] == "setmaxspeed")
            {
                if (args.Length < 2)
                {
                    Echo("Invalid arg");
                    return false;
                }
                float fValue = 0;
                bool fOK = float.TryParse(args[1].Trim(), out fValue);
                if (!fOK)
                {
                    Echo("invalid float value:" + args[1]);
                    return false;
                }
                fMaxWorldMps = fValue;
 //               sArgResults = "max speed set to " + fMaxWorldMps.ToString() + "mps";

            }
            else if (args[0] == "autogyro")
            {
                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    craft_operation &= ~CRAFT_MODE_NOAUTOGYRO;
                else
                    craft_operation |= CRAFT_MODE_NOAUTOGYRO;
            }
            else if (args[0] == "resetlaunch")
            {
                bValidOrbitalHome = false;
                bValidOrbitalLaunch = false;
            }
            else if (args[0] == "wccs")
            {

            }
            else if (args[0] == "wcct")
            {

            }
            else
            {
                int iDMode;
                if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                {
//                    sArgResults = "mode set to " + iDMode;
                    setMode(iDMode);
                    // return true;
                }
                else
                {
//                    sArgResults = "Unknown argument:" + args[0];
                }
            }
            return false; // keep processing in main
        }

        bool moduleProcessAntennaMessage(string sArgument)
        {
            // we directly received an antenna message
            return false;
        }


    }
}