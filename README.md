# PilotsDeck_FNX
This Binary is used read out some (Quartz) Displays of the Fenix A320 and write their Data to FSUIPC Offsets. There PilotsDeck (or any other Application using FSUIPC basically) can pick it up to display it on the StreamDeck.<br/>
To make one Thing directly clear: it is a **HACK**. Literally: It accesses the Process Memory directly (read-only!) to get the Data. It will likely break with the next Update (until I'll managed to find the correct Spots again).<br/>Currently compatible with Fenix Version **1.0.6.146** and MSFS **SU11**.<br/>
<br/>

# Installation
- Put the Folder/Binary generally anywhere you want, but *don't* use: Any Application's Folder (e.g. MSFS, Fenix, StreamDeck) or any of the User Folders (Documents, Downloads, etc).
- If you're upgrading from a Version before **0.8**, please delete all old Files before unpacking the new Version! (To avoid DLL conflicts, the Binary contains now everything it needs except the FSUIPC_WAPID.dll)
- It is currently compiled for .NET **7**, you'll probably need to download the according Runtimes (Download [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)). You'll need ".NET Runtime 7.0.x" and ".NET Desktop Runtime 7.0.x" as x64. (Do not confuse it with arm64!)
- You need at least FSUIPC7 Version **7.3.15**! Please make sure you install / update the WASM Module (its manifest.json should show 0.9.1).<br/>

It is designed to be started (and stopped) by FSUIPC - add this to your ini File:
```
[Programs]
RunIf1=READY,KILL,X:\PATH\YOU\USED\PilotsDeck_FNX2PLD.exe
```
But you can also start/stop it manually when MSFS is running!
<br/>

# Usage
If you want to start the Binary manually and before the Sim is running, set the *waitForConnect* Parameter in the Config-File to true!<br/>
When it is running (started either automatically or manually) just set up your Flight as you normally would. The Tool will wait in the Background until the Fenix becomes active. It also automatically detects when you have clicked Ready-to-Fly before its intial Scan (the Timer from previous Versions is gone now).<br/>
Note that this Application has no Window - you have to use Task Manager if you want to see if is running. It should close automatically after MSFS is closed (even when started manually with *waitForConnect*).<br/>
The Memory Scan usually takes about one Second and the time it takes for an Update Cycle is around 0,3ms on average (on my System).
<br/>

# Usage with other 3rd Party Tools
The Binary normally exports the Display-Values as formatted Strings for drawing them directly on the StreamDeck. You can customize the Output for your Software/Hardware with the *altScaleDelim* or *addFcuMode* Options in the Config-File.<br/>
But if handling numeric Values is easier for your Software/Hardware Combination, you can enable the Raw-Value-Mode which is available since Version 0.6. In that Mode, the Display-Values for the FCU (Speed, Heading, Altitude, Vertical Speed) are exported directly as Numeric Values to FSUIPC Offsets. But then you have to implement the Logic yourself regarding dashed / managed for example and the Format (leading Zeros).<br/>
Besides the displayed Value it also exports the "is Dashed" State of that Display (regardless of Mode). The State of the Dot ("is Managed") can directly be read from the Fenix Lvars (I_FCU_SPEED_MANAGED, I_FCU_HEADING_MANAGED, I_FCU_ALTITUDE_MANAGED). So with any Client capable of reading FSUIPC-Offsets and Lvars, you have all the Information to build an accurate working FCU!<br/>
To enable the Raw-Value-Mode, set *rawValues* in the Config-File to true. Size, Type and Addresses of the Offsets can be found in the Logs when the Binary is started and is connected to MSFS/Fenix. The Order and Addresses will stay the same (unless the Offset-Base is changed in the Config-File).<br/>
Please mind that the Default is false, so you have to re-enable it after an Update of the Binary!
<br/>

# Configuration
You can configure some Parameters in the PilotsDeck_FNX2PLD.dll.config File:
- **waitForConnect**: When *true*, the Binary will wait until the Sim is running and FSUIPC is connected. Default: *"false"*
- **offsetBase**: The first (FSUIPC) Offset Address to use (hexadecimal). Default: *"0x5408"*
- **rawValues**: When *true*, the FCU Values are exported directly as numeric Values ("Raw-Value-Mode"). Default: *"false"*
- **updateIntervall**: The time between each Update in Milliseconds. Default: *"50"*
- **altScaleDelim**: The Character to be inserted in the FCU Altitude String when the Scale is set to 100. Has no Effect on the Raw-Value-Mode. Default: *" "* (Space)
- **addFcuMode**: If false, the FCUs Displays show only the Value (no SPD/HDG/VS) in one line (Still as String, has no Effect on the Raw-Value-Mode). Default: *"true"*

