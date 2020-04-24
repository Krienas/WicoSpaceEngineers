﻿/*
* Wico craft controller Master Control Script
*
* Control Script for Rovers and Drones and Oribtal craft
* 
* Uncompressed source for this script here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Craft%20Control
 * 
 * 
 * Handles:
* Master timer for sub-modules
* Calculates ship speed and vectors (obsolete)
* Calculates simspeed (obsolete)
* Configure craft_operation settings
* making sure antenna doesn't get turned off (bug in SE turn off antenna when trying to remotely connect to grid)
* 
* Calculates cargo and power percentages and cargo multiplier and hydro fill and oxy tank fill
 * 
 * Detects grid changes and initiates re-init
 * 
* * 
* MODE_IDLE
* MODE_ATTENTION
* 
* Commands:
* 
* setsimspeed <value>: sets the current simspeed so the calculations can be accurate. (obsolete)
* init: re-init all blocks
* idle : force MODE_IDLE
* coast: turns on/off backward thrusters
* masterreset: attempts to do a master reset of all saved information
* setvaluef <blockname>:<property>:<value>  -> sets specified block's property to specified value
* Example:
*  setvaluef Advanced Rotor:UpperLimit:-24
* genpatrol [distance [up]]
* Examples:
* genpatrol
* genpatrol 300 150
* genpatrol 500
*
* Need:

* Want:
* 
* menu management for commands (including sub-modules)
* 
* minimize serialized data and make sub-modules pass their own seperately, OR support extra data in state
* 
* common function for 'handle this' combining 'me' grid and an exclusion name check
*
* multi-script handling for modes
* 
* * advanced trigger: only when module handles that mode... (so need mode->module dictionary)
* override base modes?
*
*
*
* WANT:
* setvalueb
* Actions
* Trigger timers on 'events'.
* set antenna name to match mode?
* *
* 2.0 Removed many built-in functions to make script room. These functions were duplicated in sub-modules anyway.
* 2.0.1
* 0.2 Remove items from serialize that main control no longer calculates (cargo, battery, etc).
* if simspeed>1.01, assume 1.0 and recalculate.
* 0.3 re-org code sections
* Pass arguments to sub-modules 
* 0.4 (re)integrate power and cargo
* 0.4a process multiple arguments on a command line
* 0.4b check mass change and request reinit including sub-modules.
* 
* 2.1 Code Reorg
* Cache all blocks and grids.  Support for multi-grid constructions.
* !Needs handling for grids connected via connectors..
* 
* .1a Don't force re-init on working projector.
* .1b Add 'brake' command
* Add braking for sleds (added wheelinit)
* 
* 2.2 PB changes in 1.172
* 
* .2a Added modes. Default PB name
* 
* 2.3 Start to add Power information
* 
* .3a Add drills and ejectors to reset motion. Add welders, drills, connectors and grinders to cargo check.
* don't set PB name because it erases settings.. :(
* 
* .3b getblocks fixes when called before gridsinit
* 
* 3.0 remove older items from serialize that are no longer needed
* removed NAV support
* fixed battery maxoutput values
* 
* 3.0a support no remote control blocks. Check for Cryo when getting default controller.
* 3.0b sBanner
* 3.0c caching optimizations
* 3.0d fix connectorsanyconnectors not using localdock
* 3.0e Add Master Reset command
* 3.0f 
* check for grid changes and re-init 
* rotor NOFOLLOW
* ignore projectors with !WCC in name or customdata
* ignore 'cutter' thrusters
* 
* 3.0g Fix problem with allBlockCount being loaded after it has changed
* 
* 3.0H 
* fix problems with docking/undocking and perm re-init
* 
* 05/13: fix GetBlocksContains<T>()
* 
* 3.0I MDK Version 08/20/2017   MDK: https://github.com/malware-dev/MDK-SE/
* Uncompressed source for this script here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Craft%20Control
* 
* 3.0J Add moduleDoPreModes() to Main()
* Move pre-mode to moduleDoPreModes()
* add clearing of gpsPanel to moduleDoPreModes()
* 
* 3.0K more init states if larger number of blocks in grid system.
* 
* 3.0K2  search order for text panels
* 
* 3.1 Verison for SE 1.185 PB Major changes
* 
* 3.1A init cycle optimizations
* 
* 3.1B Handle no controller (stations, etc)
* 12092017
* 
* 3.1C 12132017
* don't count ejectors in cargo%
* fix bug in DoTriggerMain() causing updates to stop
* 
* 3.1D Section processing for save information (text panels)
* fix bug in serialize wrting z,y z, instead of x,y,z (oops)
* 
* 3.2 INI WCCM 01062018
*
* 3.2A
* FilledRatio Change
* 
* 3.2B Lots of INI processing
* 
* 3.3 Handle multiple output panels.
* Only write to panels at end
* 
* 3.3A Redo Serlialize.
* Module Serlialize
* 
* 3.4 
* add namecameras
* 
* 3.4a
* init optimizations for text panels
* 
* 3.4B turn off auto-pirate mode.
* 
* 3.4C options for timer names
* options for debugupdate
* options for submodule trigger rate
* 
* 3.4D Mar 29 2018 Current Source
* removed ModeScans/MODE_DOSCAN
* 
* 3.4E Mar 22, 2018
* Error messages on missing blocks on startup
* Re-try startup if there are errors.
* 
* 3.4F 
* handle stations having no propulsion methods
* increase default sub-module trigger to 5seconds
* May 27, 2018
* 
* 3.4G June 08,2018
* Add setmode and setstate commands
* Clear all panels on masterreset command
* 
* 3.4H June 19,2018
* Add genpatrol command to generate a set of patrol waypoints around this ship.
* Defaults for distance are 500 and up  is 500
* 
* 3.4I
* July 23 SE 1.187 MDK 1.1.16
* 
* 3.4J Sep 08 2018
* MDK Update
* Performance Pass
* 
* 
* genpatrol [distance [up]]
* genpatrol
* genpatrol 300 150
* genpatrol 500
* 
* 3.4J
* 
* 3.5 SE V1.189
* 
* 3.7 05292019 SE 1.190
* Current source (no antenna send)
* 
* 3.7a 11242019 
* Current source
* 
* 3.8 12222019
* Old IGC removal in SE 1.193.100.  Remove references to old IGC.
* 
* 3.8a 12232019
* MyIni error reporting removed
*/
