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
        const string velocityFormat = "0.00";

        void ResetMotion(bool bNoDrills = false)  
        { 
	        powerDownThrusters(thrustAllList);
            gyrosOff();
	        if (shipOrientationBlock is IMyRemoteControl) ((IMyRemoteControl)shipOrientationBlock).SetAutoPilotEnabled(false);
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
            if(!bNoDrills) turnDrillsOff();

        }
        void MasterReset()
        {
            ResetMotion();

            MinerMasterReset();

            iniWicoCraftSave.ParseINI("");
            Serialize();
            bWantFast = true;
        }

        void ModuleSerialize(INIHolder iNIHolder)
        {
            MiningSerialize(iNIHolder);
            AsteroidSerialize();
            OreSerialize();
            ScansSerialize(iNIHolder);
            NavSerialize(iNIHolder);
        }
        void ModuleDeserialize(INIHolder iNIHolder)
        {
            MiningDeserialize(iNIHolder);
            AsteroidsDeserialize();
            OreDeserialize();
            ScansDeserialize(iNIHolder);
            NavDeserialize(iNIHolder);
        }
    }
}