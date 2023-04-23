# FenixQuartz
<img src="img/icon.png" width="196"><br/>
This Tools extracts the Values of some important Quartz Displays of the Fenix (hence the Name) directly from Memory. The Values/Output are then exported to FSUIPC-Offsets as either String (default) or Numerical Value. The Export can also be configured to L-Vars (numerical). There they can be picked up from other Applications, like PilotsDeck to display the FCU or other Displays.<br/>
It is literally an **HACK**: It accesses the Process Memory directly (read-only!) to get the Data. It will likely break with the next Update (until I'll managed to find the correct Spots again).<br/>Currently compatible with Fenix Version **1.0.6.146** and MSFS **SU12**.<br/><br/>
Following Display-Values are available:
- FCU Speed, Heading, Altitude and VS/FPA (Values and Is-Dashed State)
- XPDR (live Input)
- RMP1 and RMP2 Active & Standby Frequency (fully "live" and change correctly when "SEL" is active or ADF/HF/ILS Mode is on)
- ISIS Barometer (only Digital - sorry I don't like old Stuff :grin:)
- BAT1 and BAT2
- Rudder Trim
- Clock CHR and ET
- The Takeoff-Speeds as entered in the MCDU

<br/><br/>

# Installation
- Put the Folder/Binary generally anywhere you want, but *don't* use: Any Application's Folder (e.g. MSFS, Fenix, StreamDeck) or any of the User Folders (Documents, Downloads, etc) and above all not C:\\
- If you're upgrading from a Version before **0.9**, please delete all old Files before unpacking the new Version! (To avoid DLL conflicts)
- It is currently compiled for .NET **7**, you'll probably need to download the according Runtimes (Download [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)). You'll need ".NET Runtime 7.0.x" and ".NET Desktop Runtime 7.0.x" as x64 (Do not confuse it with arm64!).<br/>
Reboot when installing the Runtimes for the first Time. You can check if the Runtimes are correctly installed with the Command `dotnet --list-runtimes`
- You need at least FSUIPC7 Version **7.3.19** - but only if Offsets are used. If the L-Var Output is used, FSUIPC is not requried anymore.
- You also need to install the WASM Module from [MobiFlight](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) (Put it in your Community Folder)

If you want to let it start automatically, you can add this to your FSUIPC7.ini:
```
[Programs]
RunIf1=READY,KILL,X:\PATH\YOU\USED\FenixQuartz.exe
```
The ini-File is in the Folder where FSUIPC was installed to, remember to change the Path to the Binary. If there are multiple RunIf-Entries, make sure they are numbered uniquely and that the [Programs] Section only exists once.<br/>
Works well for my Setup!

<br/><br/>

# Usage
You can start FenixQuartz also manually or by other means if preferred! Either start it before MSFS or start when MSFS has finished loading and is in the Main Menu. It can safely be run with other Planes, it won't do anything besides checking every 15s if the Fenix-Binaries are running and then sleeps again. :wink: <br/>
**NOTE**: The Tool must be run with the same Elevation/User as MSFS and Fenix, so if you for whatever Reason run them "as Admin", make sure to start that Tool also as Admin! It is not needed for the Tool itself, it runs just fine if everything is started normally.<br/><br/>
FenixQuartz does not have a real GUI, but it now has a System-Tray/Notification Icon to verify that it is running (a little "Q"). When you right click on that Icon you have the option to manually Stop it or to force a Rescan manually. It will automatically close itself when you exit MSFS. The Configuration is done via the FenixQuartz.dll.config File (See [Configuration](#configuration)).<br/>
The Memory Scan usually takes about 1-3 Seconds depending on System-Load and Flight-State. The Refresh-Cycle happens every 100ms and takes less than a 1ms. So it should not be something to worry about :sweat_smile: <br/><br/>
The Accuracy was greatly improved with the last Versions and it now also the Capability to detect if the Memory Locations have changed and then does a Rescan automatically. So the Powershell Script to restart it is not really neccessary anymore. Most of the Time it should "just work" and display the correct State as it is in the Fenix!<br/>
Especially the VATSIM/IVAO critical Stuff (FCU, COM, XPDR) is 99% solid. But there is still some Guess-Work involved in choosing the correct Memory-Location for Output, so don't mind if a Battery-Display or the Rudder-Display are just zero from time-to-time. The VS-Mode Detection is now better than ever, but it might still miss it in rare Cases. If the VS-Display is not correct, push/pull either the Alt- or VS-Knob (depending on the Situation) to get it back in line. If you're testing the FCU-Displays on the Ground, please do so before loading/configuring the Flightplan in the MCDU and with the Plane started in Cold & Dark Mode.

<br/><br/>

# Usage with other 3rd Party Tools
FenixQuartz normally exports the Display-Values as formatted Strings for drawing them directly on the StreamDeck (the "*String Mode*"). The Output is directly "ready to use" since the Logic is already applied (displaying "----" instead of the Value or adding leading Zeros for example). You can customize that Output for your Software/Hardware with the **altScaleDelim** or **addFcuMode** Options in the Config-File.<br/><br/>
But if handling numeric Values is easier for your Software/Hardware Combination, you can enable the Raw-Value-Mode (**rawValues**). In that Mode, the Display-Values are exported directly as Numeric Values to FSUIPC Offsets. But then you have to implement the Logic yourself regarding dashed / managed and the Format (leading Zeros)  for example.<br/>
When the Raw-Value-Mode is configured, you can also choose to export the Values to L-Vars (**useLvars**) - both need to be set to true. In that Mode, FSUIPC is not required for FenixQuartz to run. FSUIPC is just needed for Offsets, everything else is done via the MobiFlight WASM Module by now.<br/><br/>

Besides the displayed Value it also exports the "is Dashed" State of a Display (regardless of Mode). The State of the Dot ("is Managed") can directly be read from the Fenix Lvars (I_FCU_SPEED_MANAGED, I_FCU_HEADING_MANAGED, I_FCU_ALTITUDE_MANAGED). So with any Client capable of reading FSUIPC-Offsets and/or Lvars, you have all the Information to build an accurate working FCU!<br/>
It also exports a Variable "isVsActive" which tells if the Plane is currently in VS Mode - that is mostly needed internally, but it exported also to be used in other Use-Cases. I use it for my Alt-Knob-Toggle Function on the StreamDeck, so the Lua-Script does a "Pull" instead of an "Push" on the Alt-Knob when VS is active. Note that part of the accurate Detection of the VS Mode is based on the State-Changes of the Alt- and VS-Knob, make sure they are triggered in the correct way (that is incrementing/decrementing the according S_ Variable).<br/><br/>

Regardless of Mode or Output: The Binary will create a File called **Assignments.txt** when executed which will tell you the Name/Location of the Values (even when no Sim is running). Please note that this is in Accordance to the current Configuration, so you only see the Name/Location for the currently configured Mode and Output! Also note that the TO-Speeds are always exported as L-Vars, regardless of the current Configuration.<br/>
If you miss the Baro-Display and the Time on the Clock: they don't need to be read from Memory. The Value for the Baro can be retrieved via Offset 0x0330 (A-Var "KOHLSMAN SETTING MB:1") which can be converted in Accordance to the existing Fenix L-Vars (for hPa/inHg and Standard). Since the Clock always displays the current Sim-Time, the correct Time can be retrieved via Offsets 0x023B, 0x023C and 0x023A (should be A-Vars ZULU_HOUR, ZULU_MINUTE and CLOCK_SECOND). Example how to build a String out of that can be taken from the FNX320_SYNC Script included in the PilotsDeck Profile/Integration (available on flightsim.to).

<br/><br/>

# Configuration
Please mind that the Config-File is overwritten with an Update, you have to reconfigure your Options after an Update. Please don't just copy over the old Config-File, Options may have been renamed, removed or added. But you can of course make a Backup of it before updating :wink:<br/>
These Parameters are set in the FenixQuartz.dll.config File:
- **waitForConnect**: When *true*, the Binary will wait until the Sim is running and FSUIPC is connected. Default: *"true"*
- **offsetBase**: The first (FSUIPC) Offset Address to use (hexadecimal). Default: *"0x5408"*
- **rawValues**: When *true*, the FCU Values are exported directly as numeric Values ("Raw-Value-Mode"). Default: *"false"*
- **useLvars**: When *true*, the Values are exported as L-Vars instead of FSUIPC Offsets (only works when rawValues is also set). Default: *"false"*
- **updateIntervall**: The time between each Update in Milliseconds (Read Memory-Locations -> Write to Variables). Default: *"100"*
- **altScaleDelim**: The Character to be inserted in the FCU Altitude String when the Scale is set to 100. Has no Effect on the Raw-Value-Mode. Default: *" "* (Space)
- **addFcuMode**: If *false*, the FCUs Displays show only the Value (no SPD/HDG/VS) in one line (Still as String, has no Effect on the Raw-Value-Mode). Default: *"true"*
- **ooMode**: If *true*, the Zeroes on the VS-Display will be exported as "o" instead of "0" if your Hardware/Font allows Letters to be displayed (only relevant in String-Mode). Default *"false"*
- **lvarPrefix**: The Prefix of the L-Var Names used for export. Still defaults to the old Name FNX2PLD, but you can of course change that! The old Name is used to ensure Compatibility. Default *"FNX2PLD_"*
- **ignoreBatteries**: The L-Var Output can have Issues with Memory-Consumption which directly correlates to the Amount of Updates and the Battery Values are constantly changing. If you are using L-Var-Output and don't need the Battery Values, you can disable Updates for them with *true*. Absolutely not needed when using Offsets. Default *"false"*

