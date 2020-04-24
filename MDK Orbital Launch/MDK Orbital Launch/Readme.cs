﻿/* Wico Craft ORIBTAL LAUNCH control sub-module
 * 
 * 
* Handles MODES:
* MODE_HOVER
* MODE_LAUNCHPREP
* MODE_LANDED
* MODE_ORBITALLAUNCH
* 
* Commands:
* 
* setmaxspeed <value>: sets the maximum speed in m/s. Default speed is 100. Only need to set if mod increases speed
* resetlaunch: reset any saved launch locations.
* orbitallaunch: Start launch to orbit
* 
* 1.66 (initial version)
*    
*   2.2 SE V1.72 Changes
*
* 3.0 Serialize changes and current code
* 3.0a Optimize for connectors init.
* 3.0b merge grid fixes
* 
* 3.0c MDK Version https://github.com/malware-dev/MDK-SE/wiki
* Uncompressed Source here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Orbital%20Launch
* 
* 3.1 Updates for SE V1.185
* 
* 3.1a build with current source
* 
* 3.1B current source 12/22/2017
* 
* 3.2 INI WCCM Serialize 01062018
* 
* 3.2a
* FilledRatio Change
* 
* 3.2b remove atmocrossover alt and use effectiveness of atmothrusters
* 
* 3.3 Support multiple Text Panels
* Only write to text panels at end of script.
* 
* 3.3a Rewrite of serlization
* 
* 3.4A Current Code Mar 03 2018
* 
* 3.4B performance enhancements (medium instead of fast)
*  new calculateBestGravityThrust() to choose best orbital launch side dynamically
*  add autogyro command to toggle autogyro
*  add timer names to CustomData
*  optimize updatetype. Add debug update to show updatetype
*  Apr 03 2018
*  
*  3.4C Apr 26 2018
*  
*  3.4D
*  June 09,2018
*  
*  3.5 Jan 21+.  SE 1.189
* 
* Need:
*  circumnavigate planet to target spot
*  'ask' for docking position after arriving at 'spot'

*/
