# FenixQuartz
This Binary is used read out some (Quartz) Displays of the Fenix A320 and write their Data to FSUIPC Offsets. There PilotsDeck (or any other Application which can either read FSUIPC Offsets or L-Vars) can pick it up to display it on the StreamDeck.<br/>
To make one Thing directly clear: it is a **HACK**. Literally: It accesses the Process Memory directly (read-only!) to get the Data. It will likely break with the next Update (until I'll managed to find the correct Spots again).<br/>Currently compatible with Fenix Version **1.0.6.146** and MSFS **SU12**.<br/>
<br/><br/>

# Installation
- Put the Folder/Binary generally anywhere you want, but *don't* use: Any Application's Folder (e.g. MSFS, Fenix, StreamDeck) or any of the User Folders (Documents, Downloads, etc).
- If you're upgrading from a Version before **0.8**, please delete all old Files before unpacking the new Version! (To avoid DLL conflicts)
- It is currently compiled for .NET **7**, you'll probably need to download the according Runtimes (Download [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)). You'll need ".NET Runtime 7.0.x" and ".NET Desktop Runtime 7.0.x" as x64. (Do not confuse it with arm64!)
- You need at least FSUIPC7 Version **7.3.16**!
- You also need to install the WASM Module from [MobiFlight](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) (Put it in your Community Folder)

It is designed to be started (and stopped) by FSUIPC - add this to your ini File:
```
[Programs]
RunIf1=READY,KILL,X:\PATH\YOU\USED\FenixQuartz.exe
```
But you can also start/stop it manually when MSFS is running (finished loading to the Menu)!<br/>
If you want to start/stop it manually, you can use the Powershell-Script [here](https://github.com/Fragtality/FenixQuartz/blob/master/Restart-FenixQuartz.ps1). You need to change the Path to the Binary in the Script before it can do anything useful. BUT: Don't restart it in Flight unless you really need to!
<br/><br/>

# Usage
If you want to start the Binary manually and before the Sim is running, set the *waitForConnect* Parameter in the Config-File to true!<br/>
When it is running (started either automatically or manually) just set up your Flight as you normally would. The Tool will wait in the Background until the Fenix becomes active. It also automatically detects when you have clicked Ready-to-Fly before its intial Scan (the Timer from previous Versions is gone now). It is best started (manually) when MSFS is in the Main Menu.<br/>
Note that this Application has no Window - you have to use Task Manager if you want to see if is running. It should close automatically after MSFS is closed (even when started manually with *waitForConnect*).<br/>
The Memory Scan usually takes about one Second and the time it takes for an Update Cycle is around 0,3ms on average (on my System).<br/><br/>
Note that this Binary is a Best-Effort-Approach to have working FCU for the Fenix. It is by no means 100% accurate in every possible Situation - I need to guess Things. But it is very accurate in most and normal Flight-Situations (with Flight meaning you are in the Air). So please don't play arround on the Ground, especially with an uninitialized FMGC, and then tell me it is not working correctly :wink:<br/>
The most "guesswork" is on the VS-Mode, especially in the Transition from (OP) CLB to VS. It should work when you have switched the Vertical Mode via the ALT or VS Knob (in the correct Way of increasing/decreasing it) - it is monitoring these Switches to get the Transition. The Transition into a managed Mode (e.g. from VS to G/S) should work though (since the Display goes dashed and the Value doesn't matter anymore).<br/>
Also Note that the Binary should not be started/restarted while in Flight - it could miss some Memory-Locations then and the FCU will not function correctly until the Fenix was reloaded!
<br/><br/>

# Usage with other 3rd Party Tools
The Binary normally exports the Display-Values as formatted Strings for drawing them directly on the StreamDeck. You can customize the Output for your Software/Hardware with the *altScaleDelim* or *addFcuMode* Options in the Config-File.<br/><br/>
But if handling numeric Values is easier for your Software/Hardware Combination, you can enable the Raw-Value-Mode which is available since Version 0.6. In that Mode, the Display-Values for the FCU (Speed, Heading, Altitude, Vertical Speed) are exported directly as Numeric Values to FSUIPC Offsets. But then you have to implement the Logic yourself regarding dashed / managed for example and the Format (leading Zeros). Starting with Version 0.9 you can use *useLvars* to output the raw Values as L-Vars (you need to set both *rawValues* and *useLvars*).<br/><br/>
Besides the displayed Value it also exports the "is Dashed" State of that Display (regardless of Mode). The State of the Dot ("is Managed") can directly be read from the Fenix Lvars (I_FCU_SPEED_MANAGED, I_FCU_HEADING_MANAGED, I_FCU_ALTITUDE_MANAGED). So with any Client capable of reading FSUIPC-Offsets and Lvars, you have all the Information to build an accurate working FCU! It also exports a Variable "isVsActive" which tells if the Plane is currently in VS Mode - but the Detection is only reliable when the Vertical Modes are switched manually with the FCU Knobs (in the correct Way of incrementing/decrementing the S_ Variable).<br/><br/>
Regardless of Mode or Output: The Binary will create a File called *Assignments.txt* which will tell you the Name/Location of the Values (even when no Sim is running). Please note that this is in Accordance to the current Configuration, so you only see the Name/Location for the currently configured Mode and Output!<br/>
Please mind that the Config-File is overwritten with an Update, you have to reconfigure your Options after an Update. (Please don't just keep the old Config-File!)
<br/><br/>

# Configuration
You can configure some Parameters in the FenixQuartz.dll.config File:
- **waitForConnect**: When *true*, the Binary will wait until the Sim is running and FSUIPC is connected. Default: *"false"*
- **offsetBase**: The first (FSUIPC) Offset Address to use (hexadecimal). Default: *"0x5408"*
- **rawValues**: When *true*, the FCU Values are exported directly as numeric Values ("Raw-Value-Mode"). Default: *"false"*
- **useLvars**: When *true*, the Values are exported as L-Vars instead of FSUIPC Offsets (only works when rawValues is also set). Default: *"false"*
- **updateIntervall**: The time between each Update in Milliseconds. Default: *"50"*
- **altScaleDelim**: The Character to be inserted in the FCU Altitude String when the Scale is set to 100. Has no Effect on the Raw-Value-Mode. Default: *" "* (Space)
- **addFcuMode**: If false, the FCUs Displays show only the Value (no SPD/HDG/VS) in one line (Still as String, has no Effect on the Raw-Value-Mode). Default: *"true"*

