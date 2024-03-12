# FenixQuartz
<img src="img/icon.png" width="196"><br/>
This Tools extracts the Values of some important Quartz Displays of the Fenix (hence the Name) directly from Memory. The Values/Output are then exported to FSUIPC-Offsets as either String (default) or Numerical Value. The Export can also be configured to L-Vars (numerical). There they can be picked up from other Applications, like PilotsDeck to display the FCU or other Displays on the StreamDeck.<br/>
Most Data is now retrieved via native Fenix L-Vars, but XPDR Input and Standby Baro still require to access Memory directly.<br/>Currently compatible with Fenix **V2B2** upwards and MSFS **SU14**.<br/><br/>
Following Display-Values are available:
- FCU Speed, Heading, Altitude and VS/FPA (Values and Is-Dashed State)
- XPDR (live Input)
- RMP1 and RMP2 Active & Standby Frequency (fully "live" and change correctly when "SEL" is active or ADF/HF/ILS Mode is on)
- ISIS Barometer (only Digital - sorry I don't like old Stuff :grin:)
- Captain Barometer
- BAT1 and BAT2
- Rudder Trim
- Clock CHR, UTC and ET
- The Takeoff/V-Speeds and Flex Temperature as entered in the MCDU (fully automatic, no manual Intervention neccessary anymore)

<br/><br/><br/>

# Requirements

- Windows 10/11, MSFS, Fenix :wink:
- Depending on the Mode you use: [FSUIPC7](http://fsuipc.com/)
- Capability to actually read the Readme up until and beyond this Point :stuck_out_tongue_winking_eye:
- The Installer will install the following Software:
  - .NET 7 Desktop Runtime (x64)
  - MobiFlight Event/WASM Module

[Download here](https://github.com/Fragtality/FenixQuartz/releases/latest)
(Under Assests, the FenixQuartz-Installer-vXYZ.exe File)

<br/><br/><br/>

# Installation / Update
Basically: Just run the Installer - it will extract it for you to a fixed Location and will also install/update the neccessary Software to your PC/Sim. It even setups Auto-Start and creates a Link on the Desktop, if you want.<br/><br/>
You can choose directly in the Installer to extract a preconfigured Configuration File for each of these Modes during Installation/Update. First-Time Installations will default to String/Offset Mode if "Do not change" is selected. Choosing any other Option for Updating/Reinstalling will overwrite the *whole* existing Configuration File.


Some Notes:

- FenixQuartz has to be stopped manually before installing.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- The FSUIPC Version is only checked - you still have to install/update it manually. FSUIPC is only needed if you plan to use the String/Offset or Raw-Value/Offset Mode!<br/>(Ignore the Warning in the Installer Output if you're going to use Raw-Value/L-Var Mode)
- If you upgrade from Version 1.1 or below, delete your old Installation manually (it is no longer needed).
- From Version 1.2 onwards, your Configuration will not be resetted after Updating
- The Installation-Location is fixed to %appdata%\FenixQuartz (your Users AppData\Roaming Folder) and can not be changed.
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup (not deemed neccessary), so if you want a Backup, do so yourself.
- Do not run the Installer as Admin!
- It may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps (for the whole FenixQuartz Folder)

<br/><br/><br/>

# Usage
You can also start FenixQuartz manually or by other means if you did not let the Installer configure an automatic Start. Either start it before MSFS or start when MSFS has finished loading and is in the Main Menu. It can safely be run with other Planes, it won't do anything besides checking every 15s if the Fenix-Binaries are running and then sleeps again. :wink:<br/>
**NOTE**: The Tool must be run with the **same Elevation/User** as MSFS and Fenix! If you for whatever Reason run them "as Admin", make sure to start that Tool also as Admin! It is not needed for the Tool itself, it runs just fine if everything is started with you normal User.<br/><br/>

It does not open a Window when started, but you should see it in the System-Tray/Notification Area once it runs (a little "Q"). It is designed to run silently in the Background. It will stop itself when you exit MSFS.<br/>
When you right click on that Icon you have the option to manually close it or to force a Memory Scan manually.<br/>
When you left click on the Icon it will open its "Debug UI" displaying all Values as they are read from Memory. It is only really there to verify/troubleshoot found the correct Memory Locations (and uses valid Values).<br/><br/>

When you use it for Quartz Displays you don't have to start a Memory-Scan manually - just start FenixQuartz and load up the Fenix and it will output the Display-Values to FSUIPC Offset or L-Vars (depending on the Configuration).<br/><br/>

<br/><br/><br/>

# Usage with other 3rd Party Tools
FenixQuartz normally exports the Display-Values as formatted Strings for drawing them directly on the StreamDeck (the "*String/Offset Mode*"). The Output is directly "ready to use" since the Logic is already applied (displaying "----" instead of the Value or adding leading Zeros for example). You can customize that Output for your Software/Hardware with the **altScaleDelim**, **addFcuMode** or **ooMode** Options  in the [Configuration](#configuration) File.<br/><br/>

**NOTE**: The Raw-Value Modes are now considered deprecated! They will be removed in the future Releases. Most Data can now be directly accessed via native Fenix L-Vars, so it is now only copying the Value from L-Var "A" to "B" mostly.

Regardless of Mode or Output: The Binary will create a File called **Assignments.txt** (located in %appdata\FenixQuartz) when executed which will tell you the Name/Location of the Values (even when no Sim is running). Please note that this is in Accordance to the current Configuration, so you only see the Name/Location for the currently configured Mode and Output! Also note that the TO-Speeds are always exported as L-Vars, regardless of the current Configuration.

<br/><br/><br/>

# Configuration
The Path Configuration File is located at `%appdata\FenixQuartz\FenixQuartz.config` and can be edited with any Text-Editor. Starting with Version 1.2 your current Configuration is preserved when updating.<br/>
The available Parameters are:
- **waitForConnect**: When *true*, the Binary will wait until the Sim is running and connected. Default: *"true"*
- **offsetBase**: The first (FSUIPC) Offset Address to use (hexadecimal). Default: *"0x5408"*
- **rawValues**: When *true*, the FCU Values are exported directly as numeric Values ("Raw-Value/Offset" Mode). Default: *"false"*
- **useLvars**: When *true*, the Values are exported as L-Vars instead of FSUIPC Offsets ("Raw-Value/L-Var" Mode). Only works when rawValues is also set!. Default: *"false"*
- **updateIntervall**: The time between each Update in Milliseconds (Read Memory-Locations -> Write to Variables). Default: *"100"*
- **scaleMachValue**: The MACH Value is scaled to a float Value in in both Raw-Value Modes (e.g. it is output as 0.79 instead of 79)
- **altScaleDelim**: The Character to be inserted in the FCU Altitude String when the Scale is set to 100. Has no Effect on any the Raw-Value Mode. Default: *" "* (Space)
- **addFcuMode**: If *false*, the FCUs Displays show only the Value (no SPD/HDG/VS Header) in one Line. Has no Effect on any Raw-Value Mode. Default: *"true"*
- **ooMode**: If *true*, the Zeroes on the VS-Display will be exported as "o" instead of "0" if your Hardware/Font allows Letters to be displayed (only relevant in String/Offset Mode). Default *"false"*
- **lvarPrefix**: The Prefix of the L-Var Names used for export. Still defaults to the old Name FNX2PLD, but you can of course change that! The old Name is used to ensure Compatibility. Default *"FNX2PLD_"*
