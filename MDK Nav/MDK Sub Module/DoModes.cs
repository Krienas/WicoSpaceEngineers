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
        #region domodes 
        void doModes()
        {
            Echo("mode=" + iMode.ToString() + " state=" + current_state.ToString());
            /*            if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
                        {
                            Echo("DM:docked");
                            setMode(MODE_DOCKED);
                        }
                        */
            /*
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_DESCENT) doModeDescent();
*/

            // todo. on IDLE or reset, remove all nav commands
            Echo("Commands=" + wicoNavCommands.Count.ToString());
            if (wicoNavCommands.Count > 0)
                Echo("Next=" + wicoNavCommands[0].NAVTargetName);

            if (iMode == MODE_GOINGTARGET) { doModeGoTarget(); return; }
            if (iMode == MODE_STARTNAV) { doModeStartNav(); return; }
            if (iMode == MODE_NAVNEXTTARGET) { doModeNavNext(); return; } 
            if(iMode==MODE_SCANTEST) { doModeScanTest(); return; }
       }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
//            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
              StatusLog(moduleName + " Manual Control", textPanelReport);
        }
        #endregion

    }
}