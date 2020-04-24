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
        public class TravelMovement
        {
            public bool bCollisionWasSensor = false;

            double tmCameraElapsedMs = -1;
            double tmCameraWaitMs = 0.25;

            double tmScanElapsedMs = -1;

            public Vector3D vAvoid;

            // below are private
            IMyShipController tmShipController = null;
            double tmMaxSpeed = 100; // calculated max speed.

            List<IMyTerminalBlock> thrustTmBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmRightList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmDownList = new List<IMyTerminalBlock>();
            /// <summary>
            ///  GRID orientation to aim ship
            /// </summary>
            Vector3D vBestThrustOrientation;

            List<IMyTerminalBlock> thrustGravityUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustGravityDownList = new List<IMyTerminalBlock>();

            IMySensorBlock tmSB = null;

            bool btmApproach = false;
            bool btmPrecision = false;
            bool btmClose = false;
            double dtmFar = 100;
            double dtmApproach = 50;
            double dtmPrecision = 15;
            double dtmFarSpeed = 100;
            double dtmApproachSpeed = 100 * 0.5;
            double dtmPrecisionSpeed = 100 * 0.25;
            double dtmCloseSpeed = 5;

            float tmMaxSensorM = 50f;

            int dtmRayCastQuadrant = 0;
            // 0 = center. 1= TL, 2= TR, 3=BL, 4=BR


            // propulsion mode
            bool btmRotor = false;
            bool btmSled = false;
            bool btmWheels = false;
            bool btmHasGyros = false;
            // else it's gyros and thrusters

            bool dTMDebug = false;
            bool dTMUseCameraCollision = true;
            bool dTMUseSensorCollision = true;

            string sSection = "Travel Movement";

            Program _program;
            WicoControl _wicoControl;

            public TravelMovement(Program program, WicoControl wicoControl)
            {
                _program = program;
                _wicoControl = wicoControl;

                dTMDebug = _program._CustomDataIni.Get(sSection, "Debug").ToBoolean(dTMDebug);
                _program._CustomDataIni.Set(sSection, "Debug", dTMDebug);

                dTMUseCameraCollision = _program._CustomDataIni.Get(sSection, "UseCameraCollision").ToBoolean(dTMUseCameraCollision);
                _program._CustomDataIni.Set(sSection, "UseCameraCollision", dTMUseCameraCollision);

                dTMUseSensorCollision=_program._CustomDataIni.Get(sSection, "UseSensorCollision").ToBoolean(dTMUseSensorCollision);
                _program._CustomDataIni.Set(sSection, "UseSensorCollision", dTMUseSensorCollision);

                //                _program.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                //                _program.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
            }
            void LocalGridChangedHandler()
            {
            }
            /// <summary>
            /// reset so the next call to doTravelMovement will re-initialize.
            /// </summary>
            public void ResetTravelMovement()
            {
                // invalidates any previous tm calculations
                tmShipController = null;
                _program.wicoSensors.SensorsSleepAll(); // set sensors to lower power
                _program.wicoGyros.SetMinAngle();
                //                minAngleRad = 0.01f; // reset Gyro aim tolerance to default
                tmScanElapsedMs = 0;
                tmCameraElapsedMs = -1;
                _program.wicoWheels.WheelsPowerUp(0, 50);
            }

            /// <summary>
            /// initialize the travel movement module.
            /// </summary>
            /// <param name="vTargetLocation"></param>
            /// <param name="maxSpeed"></param>
            /// <param name="myShipController"></param>
            /// <param name="iThrustType"></param>
            public void InitDoTravelMovement(Vector3D vTargetLocation, double maxSpeed, IMyTerminalBlock myShipController, int iThrustType = WicoThrusters.thrustAll)
            {
                tmMaxSpeed = maxSpeed;
                if (tmMaxSpeed > _wicoControl.fMaxWorldMps)
                    tmMaxSpeed = _wicoControl.fMaxWorldMps;

                tmShipController = myShipController as IMyShipController;
                double velocityShip = tmShipController.GetShipSpeed();

                //TODO: add grav gen support
                if (_program.wicoWheels.HasSledWheels())
                {
                    btmSled = true;
                    //                if (_shipSpeedMax > 45) _shipSpeedMax = 45;
                    //                    sStartupError += "\nI am a SLED!";
                    _program.wicoWheels.PrepareSledTravel();
                }
                else btmSled = false;

                if (_program.wicoWheels.HasWheels())
                {
                    btmWheels = true;
                    // TODO: Turn brakes OFF
                    if (tmShipController is IMyShipController) tmShipController.HandBrake = false;
                }
                else btmWheels = false;

                if (_program.wicoGyros.GyrosAvailable() > 0)
                {
                    btmHasGyros = true;
                }
                else btmHasGyros = false;

                if (_program.wicoNavRotors.NavRotorCount() > 0)
                {
                    btmRotor = true;
                    //                if (_shipSpeedMax > 15) _shipSpeedMax = 15;
                }
                else btmRotor = false;

                Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;
                double distance = vVec.Length();

                _program.wicoThrusters.ThrustersCalculateOrientation(tmShipController, ref thrustTmForwardList, ref thrustTmBackwardList,
                ref thrustTmDownList, ref thrustTmUpList,
                ref thrustTmLeftList, ref thrustTmRightList);//, iThrustType);
                _program.wicoSensors.SensorsSleepAll();
                if (_program.wicoSensors.GetCount() > 0)
                {
                    tmSB = _program.wicoSensors.GetForwardSensor();
                    //                    tmSB = sensorsList[0];
                    if (btmRotor || btmSled) tmSB.DetectAsteroids = false;
                    else
                        tmSB.DetectAsteroids = true;
                    tmSB.DetectEnemy = true;
                    tmSB.DetectLargeShips = true;
                    tmSB.DetectSmallShips = true;
                    tmSB.DetectStations = true;
                    tmSB.DetectPlayers = false; // run them over!
                    tmMaxSensorM = tmSB.GetMaximum<float>("Front");
//                    _program.ErrorLog("Found Sensors");
                    if (!_program.wicoCameras.HasForwardCameras())//  cameraForwardList.Count < 1)
                    {
                        // sensors, but no cameras.
                        //                        if (!AllowBlindNav)
//                        _program.ErrorLog(" but no cameras");
                        tmMaxSpeed = tmMaxSensorM / 2;
                        //                        if (dTMUseCameraCollision) sStartupError += "\nNo Cameras for collision detection";
                    }
                }
                else
                {
                    // no sensors...
                    tmSB = null;
                    tmMaxSensorM = 0;
//                    _program.ErrorLog("Found NO Sensors");
                    if (!_program.wicoCameras.HasForwardCameras())//  cameraForwardList.Count < 1)
                    {
                        //                        if (dTMUseCameraCollision || dTMUseSensorCollision) sStartupError += "\nNo Sensor nor cameras\n for collision detection";
                        //                        if (!AllowBlindNav)
//                        _program.ErrorLog(" and NO Cameras");
                        tmMaxSpeed = 5;
                    }
                    else
                    {
                        //                        if (dTMUseSensorCollision) sStartupError += "\nNo Sensor for collision detection";
                    }
                }
                btmApproach = false; // we have reached approach range
                btmPrecision = false; // we have reached precision range
                btmClose = false; // we have reached close range

                double optimalV = tmMaxSpeed;

                // need to check other propulsion flags..
                if (!btmSled && !btmRotor) optimalV = CalculateOptimalSpeed(thrustTmBackwardList, distance);
                if (optimalV < tmMaxSpeed)
                    tmMaxSpeed = optimalV;

                if (dTMDebug) _program.ErrorLog("\nDistance=" + _program.niceDoubleMeters(distance) + " OptimalV=" + _program.niceDoubleMeters(optimalV));

                dtmFarSpeed = tmMaxSpeed;
                dtmApproachSpeed = tmMaxSpeed * 0.50;
                dtmPrecisionSpeed = tmMaxSpeed * 0.25;

                // minimum speeds.
                if (dtmApproachSpeed < 5) dtmApproachSpeed = 5;
                if (dtmPrecisionSpeed < 5) dtmPrecisionSpeed = 5;

                if (dtmPrecisionSpeed > dtmApproachSpeed) dtmApproachSpeed = dtmPrecisionSpeed;
                if (dtmPrecisionSpeed > dtmFarSpeed) dtmFarSpeed = dtmPrecisionSpeed;

                //            dtmPrecision =calculateStoppingDistance(thrustTmBackwardList, dtmPrecisionSpeed*1.1, 0);
                //            dtmApproach = calculateStoppingDistance(thrustTmBackwardList, dtmApproachSpeed*1.1, 0);

                if (!(btmWheels || btmRotor))
                {
                    dtmPrecision = _program.wicoThrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmPrecisionSpeed + (dtmApproachSpeed - dtmPrecisionSpeed) / 2, 0);
                    dtmApproach = _program.wicoThrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmApproachSpeed + (dtmFarSpeed - dtmApproachSpeed) / 2, 0);

                    dtmFar = _program.wicoThrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmFarSpeed, 0); // calculate maximum stopping distance at full speed                }

                }
                if (dTMDebug) _program.ErrorLog("FarSpeed==" + _program.niceDoubleMeters(dtmFarSpeed) + " ASpeed=" + _program.niceDoubleMeters(dtmApproachSpeed));
                if (dTMDebug) _program.ErrorLog("Far ==" + _program.niceDoubleMeters(dtmFar) + " A=" + _program.niceDoubleMeters(dtmApproach) + " P=" + _program.niceDoubleMeters(dtmPrecision));
                bCollisionWasSensor = false;
                tmCameraElapsedMs = -1; // no delay for next check  
                tmScanElapsedMs = 0;// do delay until check 
                dtmRayCastQuadrant = 0;

                _program.wicoGyros.SetMinAngle(0.01f);// minAngleRad = 0.01f; // reset Gyro aim tolerance to default

                Vector3D vNG = tmShipController.GetNaturalGravity();

                if (vNG.Length() > 0) // we have gravity
                {
                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    _program.wicoThrusters.GetBestThrusters(vNGN,
                        thrustTmForwardList, thrustTmBackwardList,
                        thrustTmDownList, thrustTmUpList,
                        thrustTmLeftList, thrustTmRightList,
                        out thrustGravityUpList, out thrustGravityDownList
                        );
                }
                else
                {
                    // TODO: Could also choose 'best thrust' instead of assuming 'forward'
                    Matrix or1;
                    tmShipController.Orientation.GetMatrix(out or1);
                    vBestThrustOrientation = or1.Forward;
                }
                if(dTMDebug) _program.ErrorLog(" TM Init: max=" + tmMaxSpeed.ToString("0") + ":far=" + dtmFar.ToString("0"));
            }

            /// <summary>
            /// Does travel movement with collision detection and avoidance. On arrival, changes state to arrivalState. If collision, changes to colDetectState
            /// </summary>
            /// <param name="vTargetLocation">Location of target</param>
            /// <param name="arrivalDistance">minimum distance for 'arrival'</param>
            /// <param name="arrivalState">state to use when 'arrived'</param>
            /// <param name="colDetectState">state to use when 'collision'</param>
            /// <param name="bAsteroidTarget">if True, target location is in/near an asteroid.  don't collision detect with it</param>
            public void doTravelMovement(Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState, 
                double maxSpeed, int iThrustType = WicoThrusters.thrustAll, bool bAsteroidTarget = false)
            {
                bool bArrived = false;
                if (dTMDebug)
                {
                    _program.Echo("dTM:" + _wicoControl.IState + "->" + arrivalState + "-C>" + colDetectState + " A:" + arrivalDistance);
                    //                    _program.Echo("dTM:" + current_state + "->" + arrivalState + "-C>" + colDetectState + " A:" + arrivalDistance);
                    _program.Echo("W=" + btmWheels.ToString() + " S=" + btmSled.ToString() + " R=" + btmRotor.ToString());
                }
                //		Vector3D vTargetLocation = vHome;// shipOrientationBlock.GetPosition();
                //    shipOrientationBlock.CubeGrid.
                if (tmShipController == null)
                {
                    // first (for THIS target) time init
                    InitDoTravelMovement(vTargetLocation, maxSpeed, _program.wicoBlockMaster.GetMainController(), iThrustType);
                }
                Vector3D vg= tmShipController.GetNaturalGravity();
                double dGravity = vg.Length();
                double velocityShip = tmShipController.GetShipSpeed();

                if (tmCameraElapsedMs >= 0) tmCameraElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (tmScanElapsedMs >= 0) tmScanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;

                double distance = vVec.Length();

                // TODO: Adjust targetlocation for gravity and min altitude.
                //                if (NAVGravityMinElevation > 0 && dGravity > 0)
                if (_program.wicoBlockMaster.DesiredMinTravelElevation > 0 && dGravity > 0)
                {
                    // Need some trig here to calculate angle/distance to ground and target above ground
                    // Have: our altitude. Angle to target. Distance to target.
                    // iff target is 'below' us, then raise effective target to maintain min alt
                    // iff target is 'above' us..  then raise up to it..


                    // cheat for now.
                    if (distance < (arrivalDistance + _program.wicoBlockMaster.DesiredMinTravelElevation))
                        bArrived = true;

                }

                if (dTMDebug)
                {
                    _program.Echo("dTM:distance=" + _program.niceDoubleMeters(distance) + " (" + arrivalDistance.ToString() + ")");
                    _program.Echo("dTM:velocity=" + velocityShip.ToString("0.00"));
                    _program.Echo("dTM:tmMaxSpeed=" + tmMaxSpeed.ToString("0.00"));
                }
                if (distance < arrivalDistance)
                    bArrived = true;

                if (bArrived)
                {
                    _program.ResetMotion(); // start the stopping
                    _wicoControl.SetState(arrivalState);// current_state = arrivalState; // we have arrived
                    ResetTravelMovement(); // reset our brain so we re-calculate for the next time we're called
                                           // TODO: Turn brakes ON (if not done in ResetMotion())
                    _wicoControl.WantFast();// bWantFast = true; // process this quickly
                    return;
                }
                //                debugGPSOutput("TargetLocation", vTargetLocation);

                List<IMySensorBlock> aSensors = null;

                double stoppingDistance = 0;
                if (!(btmWheels || btmRotor))
                {
                    //                if (dTMDebug) Echo("CalcStopD()");

                    var myMass = tmShipController.CalculateShipMass();
                    
                    stoppingDistance = _program.wicoThrusters.calculateStoppingDistance(myMass.PhysicalMass,thrustTmBackwardList, velocityShip, 0);
                }
                // TODO: calculate stopping D for wheels

                //            Echo("dtmStoppingD=" + niceDoubleMeters(stoppingDistance));

                if (_program.wicoSensors.GetCount() > 0)
                {
                    //                    float fScanDist = Math.Min(1f, (float)stoppingDistance * 1.5f);
                    float fScanDist = Math.Min(tmMaxSensorM, (float)stoppingDistance * 1.5f);
                    if (!dTMUseCameraCollision) _program.wicoSensors.SensorSetToShip(tmSB, 1, 1, 1, 1, fScanDist, 0);
                    else _program.wicoSensors.SensorSetToShip(tmSB, 0, 0, 0, 0, fScanDist, 0);

                }
                //            else Echo("No Sensor for Travel movement");
                bool bAimed = false;

                Vector3D grav = tmShipController.GetNaturalGravity();
                if (
                    btmSled
                    || btmRotor
                    )
                {

                    double yawangle = -999;
                    yawangle = _program.wicoGyros.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    if (btmSled)
                    {
                        _program.Echo("Sled");
                        _program.wicoGyros.DoRotate(yawangle, "Yaw");
                    }
                    else if (btmRotor)
                    {
                        _program.Echo("Rotor");
                        _program.wicoNavRotors.DoRotorRotate(yawangle);
                    }
                    bAimed = Math.Abs(yawangle) < .05;
                }
                else if (btmWheels && btmHasGyros)
                {
                    _program.Echo("Wheels W/ Gyro");
                    double yawangle = -999;
                    yawangle = _program.wicoGyros.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    bAimed = Math.Abs(yawangle) < .05;
                    if (!bAimed)
                    {
                        _program.wicoWheels.WheelsPowerUp(0, 5);
                        //                    WheelsSetFriction(0);
                        _program.wicoGyros.DoRotate(yawangle, "Yaw");
                    }
                    else _program.wicoWheels.WheelsSetFriction(50);
                }
                else if (btmWheels) // & ! btmHasGyro)
                {
                    _program.Echo("Wheels with no gyro...");
                    double yawangle = _program.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    bAimed = Math.Abs(yawangle) < .05;
                    if (!bAimed)
                    {
                        // TODO: rotate with wheels..
                    }
                    else _program.wicoWheels.WheelsSetFriction(50);
                }
                else
                {
                    if (grav.Length() > 0)
                    { // in gravity. try to stay aligned to gravity, but change yaw to aim at location.
                        if (_program.wicoGyros.AlignGyros(vBestThrustOrientation, grav))
//                            bool bGravAligned = GyroMain("", grav, tmShipController);
                        //                    if (bGravAligned)
                        {
                            double yawangle = _program.wicoGyros.CalculateYaw(vTargetLocation, tmShipController);
                            _program.wicoGyros.DoRotate(yawangle, "Yaw");
                            bAimed = Math.Abs(yawangle) < .05;
                        }
                    }
                    else
                    {
                        Matrix or1;
                        tmShipController.Orientation.GetMatrix(out or1);
                        bAimed=_program.wicoGyros.AlignGyros(or1.Forward, vVec);// bAimed = GyroMain("forward", vVec, tmShipController);
                    }
                }
                tmShipController.DampenersOverride = true;

                if ((distance - stoppingDistance) < arrivalDistance)
                { // we are within stopping distance, so start slowing
                    _program.wicoGyros.SetMinAngle(0.0005f);// minAngleRad = 0.005f;// aim tighter (next time)
                                                               //                    StatusLog("\"Arriving at target.  Slowing", textPanelReport);
                    _program.Echo("Waiting for stop");
                    if (!bAimed) _wicoControl.WantFast();// bWantFast = true;
                    else _wicoControl.WantMedium();
                    _program.ResetMotion();
                    return;
                }
                if (bAimed)
                {
                    _wicoControl.WantMedium(); // bWantMedium = true;
                    // we are aimed at location
                    _program.Echo("Aimed");
                    _program.wicoGyros.gyrosOff();
                    if (btmWheels) _program.wicoWheels.WheelsSetFriction(50);
                    if (
                        dTMUseSensorCollision
//                        && (tmScanElapsedMs > dSensorSettleWaitMS || tmScanElapsedMs < 0)
                        )
                    {
                        tmScanElapsedMs = 0;
                        aSensors = _program.wicoSensors.SensorsGetActive();

                        if (aSensors.Count > 0)
                        {

                            var entities = new List<MyDetectedEntityInfo>();
                            string s = "";
                            for (int i1 = 0; i1 < aSensors.Count; i1++) // we only use one sensor
                            {
                                aSensors[i1].DetectedEntities(entities);
                                int j1 = 0;
                                bool bValidCollision = false;
                                if (entities.Count > 0) bValidCollision = true;

                                for (; j1 < entities.Count; j1++)
                                {

                                    s = "\nSensor TRIGGER!";
                                    s += "\nName: " + entities[j1].Name;
                                    s += "\nType: " + entities[j1].Type;
                                    s += "\nRelationship: " + entities[j1].Relationship;
                                    s += "\n";
                                    if (dTMDebug)
                                    {
                                        _program.Echo(s);
                                        //                                        StatusLog(s, textLongStatus);
                                    }
                                    if (entities[j1].Type == MyDetectedEntityType.Planet)
                                    {
                                        bValidCollision = false;
                                    }
                                    if (entities[j1].Type == MyDetectedEntityType.LargeGrid
                                        || entities[j1].Type == MyDetectedEntityType.SmallGrid
                                        )
                                    {
                                        if (entities[j1].BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                        {
                                            if (dTMDebug)
                                                _program.Echo("Ignoring collision because we want to be INSIDE");
                                            // if the target is inside the BB of the target, ignore the collision
                                            bValidCollision = false;
                                        }
                                    }
                                    if (bValidCollision) break;

                                }

                                if (bValidCollision)
                                {
                                    // something in way.
                                    // save what we detected
                                    lastDetectedInfo = entities[j1];
                                    ResetTravelMovement();
                                    _wicoControl.SetState(colDetectState);// current_state = colDetectState; // set the collision detetected state
                                    bCollisionWasSensor = true;
                                    _wicoControl.WantFast();// bWantFast = true; // process next state quickly
                                    _program.ResetMotion(); // start stopping
                                    return;
                                }
                            }
                        }
                        else lastDetectedInfo = new MyDetectedEntityInfo(); // since we found nothing, clear it.
                    }
                    double scanDistance = stoppingDistance * 2;
                    //               double scanDistance = stoppingDistance * 1.05;
                    //                if (bAsteroidTarget) scanDistance *= 2;
                    //                if (btmRotor || btmSled)
                    {
                        if (scanDistance < 100)
                        {
                            if (distance < 500)
                                scanDistance = distance;
                            else scanDistance = 500;
                        }
                        scanDistance = Math.Min(distance, scanDistance);
                    }

                    //               if (dTMDebug)
                    if (dTMUseCameraCollision)
                    {
                        //                    Echo("Scanning distance=" + niceDoubleMeters(scanDistance));
                    }
                    if (
                        dTMUseCameraCollision
                        && (tmCameraElapsedMs > tmCameraWaitMs || tmCameraElapsedMs < 0) // it is time to scan..
                        && distance > tmMaxSensorM // if we are in sensor range, we don't need to scan with cameras
                                                   //                    && !bAsteroidTarget
                        )
                    {

                        OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(tmShipController);
                        Vector3D[] points = new Vector3D[4];
                        orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, points); // front output order is BL, BR, TL, TR

                        // assumes moving forward...
                        // May 29, 2018 do a BB forward scan instead of just center..
                        bool bDidScan = false;
                        Vector3D vTarget;
                        switch (dtmRayCastQuadrant)
                        {
                            case 0:
                                if (_program.wicoCameras.CameraForwardScan(scanDistance))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 1:
                                vTarget = points[2] + tmShipController.WorldMatrix.Forward * distance;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 2:
                                vTarget = points[3] + tmShipController.WorldMatrix.Forward * distance;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 3:
                                vTarget = points[0] + tmShipController.WorldMatrix.Forward * distance;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 4:
                                vTarget = points[1] + tmShipController.WorldMatrix.Forward * distance;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 5:
                                // check center again.  always full length
                                if (_program.wicoCameras.CameraForwardScan(scanDistance))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 6:
                                vTarget = points[2] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 7:
                                vTarget = points[3] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 8:
                                vTarget = points[0] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 9:
                                vTarget = points[1] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_program.wicoCameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                        }

                        if (bDidScan)
                        {
                            dtmRayCastQuadrant++;
                            if (dtmRayCastQuadrant > 9) dtmRayCastQuadrant = 0;

                            tmCameraElapsedMs = 0;
                            lastDetectedInfo = _program.wicoCameras.lastDetectedInfo;
                            if (!lastDetectedInfo.IsEmpty())
                            {
                                bool bValidCollision = true;
                                // assume it MIGHT be asteroid and check
                                //                            if (bAsteroidTarget)
                                {
                                    if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        if (lastDetectedInfo.BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                        { // if the target is inside the BB of the target, ignore the collision
                                            bValidCollision = false;
                                            // check to see if we are close enough to surface of asteroid
                                            double astDistance = ((Vector3D)lastDetectedInfo.HitPosition - tmShipController.GetPosition()).Length();
                                            if ((astDistance - stoppingDistance) < arrivalDistance)
                                            {
                                                _program.ResetMotion();
                                                _wicoControl.SetState(arrivalState);// current_state = arrivalState;
                                                ResetTravelMovement();
                                                // don't need 'fast'...
                                                return;
                                            }
                                        }
                                    }
                                    else if (lastDetectedInfo.Type == MyDetectedEntityType.Planet)
                                    {
                                        // ignore
                                        bValidCollision = false;
                                    }

                                    else
                                    {
                                    }
                                }
                                if (dTMDebug)
                                {
                                    //                            Echo(s);
                                    _program.Echo("raycast hit:" + lastDetectedInfo.Type.ToString());
//                                    StatusLog("Camera Trigger collision", textPanelReport);
                                }
                                if (bValidCollision)
                                {
                                    //                                sInitResults += "Camera collision: " + scanDistance + "\n" + lastDetectedInfo.Name + ":" + lastDetectedInfo.Type + "\n";
                                    // something in way.
                                    ResetTravelMovement(); // reset our brain for next call
                                    _wicoControl.SetState(colDetectState);// current_state = colDetectState; // set the detetected state
                                    bCollisionWasSensor = false;
                                    _wicoControl.WantFast();// bWantFast = true; // process next state quickly
                                    _program.ResetMotion(); // start stopping
                                    return;
                                }
                            }
                            else
                            {
                                if (dTMDebug)
                                {
                                    //                            Echo(s);
                                    //                                   StatusLog("Camera Scan Clear", textPanelReport);
                                }
                            }
                        }
                        else
                        {
                            if (dTMDebug)
                            {
                                //                            Echo(s);
                                //                               StatusLog("No Scan Available", textPanelReport);
                            }
                        }
                    }
                    else _program.Echo("Raycast delay");

                    if (dTMDebug)
                        _program.Echo("dtmFar=" + _program.niceDoubleMeters(dtmFar));
                    if (dTMDebug)
                        _program.Echo("dtmApproach=" + _program.niceDoubleMeters(dtmApproach));
                    if (dTMDebug)
                        _program.Echo("dtmPrecision=" + _program.niceDoubleMeters(dtmPrecision));

                    if (distance > dtmFar && !btmApproach)
                    {
                        // we are 'far' from target location.  use fastest movement
                        //                    if(dTMDebug)
                        _program.Echo("dtmFar. Target Vel=" + dtmFarSpeed.ToString("N0"));
                        //                        StatusLog("\"Far\" from target\n Target Speed=" + dtmFarSpeed.ToString("N0") + "m/s", textPanelReport);

                        TmDoForward(dtmFarSpeed, 100f);
                    }
                    else if (distance > dtmApproach && !btmPrecision)
                    {
                        // we are on 'approach' to target location.  use a good speed
                        //                    if(dTMDebug)
                        _program.Echo("Approach. Target Vel=" + dtmApproachSpeed.ToString("N0"));

                        //                        StatusLog("\"Approach\" distance from target\n Target Speed=" + dtmApproachSpeed.ToString("N0") + "m/s", textPanelReport);
                        btmApproach = true;
                        TmDoForward(dtmApproachSpeed, 100f);
                    }
                    else if (distance > dtmPrecision && !btmClose)
                    {
                        // we are getting nearto our target.  use a slower speed
                        //                    if(dTMDebug)
                        _program.Echo("Precision. Target Vel=" + dtmPrecisionSpeed.ToString("N0"));
                        //                        StatusLog("\"Precision\" distance from target\n Target Speed=" + dtmPrecisionSpeed.ToString("N0") + "m/s", textPanelReport);
                        if (!btmPrecision) _program.wicoGyros.SetMinAngle(0.005f);// minAngleRad = 0.005f;// aim tighter (next time)
                        btmPrecision = true;
                        TmDoForward(dtmPrecisionSpeed, 100f);
                    }
                    else
                    {
                        // we are very close to our target. use a very small speed
                        //                    if(dTMDebug)
                        _program.Echo("Close. Target Speed=" + dtmCloseSpeed.ToString("N0") + "m/s");
                        //                        StatusLog("\"Close\" distance from target\n Target Speed=" + dtmCloseSpeed.ToString("N0") + "m/s", textPanelReport);
                        if (!btmClose) _program.wicoGyros.SetMinAngle(0.005f);//minAngleRad = 0.005f;// aim tighter (next time)
                        btmClose = true;
                        TmDoForward(dtmCloseSpeed, 100f);
                    }
                }
                else
                {
                    //                    StatusLog("Aiming at target", textPanelReport);
                    if (dTMDebug) _program.Echo("Aiming");
                    _wicoControl.WantFast();// bWantFast = true;
                    tmShipController.DampenersOverride = true;
                    if (velocityShip < 5)
                    {
                        // we are probably doing precision maneuvers.  Turn on all thrusters to avoid floating past target
                        _program.wicoThrusters.powerDownThrusters();
                    }
                    else
                    {
                        _program.wicoThrusters.powerDownThrusters(thrustTmBackwardList, WicoThrusters.thrustAll, true); // coast
                    }
                    //		sleepAllSensors();
                }

            }
            /// <summary>
            /// returns the optimal max speed based on available braking thrust and ship mass
            /// </summary>
            /// <param name="thrustList">Thrusters to use</param>
            /// <param name="distance">current distance to target location</param>
            /// <returns>optimal max speed (may be game max speed)</returns>
            public double CalculateOptimalSpeed(List<IMyTerminalBlock> thrustList, double distance)
            {
                if(thrustList==null)
                {
                    _program.ErrorLog("COS: NULL list");
                    return 0;
                }
                //
                //            Echo("#thrusters=" + thrustList.Count.ToString());
                if (thrustList.Count < 1) return _wicoControl.fMaxWorldMps;

                IMyShipController myShip = tmShipController;
                if (myShip == null) myShip = _program.wicoBlockMaster.GetMainController();

                MyShipMass myMass;
                myMass = myShip.CalculateShipMass();
                double maxThrust = _program.wicoThrusters.calculateMaxThrust(thrustList);
                double maxDeltaV = maxThrust / myMass.PhysicalMass;
                // Magic..
                double optimalV, secondstozero, stoppingM;

                if (dTMDebug) _program.ErrorLog("distance="+distance.ToString("N1")+ " MaxdeltaV="+maxDeltaV.ToString("F1"));

                //                optimalV = ((distance * .75) / 2) / (maxDeltaV); // determined by experimentation and black magic
                //            Echo("COS");
                optimalV = tmMaxSpeed;
                if (dTMDebug) _program.ErrorLog("initial optimalV=" + _program.niceDoubleMeters(optimalV));

                do
                {
                    //                Echo("COS:DO");
                    secondstozero = optimalV / maxDeltaV;
                    stoppingM = optimalV / 2 * secondstozero;
                    if (dTMDebug) _program.ErrorLog("stopm=" + stoppingM.ToString("F1") + " distance=" + distance.ToString("N1"));
                    if (stoppingM > distance)
                    {
                        optimalV *= 0.85;
                    }
                    if (dTMDebug) _program.ErrorLog("looping optimalV=" + _program.niceDoubleMeters(optimalV));

                }
                while (stoppingM > distance);
                //            Echo("COS:X");
                return optimalV;
            }

            public void calcCollisionAvoid(Vector3D vTargetLocation, MyDetectedEntityInfo lastDetectedInfo, out Vector3D vAvoid)
            {
                if (tmCameraElapsedMs >= 0) tmCameraElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (tmScanElapsedMs >= 0) tmScanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                //            Echo("Collsion Detected");
                Vector3D vHit;
                if (lastDetectedInfo.HitPosition.HasValue)
                {
                    //                StatusLog("Has hitposition", gpsPanel);
                    vHit = (Vector3D)lastDetectedInfo.HitPosition;
                }
                else
                {
                    //                StatusLog("NO hitposition", gpsPanel);
                    vHit = _program.wicoBlockMaster.GetMainController().GetPosition();
                }

                Vector3D vCenter = lastDetectedInfo.Position;
                //	Vector3D vTargetLocation = vHome;
                //vTargetLocation;
                //            debugGPSOutput("TargetLocation", vTargetLocation);
                //            debugGPSOutput("HitPosition", vHit);
                //            debugGPSOutput("CCenter", vCenter);

                // need to check if vector is straight through the object.  then choose 90% vector or something
                Vector3D vVec = (vCenter - vHit);
                vVec.Normalize();
                //           double ang;

                Vector3D vMinBound = lastDetectedInfo.BoundingBox.Min;
                //            debugGPSOutput("vMinBound", vMinBound);
                Vector3D vMaxBound = lastDetectedInfo.BoundingBox.Max;
                //            debugGPSOutput("vMaxBound", vMaxBound);

                double radius = (vCenter - vMinBound).Length();
                //            Echo("Radius=" + radius.ToString("0.00"));

                double modRadius = radius + _program.wicoBlockMaster.WidthInMeters() * 5;

                // the OLD way.

                //            vAvoid = vCenter - vVec * (radius + shipDim.WidthInMeters() * 5);
                //	 Vector3D shipOrientationBlock.GetPosition() - vAvoid;

                Vector3D cross;

                cross = Vector3D.Cross(vTargetLocation, vHit);
                cross.Normalize();
                cross = vHit + cross * modRadius;
                //            debugGPSOutput("crosshit", cross);

                vAvoid = cross;

                //            debugGPSOutput("vAvoid", vAvoid);
            }


            // pathfinding routines.  Try to escape from inside an asteroid.

            bool bScanLeft = true;
            bool bScanRight = true;
            bool bScanUp = true;
            bool bScanDown = true;
            bool bScanBackward = true;
            bool bScanForward = true;

            MyDetectedEntityInfo lastDetectedInfo;

            MyDetectedEntityInfo leftDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo rightDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo upDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo downDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo backwardDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo forwardDetectedInfo = new MyDetectedEntityInfo();

            //        bool bEscapeGrid = false;

            QuadrantCameraScanner ScanEscapeFrontScanner;
            QuadrantCameraScanner ScanEscapeBackScanner;
            QuadrantCameraScanner ScanEscapeLeftScanner;
            QuadrantCameraScanner ScanEscapeRightScanner;
            QuadrantCameraScanner ScanEscapeTopScanner;
            QuadrantCameraScanner ScanEscapeBottomScanner;

            public MyDetectedEntityInfo LastDetectedInfo
            {
                get
                {
                    return lastDetectedInfo;
                }

                set
                {
                    lastDetectedInfo = value;
                }
            }

            /// <summary>
            /// Initialize the escape scanning (mini-pathfinding)
            /// Call once to setup
            /// </summary>
            public void initEscapeScan(bool bWantBack = false, bool bWantForward = true)
            {
                if (tmCameraElapsedMs >= 0) tmCameraElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (tmScanElapsedMs >= 0) tmScanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                bScanLeft = true;
                bScanRight = true;
                bScanUp = true;
                bScanDown = true;
                bScanBackward = bWantBack;// don't rescan where we just came from..
                bScanForward = bWantForward;

                leftDetectedInfo = new MyDetectedEntityInfo();
                rightDetectedInfo = new MyDetectedEntityInfo();
                upDetectedInfo = new MyDetectedEntityInfo();
                downDetectedInfo = new MyDetectedEntityInfo();
                backwardDetectedInfo = new MyDetectedEntityInfo();
                forwardDetectedInfo = new MyDetectedEntityInfo();

                //            bEscapeGrid = false;
                if (lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                    || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                    )
                {
                    //               bEscapeGrid = true;
                }
                
                // don't assume all drones have all cameras..
                if (_program.wicoCameras.HasLeftCameras()) bScanLeft = false;
                if (_program.wicoCameras.HasRightCameras()) bScanRight = false;
                if (_program.wicoCameras.HasUpCameras()) bScanUp = false;
                if (_program.wicoCameras.HasDownCameras()) bScanDown = false;
                if (_program.wicoCameras.HasForwardCameras()) bScanForward = false;
                if (_program.wicoCameras.HasBackCameras()) bScanBackward = false;
                ScanEscapeFrontScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetForwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeBackScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetBackwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeLeftScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetLeftCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeRightScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetRightCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeTopScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetUpCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeBottomScanner = new QuadrantCameraScanner(_program, _program.wicoCameras.GetDownwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);

            }
            /// <summary>
            /// Perform the pathfinding. Call this until it returns true
            /// </summary>
            /// <returns>true if vAvoid now contains the location to go to to (try to) escape</returns>
            public bool scanEscape()
            {
                if (tmCameraElapsedMs >= 0) tmCameraElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (tmScanElapsedMs >= 0) tmScanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                MatrixD worldtb = tmShipController.WorldMatrix;
                Vector3D vVec = worldtb.Forward;
                _program.Echo("ScanEscape()");
                if (bScanLeft)
                {
                    //                sStartupError+="\nLeft";
                    if (_program.wicoCameras.CameraLeftScan(200))//   doCameraScan(_program.wicoCameras.cameraLeftList, 200))
                    {
                        bScanLeft = false;
                        leftDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Left;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanLeft = ScanEscapeLeftScanner.DoScans();
                    if (ScanEscapeLeftScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        leftDetectedInfo = lastDetectedInfo;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeLeftScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanRight)
                {
                    //                sStartupError += "\nRight";
                    if (_program.wicoCameras.CameraRightScan(200))// if (doCameraScan(cameraRightList, 200))
                    {
                        bScanRight = false;
                        rightDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Right;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanRight = ScanEscapeRightScanner.DoScans();
                    if (ScanEscapeRightScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        rightDetectedInfo = lastDetectedInfo;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeRightScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanUp)
                {
                    //                sStartupError += "\nUp";
                    if (_program.wicoCameras.CameraUpScan(200))// if (doCameraScan(cameraUpList, 200))
                    {
                        //                  upDetectedInfo = lastDetectedInfo;
                        bScanUp = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
//                            sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Up;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanUp = ScanEscapeTopScanner.DoScans();
                    if (ScanEscapeTopScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        upDetectedInfo = lastDetectedInfo;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeTopScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanDown)
                {
                    //                sStartupError += "\nDown";
                    if (_program.wicoCameras.CameraDownScan(200))// if (doCameraScan(cameraDownList, 200))
                    {
                        //                    sStartupError += "\n Straight Camera HIT!";
                        downDetectedInfo = lastDetectedInfo;
                        bScanDown = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            vVec = worldtb.Down;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanDown = ScanEscapeBottomScanner.DoScans();
                    if (ScanEscapeBottomScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        downDetectedInfo = lastDetectedInfo;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeBottomScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanBackward)
                {
                    //                sStartupError += "\nBack";
                    if (_program.wicoCameras.CameraBackwardScan(200))// if (doCameraScan(cameraBackwardList, 200))
                    {
                        //                    sStartupError += "\n Straight Camera HIT!";
                        backwardDetectedInfo = lastDetectedInfo;
                        bScanBackward = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            vVec = worldtb.Backward;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanBackward = ScanEscapeBackScanner.DoScans();
                    if (ScanEscapeBackScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        backwardDetectedInfo = lastDetectedInfo;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeBackScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanForward)
                {
                    //                sStartupError += "\nForward";
                    if (_program.wicoCameras.CameraForwardScan(200))// if (doCameraScan(cameraForwardList, 200))
                    {
                        bScanForward = false;
                        forwardDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Forward;
                            vVec.Normalize();
                            vAvoid = tmShipController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanForward = ScanEscapeFrontScanner.DoScans();
                    if (ScanEscapeFrontScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        forwardDetectedInfo = lastDetectedInfo;
                        vAvoid = ScanEscapeFrontScanner.vEscapeTarget * 200;
                        vAvoid = tmShipController.GetPosition() + ScanEscapeFrontScanner.vEscapeTarget * 200;
                        return true;
                    }
                }

                if (bScanForward || bScanBackward || bScanUp || bScanDown || bScanLeft || bScanRight)
                {
                    _program.Echo("More scans");
                    return false; // still more scans to go
                }

                // nothing was 'clear'.  find longest vector and try to go that direction
                _program.Echo("Scans done. Choose longest");
                MyDetectedEntityInfo furthest = backwardDetectedInfo;
                Vector3D currentpos = tmShipController.GetPosition();
                vVec = worldtb.Backward;
                if (furthest.HitPosition == null || leftDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)leftDetectedInfo.HitPosition))
                {
                    vVec = worldtb.Left;
                    furthest = leftDetectedInfo;
                }
                if (furthest.HitPosition == null || rightDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)rightDetectedInfo.HitPosition))
                {
                    vVec = worldtb.Right;
                    furthest = rightDetectedInfo;
                }
                if (furthest.HitPosition == null || upDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)upDetectedInfo.HitPosition))
                {
                    vVec = worldtb.Up;
                    furthest = upDetectedInfo;
                }
                if (furthest.HitPosition == null || downDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)downDetectedInfo.HitPosition))
                {
                    vVec = worldtb.Down;
                    furthest = downDetectedInfo;
                }
                if (furthest.HitPosition == null || forwardDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)forwardDetectedInfo.HitPosition))
                {
                    vVec = worldtb.Forward;
                    furthest = forwardDetectedInfo;
                }
                if (furthest.HitPosition == null) return false;

                double distance = Vector3D.Distance(currentpos, (Vector3D)furthest.HitPosition);
                _program.Echo("Distance=" + _program.niceDoubleMeters(distance));
                vVec.Normalize();
                vAvoid = tmShipController.GetPosition() + vVec * distance / 2;
                /*
                            if (distance<15)
                            {
                                if (doCameraScan(cameraForwardList, 20,35,35))
                                {
                                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                                    if (distance < vVec.Length())
                                    {
                                    Echo("35,35");
                                        distance = vVec.Length();
                                        vVec.Normalize();
                                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                                    }

                                }
                                if (doCameraScan(cameraForwardList, 20,-35,-35))
                                {
                                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                                    if (distance < vVec.Length())
                                    {
                                    Echo("-35,-35");
                                        distance = vVec.Length();
                                        vVec.Normalize();
                                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                                    }

                                }
                                if (doCameraScan(cameraForwardList, 20,35,-35))
                                {
                                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                                    if (distance < vVec.Length())
                                    {
                                    Echo("35,-35");
                                        distance = vVec.Length();
                                        vVec.Normalize();
                                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                                    }

                                }
                                if (doCameraScan(cameraForwardList, 20,-35,35))
                                {
                                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                                    if (distance < vVec.Length())
                                    {
                                    Echo("-35,35");
                                        distance = vVec.Length();
                                        vVec.Normalize();
                                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                                    }
                                }

                            }
                */
                //            if (distance > 15)
                if (distance > 4)
                {
                    return true;
                }

                _program.Echo("not FAR enough: ERROR!");
                return false;
            }

            void TmPowerForward(float fPower)
            {
                if (btmRotor)
                {
                    /*
                    // need to ramp up/down rotor power or they will flip small vehicles and spin a lot
                    float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");
                    float currentVelocity = rotorNavLeftList[0].GetValueFloat("Velocity");
                    float cPower = (currentVelocity / maxVelocity * 100);
                    cPower = Math.Abs(cPower);
                    if (fPower > (cPower + 5f))
                        fPower = cPower + 5;
                    if (fPower < (cPower - 5))
                        fPower = cPower - 5;

                    if (fPower < 0f) fPower = 0f;
                    if (fPower > 100f) fPower = 100f;
                    */
                    _program.wicoNavRotors.powerUpRotors(fPower);
                }
                else
                    _program.wicoThrusters.powerUpThrusters(thrustTmForwardList, fPower);
            }

            void TmDoForward(double maxSpeed, float maxThrust)
            {
                double velocityShip = tmShipController.GetShipSpeed();
                if (btmWheels)
                {
                    if (velocityShip < 1)
                    {
                        // full power, captain!
                        _program.wicoWheels.WheelsPowerUp(maxThrust);
                    }
                    // if we need to go much faster or we are FAR and not near max speed
                    else if (velocityShip < maxSpeed * .75 || (!btmApproach && velocityShip < maxSpeed * .98))
                    {
                        float delta = (float)maxSpeed / _wicoControl.fMaxWorldMps * maxThrust;
                        _program.wicoWheels.WheelsPowerUp(maxThrust);
                    }
                    else if (velocityShip < maxSpeed * .85)
                        _program.wicoWheels.WheelsPowerUp(maxThrust);
                    else if (velocityShip <= maxSpeed * .98)
                    {
                        _program.wicoWheels.WheelsPowerUp(maxThrust);
                    }
                    else if (velocityShip >= maxSpeed * 1.02)
                    {
                        // TOO FAST
                        _program.wicoWheels.WheelsPowerUp(0);
                    }
                    else // sweet spot
                    {
                        _program.wicoWheels.WheelsPowerUp(1);
                    }

                }
                else if (!btmRotor)
                {
                    if (velocityShip < 1)
                    {
                        // full power, captain!
                        _program.wicoThrusters.powerUpThrusters(thrustTmForwardList, maxThrust);
                    }
                    // if we need to go much faster or we are FAR and not near max speed
                    else if (velocityShip < maxSpeed * .75 || (!btmApproach && velocityShip < maxSpeed * .98))
                    {
                        float delta = (float)maxSpeed / _wicoControl.fMaxWorldMps * maxThrust;
                        _program.wicoThrusters.powerUpThrusters(thrustTmForwardList, delta);
                    }
                    else if (velocityShip < maxSpeed * .85)
                        _program.wicoThrusters.powerUpThrusters(thrustTmForwardList, 15f);
                    else if (velocityShip <= maxSpeed * .98)
                    {
                        _program.wicoThrusters.powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= maxSpeed * 1.02)
                    {
                        _program.wicoThrusters.powerDownThrusters();
                    }
                    else // sweet spot
                    {
                        _program.wicoThrusters.powerDownThrusters(); // turns ON all thrusters
                                                                                     // turns off the 'backward' thrusters... so we don't slow down
                        _program.wicoThrusters.powerDownThrusters(thrustTmBackwardList, WicoThrusters.thrustAll, true);
                        //                 tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
                }
                else
                { // rotor control
                    TmPowerForward(maxThrust);
                }

            }
        }
    }
}
