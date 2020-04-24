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
        private StringBuilder strbSearchOrient = new StringBuilder();

        double SOElapsedMs = 0;

        /*
         * States
         * 0 Master init
         * 10 Wait for motion, then ->20 
         * 20 do the aiming at last contact (entrance)
         * when aimed, -> SEARCH_SHIFT
         */

        void doModeSearchOrient()
        {
            /*
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;
            IMySensorBlock sb2;
            */
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":SearchOrient", textPanelReport);
            Echo("Search Orient:current_state=" + current_state.ToString());
//            Echo(Vector3DToString(vExpectedAsteroidExit));
//            Echo(Vector3DToString(vLastAsteroidContact));
//            Echo(Vector3DToString(vLastAsteroidExit));
            double maxThrust = calculateMaxThrust(thrustForwardList);
            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

            Echo("Cargo=" + cargopcent.ToString() + "%");

            Echo("velocity=" + velocityShip.ToString("0.00"));
            Echo("SOElapsedMs=" + SOElapsedMs.ToString("0.00"));

            setMode(MODE_ATTENTION);
            /*
            if (current_state == 0)
            {
                StatusLog(DateTime.Now.ToString() + " StartSearchOrient", textLongStatus, true);
//                dtStartSearch = dtStartNav = DateTime.Now;
                ResetMotion();
                if (maxDeltaV < (fTargetMiningMps/2) || cargopcent > MiningCargopctlowwater)
//                if (cargopcent > 99)
                {
                    setMode(MODE_DOCKING);
                    return;
                }
//                sStartupError += "\nSO Start:" + Vector3DToString(vLastAsteroidContact);
                current_state = 10;
            }
            else if (current_state == 10)
            {
                ResetMotion();
                if (velocityShip < 0.2f) 
                {
//                    startNavWaypoint(vLastContact, true);
                    StatusLog(DateTime.Now.ToString() + " Aiming at " + Vector3DToString(vLastAsteroidContact), textLongStatus, true);
                    current_state = 20;
                }
                else Echo("Waiting for motion");
            }
            else if(current_state==20)
            {
                // NEED: Time out.
                bWantFast = true;
                if (GyroMain("forward", -vExpectedAsteroidExit, shipOrientationBlock))
                { // we are aimed roll to 'up'
                    current_state = 30;
                }
            }
            else if(current_state==30)
            {
                bWantFast = true;
                bool bAimed = GyroMain("up", AsteroidUpVector, shipOrientationBlock);
                if (bAimed)
                {
                    ResetMotion();
                    vLastAsteroidExit = shipOrientationBlock.GetPosition();
                    vExpectedAsteroidExit = -vExpectedAsteroidExit;
                    setMode(MODE_SEARCHSHIFT);
                }

            }
            */

        }
    }
}