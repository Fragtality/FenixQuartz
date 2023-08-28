using FSUIPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace FenixQuartz
{
    public class ElementManager : IDisposable
    {
        public Dictionary<string, MemoryPattern> MemoryPatterns;
        public Dictionary<string, MemoryValue> MemoryValues;
        protected List<OutputDefinition> Definitions;

        protected Dictionary<string, IPCValue> IPCValues;
        protected MemoryScanner Scanner;
        public static readonly NumberFormatInfo formatInfo = new CultureInfo("en-US").NumberFormat;
        private bool firstUpdate = true;

        public float lastSwitchVS;
        public float lastSwitchAlt;

        public bool lastVSdashed = false;
        public int lastVSval = 0;
        public bool lastALTmanaged = false;
        public bool lastHDGmanaged = false;
        public bool isAltVsMode = false;
        public bool isLightTest = false;
        public bool isAltManaged = false;
        public bool isHdgManaged = false;
        public bool isSpdManaged = false;
        public bool fcuNotInitialized = true;
        public bool fcuIsPowered = false;
        public bool perfWasSet = false;
        public bool simOnGround = true;
        public bool hasLanded = false;
        public int speedV1 = 0;
        public int speedVR = 0;
        public int speedV2 = 0;
        public bool xpdrWasCleared = false;
        public int xpdrClearedCounter = 0;

        public ElementManager(List<OutputDefinition> definitions)
        {
            IPCValues = new ();
            MemoryValues = new ();
            Definitions = definitions;

            //// MEMORY PATTERNS
            MemoryPatterns = new()
            {
                { "FCU-1", new MemoryPattern("46 00 43 00 55 00 20 00 70 00 6F 00 77 00 65 00 72 00 20 00 69 00 6E 00 70 00 75 00 74 00") },
                { "FCU-2", new MemoryPattern("00 00 00 00 CE 05 00 00 FF FF FF FF 00 00 00 80") },
                { "ISIS-1", new MemoryPattern("49 00 53 00 49 00 53 00 20 00 70 00 6F 00 77 00 65 00 72 00 65 00 64 00") },
                { "ISIS-2", new MemoryPattern("46 00 65 00 6E 00 69 00 78 00 42 00 72 00 61 00 6B 00 65 00 46 00 61 00 6E 00 73 00") },
                { "XPDR-1", new MemoryPattern("58 00 50 00 44 00 52 00 20 00 63 00 68 00 61 00 72 00 61 00 63 00 74 00 65 00 72 00 73 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 65 00 64") },
                { "BAT1-1", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "BAT2-1", new MemoryPattern("61 00 69 00 72 00 63 00 72 00 61 00 66 00 74 00 2E 00 65 00 6C 00 65 00 63 00 74 00 72 00 69 00 63 00 61 00 6C 00 2E 00 62 00 61 00 74 00 74 00 65 00 72 00 79 00 31 00 2E") },
                { "BAT2-2", new MemoryPattern("00 00 42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 32 00") },
                { "RUDDER-1", new MemoryPattern("00 00 52 00 75 00 64 00 64 00 65 00 72 00 20 00 74 00 72 00 69 00 6D 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 20 00 64 00 61 00 73 00 68 00 65 00 64 00") },
                { "MCDU-1", new MemoryPattern("00 00 00 00 10 27 00 00 10 27 00 00 ?? FF FF FF ?? FF FF FF ?? FF FF FF ?? FF FF FF 00 00 ?? 00 00 00 00 ?? 00 00 00 00 00 00 00 00", 2) },
                { "MCDU-2", new MemoryPattern("00 00 00 00 10 27 00 00 10 27 00 00 ?? FF FF FF ?? FF FF FF ?? FF FF FF ?? FF FF FF 00 ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00", 2) },
                { "MCDU-3", new MemoryPattern("00 00 00 00 10 27 00 00 10 27 00 00 ?? FF FF FF ?? FF FF FF ?? FF FF FF ?? FF FF FF 00 ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00", 3) },
                { "MCDU-4", new MemoryPattern("00 00 00 00 10 27 00 00 10 27 00 00 ?? FF FF FF ?? FF FF FF ?? FF FF FF ?? FF FF FF 00 00 ?? 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "MCDU-5", new MemoryPattern("4E D4 90 C0 38 2B 48 40 47 F7 7B 7B 3A 6B 27 40 4E D4 90 C0 38 2B 48 40 47 F7 7B 7B 3A 6B 27 40") },

            };

            InitializeScanner();


            //// MEMORY VALUES
            //FCU
            AddMemoryValue("fcuSpd", MemoryPatterns["FCU-1"], -0x6C, 8, "double");
            AddMemoryValue("fcuSpdManaged", MemoryPatterns["FCU-1"], -0x28, 4, "int");
            AddMemoryValue("fcuSpdDashed", MemoryPatterns["FCU-2"], -0x90C, 1, "bool");
            AddMemoryValue("fcuHdgManaged", MemoryPatterns["FCU-1"], -0x20, 4, "int");
            AddMemoryValue("fcuHdg", MemoryPatterns["FCU-1"], -0x60, 4, "int");
            AddMemoryValue("fcuHdgDashed", MemoryPatterns["FCU-2"], -0x1764, 1, "bool");
            AddMemoryValue("fcuAlt", MemoryPatterns["FCU-1"], -0x5C, 4, "int");
            AddMemoryValue("fcuVsManaged", MemoryPatterns["FCU-1"], -0x18, 4, "int");
            AddMemoryValue("fcuVs", MemoryPatterns["FCU-1"], -0x64, 4, "int");
            AddMemoryValue("fcuVsDashed", MemoryPatterns["FCU-2"], -0x66C, 1, "bool");

            //ISIS
            AddMemoryValue("isisStd1", MemoryPatterns["ISIS-1"], -0xC7, 1, "bool");
            AddMemoryValue("isisBaro1", MemoryPatterns["ISIS-1"], -0xEC, 8, "double");
            AddMemoryValue("isisStd2", MemoryPatterns["ISIS-1"], -0xDF, 1, "bool");
            AddMemoryValue("isisBaro2", MemoryPatterns["ISIS-1"], -0x104, 8, "double");
            AddMemoryValue("isisStd3", MemoryPatterns["ISIS-2"], -0x3F, 1, "bool");
            AddMemoryValue("isisBaro3", MemoryPatterns["ISIS-2"], -0x64, 8, "double");

            //COM1
            AddMemoryValue("com1Standby", MemoryPatterns["FCU-2"], +0x45C, 4, "int");
            AddMemoryValue("com1Active", MemoryPatterns["FCU-2"], +0x444, 4, "int");

            //COM2
            AddMemoryValue("com2Active", MemoryPatterns["FCU-2"], +0x3E4, 4, "int");
            AddMemoryValue("com2Standby", MemoryPatterns["FCU-2"], +0x3FC, 4, "int");

            //XPDR
            AddMemoryValue("xpdrDisplay", MemoryPatterns["XPDR-1"], -0x110, 2, "int");
            AddMemoryValue("xpdrInput", MemoryPatterns["FCU-2"], +0x714, 2, "int");
            AddMemoryValue("xpdrDigits", MemoryPatterns["FCU-2"], +0x90C, 2, "int");

            //BAT
            if (!App.ignoreBatteries)
            {
                //BAT1
                AddMemoryValue("bat1Display", MemoryPatterns["BAT1-1"], -0x2C, 8, "double");

                //BAT2
                AddMemoryValue("bat2Display1", MemoryPatterns["BAT2-1"], +0x51C, 8, "double");
                AddMemoryValue("bat2Display2", MemoryPatterns["BAT2-2"], -0x282, 8, "double"); //same as +89C to pattern 2-1?
            }

            //RUDDER
            AddMemoryValue("rudderDisplay1", MemoryPatterns["RUDDER-1"], 0xB9E, 8, "double");
            AddMemoryValue("rudderDisplay2", MemoryPatterns["RUDDER-1"], 0xBCE, 8, "double");
            AddMemoryValue("rudderDisplay3", MemoryPatterns["RUDDER-1"], 0xB1E, 8, "double");
            AddMemoryValue("rudderDisplay4", MemoryPatterns["RUDDER-1"], 0xB6E, 8, "double");
            AddMemoryValue("rudderDisplay5", MemoryPatterns["RUDDER-1"], 0xB46, 8, "double");

            //CHR / ET
            AddMemoryValue("clockCHR", MemoryPatterns["FCU-2"], -0x54, 4, "int");
            AddMemoryValue("clockET", MemoryPatterns["FCU-2"], -0x3C, 4, "int");

            //TO Speeds
            AddMemoryValue("speedV1-1", MemoryPatterns["MCDU-1"], +0xAB30, 4, "int"); //+0xA5B8
            AddMemoryValue("speedVR-1", MemoryPatterns["MCDU-1"], +0xAB40, 4, "int");
            AddMemoryValue("speedV2-1", MemoryPatterns["MCDU-1"], +0xAB38, 4, "int");
            AddMemoryValue("speedV1-2", MemoryPatterns["MCDU-2"], +0x590, 4, "int"); //+0x18
            AddMemoryValue("speedVR-2", MemoryPatterns["MCDU-2"], +0x5A0, 4, "int");
            AddMemoryValue("speedV2-2", MemoryPatterns["MCDU-2"], +0x598, 4, "int");
            AddMemoryValue("speedV1-3", MemoryPatterns["MCDU-3"], +0x590, 4, "int"); //+0x18
            AddMemoryValue("speedVR-3", MemoryPatterns["MCDU-3"], +0x5A0, 4, "int");
            AddMemoryValue("speedV2-3", MemoryPatterns["MCDU-3"], +0x598, 4, "int");
            AddMemoryValue("speedV1-4", MemoryPatterns["MCDU-4"], +0xAE8, 4, "int"); //+0x40
            AddMemoryValue("speedVR-4", MemoryPatterns["MCDU-4"], +0xAF8, 4, "int");
            AddMemoryValue("speedV2-4", MemoryPatterns["MCDU-4"], +0xAF0, 4, "int");
            AddMemoryValue("speedV1-5", MemoryPatterns["MCDU-5"], +0x104, 4, "int"); //+0x40
            AddMemoryValue("speedVR-5", MemoryPatterns["MCDU-5"], +0x114, 4, "int");
            AddMemoryValue("speedV2-5", MemoryPatterns["MCDU-5"], +0x10C, 4, "int");
            AddMemoryValue("speedV1-6", MemoryPatterns["MCDU-2"], -0x14AEC0, 4, "int"); //+0x40
            AddMemoryValue("speedVR-6", MemoryPatterns["MCDU-2"], -0x14AEB0, 4, "int");
            AddMemoryValue("speedV2-6", MemoryPatterns["MCDU-2"], -0x14AEB8, 4, "int");

            //VAPP manual
            AddMemoryValue("speedVAPP-1", MemoryPatterns["MCDU-1"], +0xAC50, 4, "int"); //6A0?
            AddMemoryValue("speedVAPP-2", MemoryPatterns["MCDU-2"], +0x6B0, 4, "int"); //6A0 -
            AddMemoryValue("speedVAPP-3", MemoryPatterns["MCDU-3"], +0x6B0, 4, "int"); //6A0?
            AddMemoryValue("speedVAPP-4", MemoryPatterns["MCDU-4"], +0xC08, 4, "int");
            AddMemoryValue("speedVAPP-5", MemoryPatterns["MCDU-5"], +0x224, 4, "int");
            AddMemoryValue("speedVAPP-6", MemoryPatterns["MCDU-2"], -0x14ADA0, 4, "int");


            //// STRING VALUES - StreamDeck
            if (!App.rawValues)
            {
                foreach (var def in Definitions)
                    AddIpcOffset(def.ID, def.Type, def.Size, def.Offset);
            }
            //// RAW VALUES (Offset)
            else if (!App.useLvars)
            {
                foreach (var def in Definitions)
                    AddIpcOffset(def.ID, def.Type, def.Size, def.Offset);
            }
            //// RAW VALUES (L-Var)
            else
            {
                foreach (var def in Definitions)
                    AddIpcLvar(def.ID);
            }

            ////TO L-Vars
            AddIpcLvar("speedV1");
            AddIpcLvar("speedVR");
            AddIpcLvar("speedV2");
            AddIpcLvar("speedVAPP");

            IPCManager.SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            IPCManager.SimConnect.SubscribeLvar("S_OH_IN_LT_ANN_LT");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_VERTICAL_SPEED");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_ALTITUDE");
            IPCManager.SimConnect.SubscribeLvar("E_FCU_VS");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_TRACK_FPA_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_HEADING_VS_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_SPEED_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_SPEED_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_HEADING_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_ALTITUDE_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_ALTITUDE_SCALE");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_EFIS1_BARO_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_CDU1_FM");            
            string com = "1";
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_VOR");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_ILS");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_ADF");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_HF1");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_HF2");
            com = "2";
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_VOR");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_ILS");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_ADF");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_HF1");
            IPCManager.SimConnect.SubscribeLvar($"I_PED_RMP{com}_HF2");
        }

        private void AddMemoryValue(string id, MemoryPattern pattern, long offset, int size, string type, bool castInt = false)
        {
            MemoryValues.Add(id, new MemoryValue(id, pattern, offset, size, type, castInt));
        }

        private void AddIpcOffset(string id, string type, int size, int offset)
        {
            IPCValues.Add(id, new IPCValueOffset(id, offset, type, size));
        }

        private void AddIpcLvar(string id)
        {
            IPCValues.Add(id, new IPCValueLvar(id));
        }

        private void InitializeScanner()
        {
            Process fenixProc = Process.GetProcessesByName(App.FenixExecutable).FirstOrDefault();
            if (fenixProc != null)
            {
                Scanner = new MemoryScanner(fenixProc);
            }
            else
            {
                throw new NullReferenceException("Fenix Proc is NULL!");
            }

            Logger.Log(LogLevel.Information, "ElementManager:InitializeScanner", $"Running Pattern Scan ... (Patterns#: {MemoryPatterns.Count})");
            Scanner.SearchPatterns(MemoryPatterns.Values.ToList());
        }

        public void Dispose()
        {
            foreach (var value in IPCValues.Values)
            {
                if (!App.useLvars && value is IPCValueOffset)
                    (value as IPCValueOffset).Offset.Disconnect();                    
            }

            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
                IPCManager.SimConnect.UnsubscribeAll();

            IPCValues.Clear();

            foreach (var value in MemoryValues.Values)
            {
                value.Dispose();
            }
            MemoryValues.Clear();
            MemoryPatterns.Clear();

            Scanner = null;

            GC.SuppressFinalize(this);
        }

        private void CheckFCU()
        {
            fcuIsPowered = IPCManager.SimConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 1 || IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1;
            isSpdManaged = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MANAGED") == 1;
            isAltManaged = IPCManager.SimConnect.ReadLvar("I_FCU_ALTITUDE_MANAGED") == 1;
            isHdgManaged = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_MANAGED") == 1;
            int alt = (int)(MemoryValues["fcuAlt"].GetValue() ?? 0);
            int spd = (int)(MemoryValues["fcuSpd"].GetValue() ?? 0);
            int hdg = (int)(MemoryValues["fcuHdg"].GetValue() ?? 0);

            if (alt < 100 || alt > 49000 || spd < 0 || spd > 401 || hdg < 0 || hdg > 361)
            {
                Logger.Log(LogLevel.Information, "ElementManager:CheckFCU", $"Memory Locations changed (FCU-Check)! Rescanning ...");
                System.Threading.Thread.Sleep(500);
                Rescan();
            }
        }

        private void CheckMCDU()
        {
            if (firstUpdate && fcuIsPowered)
            {
                Logger.Log(LogLevel.Information, "ElementManager:CheckMCDU", $"FCU already powered, skip Rescans.");
                perfWasSet = true;
            }

            if (!firstUpdate && !perfWasSet && isSpdManaged && isHdgManaged)
            {
                Logger.Log(LogLevel.Information, "ElementManager:CheckMCDU", $"PERF was set (SPD & HDG are managed)! Rescanning ...");
                System.Threading.Thread.Sleep(500);
                Rescan();
                perfWasSet = true;
            }

        }

        private void Rescan()
        {
            foreach (var pattern in MemoryPatterns.Values)
                pattern.Location = 0;
            InitializeScanner();
            Scanner.UpdateBuffers(MemoryValues);
        }

        public bool GenerateValues()
        {
            try
            {
                if (!Scanner.UpdateBuffers(MemoryValues))
                {
                    Logger.Log(LogLevel.Error, "ElementManager:GenerateValues", $"UpdateBuffers() failed");
                    return false;
                }
                CheckFCU();
                CheckMCDU();

                isLightTest = IPCManager.SimConnect.ReadLvar("S_OH_IN_LT_ANN_LT") == 2;
                UpdateFMA();
                UpdateFCU();
                UpdateISIS();
                UpdateCom("1");
                UpdateCom("2");
                UpdateXpdr();
                if (!App.ignoreBatteries)
                    UpdateBatteries();
                UpdateRudder();
                UpdateClock();
                UpdateSpeeds();

                if (!App.useLvars)
                    FSUIPCConnection.Process(App.groupName);

                if (firstUpdate)
                    firstUpdate = false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateFMA()
        {
            int vsVal = MemoryValues["fcuVs"].GetValue() ?? 0;
            int vsValMng = MemoryValues["fcuVsManaged"].GetValue() ?? 0;
            bool isDashed = MemoryValues["fcuVsDashed"].GetValue();
            if (!firstUpdate && !isAltVsMode && vsVal != 0 && lastVSval != vsVal && vsValMng == 0 && !isDashed)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (VS-Value (FMA) changed when isAltVsMode was false)");
            }
            lastVSval = vsVal;

            bool lastOnGround = simOnGround;
            simOnGround = IPCManager.SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            if (!isSpdManaged && !isHdgManaged && simOnGround && !fcuNotInitialized)
            {
                fcuNotInitialized = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting fcuNotInitialized to TRUE");
            }
            else if (!simOnGround && fcuNotInitialized)
            {
                fcuNotInitialized = false;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting fcuNotInitialized to FALSE (not on Ground)");
            }
            else if ((isSpdManaged || isHdgManaged) && fcuNotInitialized)
            {
                fcuNotInitialized = false;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting fcuNotInitialized to FALSE (SPD or HDG managed)");
            }

            if (!firstUpdate && simOnGround && !lastOnGround)
            {
                hasLanded = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting hasLanded to TRUE");
            }
            else if (!simOnGround && hasLanded)
            {
                hasLanded = false;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting hasLanded to FALSE");
            }

            if (hasLanded && !isSpdManaged && !isHdgManaged && perfWasSet)
            {
                perfWasSet = false;
                speedV1 = 0;
                speedVR = 0;
                speedV2 = 0;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting perfWasSet to FALSE");
            }

            float switchAlt = IPCManager.SimConnect.ReadLvar("S_FCU_ALTITUDE");
            if (switchAlt != lastSwitchAlt || isDashed)
            {
                isAltVsMode = false;
                if (lastVSdashed != isDashed)
                    Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to FALSE (Alt-Knob or Dashed)");
            }
            lastSwitchAlt = switchAlt;

            float switchVS = IPCManager.SimConnect.ReadLvar("S_FCU_VERTICAL_SPEED");
            if (switchVS != lastSwitchVS && !firstUpdate)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (VS-Knob)");
            }
            lastSwitchVS = switchVS;           
            
            if (!isAltManaged && isAltManaged != lastALTmanaged && lastVSdashed != isDashed && !isDashed && !firstUpdate)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (Alt-Dot changed and Dashed changed)");
            }

            if (isHdgManaged != lastHDGmanaged && lastVSdashed != isDashed && !isDashed)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (Hdg-Dot changed and Dashed changed)");
            }

            lastHDGmanaged = isHdgManaged;
            lastALTmanaged = isAltManaged;
            lastVSdashed = isDashed;

            if (!App.rawValues)
            {
                if (isAltVsMode)
                    IPCValues["isVsActive"].SetValue("1");
                else
                    IPCValues["isVsActive"].SetValue("0");
            }
            else
            {
                if (isAltVsMode)
                    IPCValues["isVsActive"].SetValue((byte)1);
                else
                    IPCValues["isVsActive"].SetValue((byte)0);
            }
        }

        private void UpdateFCU()
        {
            if (App.rawValues)
            {
                UpdateRawFCU();
                return;
            }

            bool isModeHdgVs = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1;
            bool isModeSpd = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MODE") == 1;
            bool isAltHundred = IPCManager.SimConnect.ReadLvar("S_FCU_ALTITUDE_SCALE") == 0;
            
            //SPEED
            string result = "";
            if (fcuIsPowered)
            {
                if (isLightTest)
                {
                    if (App.addFcuMode)
                        result = "8888\n888*";
                    else
                        result = "888*";
                }
                else
                {
                    if (App.addFcuMode)
                    {
                        if (isModeSpd)
                            result = "SPD\n";
                        else
                            result = "MACH\n";
                    }

                    int value;
                    if (isSpdManaged)
                        value = MemoryValues["fcuSpdManaged"].GetValue();
                    else
                        value = (int)Math.Round(MemoryValues["fcuSpd"].GetValue());

                    if (MemoryValues["fcuSpdDashed"].GetValue())
                        result += "---*";
                    else
                    {
                        if (isModeSpd)
                            result += value.ToString("D3");
                        else
                            result += "." + value.ToString();

                        if (isSpdManaged)
                            result += "*";
                    }
                }
            }
            IPCValues["fcuSpdStr"].SetValue(result);

            //HDG
            result = "";
            if (fcuIsPowered)
            {
                if (isLightTest)
                {
                    if (App.addFcuMode)
                        result = "888\n888*";
                    else
                        result = "888*";
                }
                else
                {
                    if (App.addFcuMode)
                    {
                        if (isModeHdgVs)
                            result = "HDG\n";
                        else
                            result = "TRK\n";
                    }

                    bool isHdgDashed = MemoryValues["fcuHdgDashed"].GetValue();

                    string hdgMng = MemoryValues["fcuHdgManaged"].GetValue()?.ToString("D3") ?? "000";
                    string hdg = MemoryValues["fcuHdg"].GetValue()?.ToString("D3") ?? "000";
                    string value = hdg;
                    if (isHdgManaged && !isHdgDashed && hdgMng != "000")
                        value = hdgMng;


                    if (isHdgDashed)
                        result += "---*";
                    else
                    {
                        result += value;
                        if (isHdgManaged)
                        {
                            result += "*";
                        }
                    }
                }
            }
            IPCValues["fcuHdgStr"].SetValue(result);

            //ALT
            result = "";
            if (fcuIsPowered)
            {
                if (isLightTest)
                    result = "88888*";
                else
                {
                    result = MemoryValues["fcuAlt"].GetValue()?.ToString("D5") ?? "00100";
                    if (isAltHundred && !string.IsNullOrEmpty(App.altScaleDelim))
                        result = result.Insert(2, App.altScaleDelim);
                    if (isAltManaged)
                        result += "*";
                }
            }
            IPCValues["fcuAltStr"].SetValue(result);

            //VS
            result = "";
            if (fcuIsPowered)
            {
                if (isLightTest)
                {
                    if (App.addFcuMode)
                        result = "888\n+8888";
                    else
                        result = "+8888";
                }
                else
                {
                    if (App.addFcuMode)
                    {
                        if (isModeHdgVs)
                            result = "V/S\n";
                        else
                            result = "FPA\n";
                    }

                    int vs = MemoryValues["fcuVsManaged"].GetValue() ?? 0;
                    if (isAltVsMode && !fcuNotInitialized)
                    {
                        vs = MemoryValues["fcuVs"].GetValue() ?? 0;
                    }

                    if (MemoryValues["fcuVsDashed"].GetValue())
                        result += "-----";
                    else if (isModeHdgVs)
                    {
                        if (vs >= 0)
                            result += "+";

                        if (!App.ooMode)
                            result += vs.ToString("D4");
                        else
                        {
                            string tmp = vs.ToString("D4");
                            if (vs >= 0)
                                result += tmp[0..2] + "oo";
                            else
                                result += tmp[0..3] + "oo";
                        }
                    }
                    else //fpa
                    {
                        float fpa = vs / 1000.0f;
                        if (fpa >= 0.0f)
                            result += "+";

                        result += fpa.ToString("F1", formatInfo);
                    }
                }
            }
            IPCValues["fcuVsStr"].SetValue(result);
        }

        private void UpdateRawFCU()
        {
            //SPD
            bool isSpdManaged = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MANAGED") == 1;

            float fvalue;
            if (isSpdManaged)
                fvalue = MemoryValues["fcuSpdManaged"].GetValue();
            else
                fvalue = (int)Math.Round(MemoryValues["fcuSpd"].GetValue());

            IPCValues["fcuSpd"].SetValue(fvalue);
            if (MemoryValues["fcuSpdDashed"].GetValue())
                IPCValues["fcuSpdDashed"].SetValue((byte)1);
            else
                IPCValues["fcuSpdDashed"].SetValue((byte)0);

            //HDG
            bool isHdgManaged = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_MANAGED") == 1;
            bool isHdgDashed = MemoryValues["fcuHdgDashed"].GetValue();

            int hdgMng = MemoryValues["fcuHdgManaged"].GetValue() ?? 0;
            int hdg = MemoryValues["fcuHdg"].GetValue() ?? 0;
            int ivalue = hdg;
            if (isHdgManaged && !isHdgDashed && hdgMng != 0)
                ivalue = hdgMng;

            IPCValues["fcuHdg"].SetValue(ivalue);
            if (MemoryValues["fcuHdgDashed"].GetValue())
                IPCValues["fcuHdgDashed"].SetValue((byte)1);
            else
                IPCValues["fcuHdgDashed"].SetValue((byte)0);


            //ALT
            IPCValues["fcuAlt"].SetValue((int)(MemoryValues["fcuAlt"].GetValue() ?? 100));

            //VS
            bool isModeHdgVs = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1;

            float vs = MemoryValues["fcuVsManaged"].GetValue() ?? 0;
            if (!MemoryValues["fcuVsDashed"].GetValue())
            {
                if (isAltVsMode && !fcuNotInitialized)
                    vs = MemoryValues["fcuVs"].GetValue() ?? 0;
                if (!isModeHdgVs)
                    vs = (float)Math.Round(vs / 1000.0f, 1);
            }
            else
                vs = 0.0f;

            IPCValues["fcuVs"].SetValue(vs);
            if (MemoryValues["fcuVsDashed"].GetValue())
                IPCValues["fcuVsDashed"].SetValue((byte)1);
            else
                IPCValues["fcuVsDashed"].SetValue((byte)0);

        }

        private void UpdateISIS()
        {
            double baro = MemoryValues["isisBaro1"].GetValue();
            bool std = MemoryValues["isisStd1"].GetValue();
            if (baro < 800 || baro > 1200)
            {
                baro = MemoryValues["isisBaro2"].GetValue();
                std = MemoryValues["isisStd2"].GetValue();
                if (baro < 800 || baro > 1200)
                {
                    baro = MemoryValues["isisBaro3"].GetValue();
                    std = MemoryValues["isisStd3"].GetValue();
                }
            }

            if (!App.rawValues)
            {
                string result;
                if (std)
                    result = "STD";
                else
                {
                    bool isHpa = IPCManager.SimConnect.ReadLvar("S_FCU_EFIS1_BARO_MODE") == 1;
                    if (isHpa)
                    {
                        baro = Math.Round(baro, 0);
                        result = string.Format("{0,4:0000}", baro);
                    }
                    else
                    {
                        baro = Math.Round(baro * 0.029529983071445, 2);
                        result = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", baro);
                    }
                }

                IPCValues["isisStr"].SetValue(result);
            }
            else
            {
                IPCValues["isisStd"].SetValue(std ? (byte)1 : (byte)0);
                IPCValues["isisBaro"].SetValue((float)baro);
            }
        }

        private void UpdateCom(string com)
        {
            int valueActive = MemoryValues[$"com{com}Active"].GetValue() ?? 0;
            int valueStandby = MemoryValues[$"com{com}Standby"].GetValue() ?? 0;

            bool courseMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_VOR") == 1 || IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_ILS") == 1;
            bool adfMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_ADF") == 1;
            bool hfMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_HF1") == 1 || IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_HF2") == 1;

            if (!App.rawValues)
            {
                if (isLightTest)
                {
                    IPCValues[$"com{com}ActiveStr"].SetValue("888888");
                    IPCValues[$"com{com}StandbyStr"].SetValue("888888");
                }
                else if (courseMode)
                {
                    if (valueActive > 0)
                    {
                        IPCValues[$"com{com}ActiveStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0:F3}", valueActive / 1000.0f));
                        if (valueStandby < 360)
                            IPCValues[$"com{com}StandbyStr"].SetValue("C-" + string.Format(new CultureInfo("en-US"), "{0,3:F0}", valueStandby).Replace(' ','0'));
                        else
                            IPCValues[$"com{com}StandbyStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0:F3}", valueStandby / 1000.0f));
                    }
                    else
                    {
                        IPCValues[$"com{com}ActiveStr"].SetValue("");
                        IPCValues[$"com{com}StandbyStr"].SetValue("");
                    }

                }
                else if (adfMode)
                {
                    if (valueActive > 0)
                        IPCValues[$"com{com}ActiveStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0,4:F1}", valueActive / 100.0f).Replace(' ', '0'));
                    else
                        IPCValues[$"com{com}ActiveStr"].SetValue("");

                    if (valueStandby > 0)
                        IPCValues[$"com{com}StandbyStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0,4:F1}", valueStandby / 100.0f).Replace(' ', '0'));
                    else
                        IPCValues[$"com{com}StandbyStr"].SetValue("");
                }
                else if (hfMode)
                {
                    if (valueActive > 0)
                        IPCValues[$"com{com}ActiveStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0,7:F3}", valueActive / 1000.0f).Replace(' ', '0'));
                    else
                        IPCValues[$"com{com}ActiveStr"].SetValue("");

                    if (valueStandby > 0)
                        IPCValues[$"com{com}StandbyStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0,7:F3}", valueStandby / 1000.0f).Replace(' ', '0'));
                    else
                        IPCValues[$"com{com}StandbyStr"].SetValue("");
                }
                else
                {
                    if (valueActive == 118000)
                        IPCValues[$"com{com}ActiveStr"].SetValue("dAtA");
                    else if (valueActive > 0)
                        IPCValues[$"com{com}ActiveStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0:F3}", valueActive / 1000.0f));
                    else
                        IPCValues[$"com{com}ActiveStr"].SetValue("");

                    if (valueStandby > 0)
                        IPCValues[$"com{com}StandbyStr"].SetValue(string.Format(new CultureInfo("en-US"), "{0:F3}", valueStandby / 1000.0f));
                    else
                        IPCValues[$"com{com}StandbyStr"].SetValue("");
                }
            }
            else
            {
                if (isLightTest)
                {
                    IPCValues[$"com{com}Active"].SetValue(888888);
                    IPCValues[$"com{com}Standby"].SetValue(888888);
                }
                else
                {
                    IPCValues[$"com{com}Active"].SetValue(valueActive);
                    IPCValues[$"com{com}Standby"].SetValue(valueStandby);
                }
            }
        }

        private void UpdateXpdr()
        {
            string result;
            int input = MemoryValues["xpdrInput"].GetValue() ?? 0;
            int disp = MemoryValues["xpdrDisplay"].GetValue() ?? 0;
            int digits = MemoryValues["xpdrDigits"].GetValue() ?? 0;

            if (isLightTest)
                result = "8888";
            else if (digits == 0 || MemoryValues[$"com2Active"].GetValue() == -1)
                result = "";
            else if (input != -1 && digits != 4)
                result = input.ToString($"D{digits}");
            else if (disp >= 0 && disp <= 7777)
                result = disp.ToString($"D{digits}");
            else
                result = "";

            if (!App.rawValues)
                IPCValues["xpdrStr"].SetValue(result);
            else
            {
                IPCValues["xpdrDigits"].SetValue(digits);
                if (result == "")
                    IPCValues["xpdr"].SetValue((short)-1);
                else if (short.TryParse(result, out short shortValue))
                    IPCValues["xpdr"].SetValue(shortValue);
                else
                {
                    IPCValues["xpdr"].SetValue((short)-1);
                }
            }

            //if (input == -1 && !xpdrWasCleared)
            //{
            //    Logger.Log(LogLevel.Information, "ElementManager:UpdateXpdr", $"XPDR was cleared");
            //    xpdrWasCleared = true;
            //}
            //else if (input != -1 && xpdrWasCleared)
            //{
            //    Logger.Log(LogLevel.Information, "ElementManager:UpdateXpdr", $"Reset XPDR cleared");
            //    xpdrWasCleared = false;
            //    xpdrClearedCounter = 0;
            //}

            //if (input == -1 && xpdrWasCleared && xpdrClearedCounter < 100)
            //{
            //    xpdrClearedCounter++;
            //    result = "";
            //}
            //else if (input == -1 && xpdrClearedCounter >= 100)
            //{
            //    result = disp.ToString();
            //}
            //else if ((input < -1 || input > 7777 || input == 0) && disp != 0)
            //{
            //    result = disp.ToString();
            //}
            //else
            //    result = input.ToString();            


            //if (!App.rawValues)
            //    IPCValues["xpdrStr"].SetValue(result);
            //else
            //{
            //    IPCValues["xpdrDigits"].SetValue(digits);
            //    if (short.TryParse(result, out short shortValue))
            //        IPCValues["xpdr"].SetValue(shortValue);
            //    else if (result == "")
            //        IPCValues["xpdr"].SetValue((short)-1);
            //    else
            //    {
            //        IPCValues["xpdr"].SetValue((short)0);
            //    }
            //}
        }

        private void UpdateBatteries()
        {
            double value = MemoryValues["bat1Display"].GetValue() ?? 0.0;
            if (!App.rawValues)
                IPCValues["bat1Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
            else
                IPCValues["bat1"].SetValue((float)Math.Round(value,1));

            value = MemoryValues["bat2Display1"].GetValue() ?? 0.0;
            if (value == 0.0 || MemoryValues["bat2Display1"].IsTinyValue() || double.IsNaN(value))
                value = MemoryValues["bat2Display2"].GetValue() ?? 0.0;

            if (value != 0)
            {
                if (!App.rawValues)
                    IPCValues["bat2Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
                else
                    IPCValues["bat2"].SetValue((float)Math.Round(value,1));
            }
        }

        private bool IsRudderValueValid(string index)
        {
            double value = MemoryValues[index].GetValue() ?? double.NaN;
            return !double.IsNaN(value) && !MemoryValues[index].IsTinyValue() && value >= -20.0 && value <= 20.0;
        }

        private void UpdateRudder()
        { 
            double disp1 = MemoryValues["rudderDisplay1"].GetValue() ?? double.NaN;
            bool disp1Valid = IsRudderValueValid("rudderDisplay1");
            double disp2 = MemoryValues["rudderDisplay2"].GetValue() ?? double.NaN;
            bool disp2Valid = IsRudderValueValid("rudderDisplay2");
            double disp3 = MemoryValues["rudderDisplay3"].GetValue() ?? double.NaN;
            bool disp3Valid = IsRudderValueValid("rudderDisplay3");
            double disp4 = MemoryValues["rudderDisplay4"].GetValue() ?? double.NaN;
            bool disp4Valid = IsRudderValueValid("rudderDisplay4");
            double disp5 = MemoryValues["rudderDisplay5"].GetValue() ?? double.NaN;
            bool disp5Valid = IsRudderValueValid("rudderDisplay5");

            double value = 0.0;
            if (disp1Valid)
            {
                value = disp1;
                if (disp2Valid && value == 0.0 && disp2 != 0.0)
                    value = disp2;
                else if (disp3Valid && value == 0.0 && disp3 != 0.0)
                    value = disp3;
                else if (disp4Valid && value == 0.0 && disp4 != 0.0)
                    value = disp4;
                else if (disp5Valid && value == 0.0 && disp5 != 0.0)
                    value = disp5;
            }
            else if (disp2Valid)
            {
                value = disp2;
                if (disp3Valid && value == 0.0 && disp3 != 0.0)
                    value = disp3;
                else if (disp4Valid && value == 0.0 && disp4 != 0.0)
                    value = disp4;
                else if (disp5Valid && value == 0.0 && disp5 != 0.0)
                    value = disp5;
            }
            else if (disp3Valid)
            {
                value = disp3;
                if (disp4Valid && value == 0.0 && disp4 != 0.0)
                    value = disp4;
                else if (disp5Valid && value == 0.0 && disp5 != 0.0)
                    value = disp5;
            }
            else if (disp4Valid)
            {
                value = disp4;
                if (disp5Valid && value == 0.0 && disp5 != 0.0)
                    value = disp5;
            }
            else if (disp5Valid)
            {
                value = disp5;
            }

            bool power = MemoryValues[$"com2Active"].GetValue() != -1;
            value = Math.Round(value, 2);
            if (!App.rawValues)
            {
                string result;
                if (power)
                {
                    string space = (Math.Abs(value) >= 10 ? "" : " ");
                    if (value <= -0.1)
                        result = "L" + space;
                    else if (value >= 0.1)
                        result = "R" + space;
                    else
                        result = " " + space;
                    result += string.Format(formatInfo, "{0:F1}", Math.Abs(value));
                }
                else
                    result = "";

                IPCValues["rudderStr"].SetValue(result);
            }
            else
            {
                IPCValues["rudder"].SetValue((float)Math.Round(value, 1));
            }
        }

        private void UpdateClock()
        {
            UpdateClock("clockCHR", "clockChrStr", "clockChr");
            UpdateClock("clockET", "clockEtStr", "clockEt");
        }

        private void UpdateClock(string memName, string strName, string numName)
        {
            int num = MemoryValues[memName].GetValue();
            int upper = num / 100;
            int lower = num % 100;

            if (!App.rawValues)
            {
                if (isLightTest)
                    IPCValues[strName].SetValue("88:88");
                else if (num != -1)
                    IPCValues[strName].SetValue(string.Format("{0:D2}:{1:D2}", upper, lower));
                else
                    IPCValues[strName].SetValue("");
            }
            else
            {
                if (isLightTest)
                    IPCValues[numName].SetValue(8888);
                else
                    IPCValues[numName].SetValue(num);
            }
        }

        private bool SpeedIsValid(int speed)
        {
            return speed >= 80 && speed <= 180;
        }

        private bool SpeedsAreValid(int v1, int v2, int vr)
        {
            return SpeedIsValid(v1) && SpeedIsValid(v2) && SpeedIsValid(vr);
        }

        private bool SpeedLocationIsValid (string suffix)
        {
            int v1 = MemoryValues[$"speedV1-{suffix}"].GetValue() ?? 0;
            int vr = MemoryValues[$"speedVR-{suffix}"].GetValue() ?? 0;
            int v2 = MemoryValues[$"speedV2-{suffix}"].GetValue() ?? 0;

            return SpeedsAreValid(v1, v2, vr);
        }

        private void UpdateSpeeds()
        {
            int v1 = MemoryValues["speedV1-1"].GetValue() ?? 0;
            int vr = MemoryValues["speedVR-1"].GetValue() ?? 0;
            int v2 = MemoryValues["speedV2-1"].GetValue() ?? 0;
            int vapp = MemoryValues["speedVAPP-1"].GetValue() ?? -1;

            if (!SpeedsAreValid(v1, v2, vr))
            {
                v1 = MemoryValues["speedV1-2"].GetValue() ?? 0;
                vr = MemoryValues["speedVR-2"].GetValue() ?? 0;
                v2 = MemoryValues["speedV2-2"].GetValue() ?? 0;
                vapp = MemoryValues["speedVAPP-2"].GetValue() ?? -1;
            }
            else
            {
                speedV1 = v1;
                speedVR = vr;
                speedV2 = v2;
            }

            if (!SpeedsAreValid(v1, v2, vr))
            {
                v1 = MemoryValues["speedV1-3"].GetValue() ?? 0;
                vr = MemoryValues["speedVR-3"].GetValue() ?? 0;
                v2 = MemoryValues["speedV2-3"].GetValue() ?? 0;
                vapp = MemoryValues["speedVAPP-3"].GetValue() ?? -1;
            }
            else
            {
                speedV1 = v1;
                speedVR = vr;
                speedV2 = v2;
            }

            if (!SpeedsAreValid(v1, v2, vr))
            {
                v1 = MemoryValues["speedV1-4"].GetValue() ?? 0;
                vr = MemoryValues["speedVR-4"].GetValue() ?? 0;
                v2 = MemoryValues["speedV2-4"].GetValue() ?? 0;
                vapp = MemoryValues["speedVAPP-4"].GetValue() ?? -1;
            }
            else
            {
                speedV1 = v1;
                speedVR = vr;
                speedV2 = v2;
            }

            if (!SpeedsAreValid(v1, v2, vr))
            {
                v1 = MemoryValues["speedV1-5"].GetValue() ?? 0;
                vr = MemoryValues["speedVR-5"].GetValue() ?? 0;
                v2 = MemoryValues["speedV2-5"].GetValue() ?? 0;
                vapp = MemoryValues["speedVAPP-5"].GetValue() ?? -1;
            }
            else
            {
                speedV1 = v1;
                speedVR = vr;
                speedV2 = v2;
            }

            if (!SpeedsAreValid(v1, v2, vr))
            {
                v1 = MemoryValues["speedV1-6"].GetValue() ?? 0;
                vr = MemoryValues["speedVR-6"].GetValue() ?? 0;
                v2 = MemoryValues["speedV2-6"].GetValue() ?? 0;
                vapp = MemoryValues["speedVAPP-6"].GetValue() ?? -1;
            }
            else
            {
                speedV1 = v1;
                speedVR = vr;
                speedV2 = v2;
            }

            if (!SpeedsAreValid(v1, v2, vr) && SpeedsAreValid(speedV1, speedV2, speedVR))
            {
                v1 = speedV1;
                vr = speedVR;
                v2 = speedV2;
                vapp = -1;
            }

            IPCValues["speedV1"].SetValue(v1);
            IPCValues["speedVR"].SetValue(vr);
            IPCValues["speedV2"].SetValue(v2);
            IPCValues["speedVAPP"].SetValue(vapp);
        }

        public void PrintReport()
        {
            foreach (var value in MemoryValues.Values)
            {
                ulong location = MemoryScanner.CalculateLocation(value.Pattern.Location, value.PatternOffset);
                Logger.Log(LogLevel.Information, "ElementManager:PrintReport", $"MemoryValue <{value.ID}> is at Address 0x{location:X} ({location:d})");
            }
        }
    }
}
