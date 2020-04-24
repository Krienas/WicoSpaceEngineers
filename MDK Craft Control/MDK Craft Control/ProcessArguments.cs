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


        // multi-arg

        bool moduleProcessArguments(string sArgument)
        {
//            sStartupError += "\nArgument:" + sArgument;
            string[] varArgs = sArgument.Trim().Split(';');

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');
//                sStartupError += "\n Arg:" + iArg + "=" + args[0];
                if (args[0] == "timer")
                {
                    processTimerCommand();
                }
                else if (args[0] == "idle")
                    ResetToIdle();
                else if (args[0] == "setmode")
                {
                    if (args.Length < 2)
                    {
                        Echo("Invalid command format:\nsetmode <mode#>");
                    }
                    else
                    {
                        int iValue;
                        bool bOK = int.TryParse(args[1], out iValue);
                        if (!bOK)
                        {
                            Echo("Invalid INT value:" + args[1]);
                        }
                        else
                        {
                            Echo("Set Mode to" + iValue);
                            setMode(iValue);
                        }
                    }
                }
                else if (args[0] == "setstate")
                {
                    if (args.Length < 2)
                    {
                        Echo("Invalid command format:\nsetstate <state#>");
                    }
                    else
                    {
                        int iValue;
                        bool bOK = int.TryParse(args[1], out iValue);
                        if (!bOK)
                        {
                            Echo("Invalid INT value:" + args[1]);
                        }
                        else
                        {
                            Echo("Set State to" + iValue);
                            current_state = iValue;
                        }
                    }
                }
                else if (args[0] == "masterreset")
                    MasterReset();
                else if (args[0].ToLower() == "coast")
                {
                    //	Echo("Coast: backward =" + thrustBackwardList.Count.ToString());
                    if (thrustBackwardList.Count > 1)
                    {
                        blocksToggleOnOff(thrustBackwardList);
                        //                        blockApplyAction(thrustBackwardList, "OnOff");
                        //				blockApplyAction(thrustBackwardList, "OnOff_Off");
                    }
                }
                else if (args[0] == "setvaluef")
                {
                    Echo("SetValueFloat");
                    //Miner Advanced Rotor:UpperLimit:-24
                    string sArg = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        sArg += args[i];
                        if (i < args.Length - 1)
                        {
                            sArg += " ";
                        }
                    }
                    string[] cargs = sArg.Trim().Split(':');

                    if (cargs.Length < 3)
                    {
                        Echo("Invalid Args");
                        continue;
                    }
                    IMyTerminalBlock block;
                    block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(cargs[0]);
                    if (block == null)
                    {
                        Echo("Block not found:" + cargs[0]);
                        continue;
                    }
                    float fValue = 0;
                    bool fOK = float.TryParse(cargs[2].Trim(), out fValue);
                    if (!fOK)
                    {
                        Echo("invalid float value:" + cargs[2]);
                        continue;
                    }
                    Echo("SetValueFloat:" + cargs[0] + " " + cargs[1] + " to:" + fValue.ToString());
                    block.SetValueFloat(cargs[1], fValue);
                }
                else if (args[0] == "brake")
                {
                    Echo("brake");
                    //toggle brake
                    if (shipOrientationBlock is IMyShipController)
                    {
                        IMyShipController msc = shipOrientationBlock as IMyShipController;
                        bool bBrake = msc.HandBrake;
                        msc.ApplyAction("HandBrake");
                    }
                    else Echo("No Ship Controller found");

                }
                else if (args[0] == "genpatrol")
                {
//                    sStartupError += "\n FOUND genpatrol Command";
                    Echo("genpatrol");
                    double range = 500;
                    double height = 500;
                    if (args.Length > 1)
                    {
                    bool fOK = double.TryParse(args[1].Trim(), out range);
                    }
                    if (args.Length > 2)
                    {
                        bool fOK = double.TryParse(args[2].Trim(), out height);
                    }
                    string sCmd = "WICO:PATROL:";
                    sCmd = "";
                    Vector3D vTarget;
                    Vector3D vUp = shipOrientationBlock.WorldMatrix.Up;
                    if (shipOrientationBlock is IMyShipController)
                    {
                        Vector3D vNG = ((IMyShipController)shipOrientationBlock).GetNaturalGravity();
                        if (vNG.Length() > 0.05)
                        {
                            vUp = vNG;
                            vUp.Normalize();
                        }
                    }
                    sCmd += "4:"; // number of waypoints
                    StatusLog("clear", gpsPanel);
                    vTarget = shipOrientationBlock.GetPosition() + shipOrientationBlock.WorldMatrix.Up * height + shipOrientationBlock.WorldMatrix.Right * range;
                    debugGPSOutput("Patrol0", vTarget);
                    sCmd += Vector3DToString(vTarget)+":";
                    vTarget = shipOrientationBlock.GetPosition() + shipOrientationBlock.WorldMatrix.Up * height + shipOrientationBlock.WorldMatrix.Forward * range;
                    debugGPSOutput("Patrol1", vTarget);
                    sCmd += Vector3DToString(vTarget) + ":";
                    vTarget = shipOrientationBlock.GetPosition() + shipOrientationBlock.WorldMatrix.Up * height + shipOrientationBlock.WorldMatrix.Left * range;
                    debugGPSOutput("Patrol2", vTarget);
                    sCmd += Vector3DToString(vTarget) + ":";
                    vTarget = shipOrientationBlock.GetPosition() + shipOrientationBlock.WorldMatrix.Up * height + shipOrientationBlock.WorldMatrix.Backward * range;
                    debugGPSOutput("Patrol3", vTarget);
                    sCmd += Vector3DToString(vTarget) + ":";

                    antSend("PATROL", sCmd);//        antSend(sCmd);
                    sStartupError += "PATROL:\n" + sCmd;
                }
                /*
                else if (args[0] == "namecameras")
                {
                    nameCameras(cameraForwardList, "Front");
                    nameCameras(cameraBackwardList, "Back");
                    nameCameras(cameraDownList, "Down");
                    nameCameras(cameraUpList, "Up");
                    nameCameras(cameraLeftList, "Left");
                    nameCameras(cameraRightList, "Right");
                }
                */
                else if (args[0] == "wcct" || args[0] == "")
                {
                    // do nothing special
                }
                else
                {
                    int iDMode;
                    if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                    {
                        setMode(iDMode);
                    }
                    else Echo("Unrecognized Command:" + varArgs[iArg]);
                }
            }
            return false; // keep processing in main
        }
        bool moduleProcessAntennaMessage(string sArgument)
        {
            return false;
        }

    }
}