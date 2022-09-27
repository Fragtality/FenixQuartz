using FSUIPC;
using Serilog;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Windows.Forms.VisualStyles;

namespace PilotsDeck_FNX2PLD
{
    public class ElementManager
    {
        public Dictionary<string, MemoryPattern> Patterns;

        private Dictionary<string, Offset<string>> IPCOffsets;
        private static NumberFormatInfo formatInfo = new CultureInfo("en-US").NumberFormat;
        private bool firstUpdate = true;

        private double lastSwitchVS;
        private int lastValueVS = 0;
        private bool isAltVs = false;
        private double lastSwitchAlt;

        public ElementManager()
        {
            int nextOffset = Program.offsetBase;
            IPCOffsets = new ();

            Patterns = new()
            {
                { "FCU", new MemoryPattern("46 00 43 00 55 00 20 00 70 00 6F 00 77 00 65 00 72 00 20 00 69 00 6E 00 70 00 75 00 74 00") },
                { "ISIS", new MemoryPattern("49 00 53 00 49 00 53 00 20 00 70 00 6F 00 77 00 65 00 72 00 65 00 64 00") },
                { "COM", new MemoryPattern("00 00 00 00 D3 01 00 00 FF FF FF FF FF FF FF FF") },
                { "XPDR", new MemoryPattern("58 00 50 00 44 00 52 00 20 00 63 00 68 00 61 00 72 00 61 00 63 00 74 00 65 00 72 00 73 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 65 00 64") },
                { "BAT1", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80") },
                { "BAT2", new MemoryPattern("61 00 69 00 72 00 63 00 72 00 61 00 66 00 74 00 2E 00 65 00 6C 00 65 00 63 00 74 00 72 00 69 00 63 00 61 00 6C 00 2E 00 62 00 61 00 74 00 74 00 65 00 72 00 79 00 31 00") },
                { "BAT2_2", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 32 00 00 00") },
                { "RUDDER1", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "RUDDER2", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00", 2) }
            };

            //FCU
            nextOffset = AddOffset(Patterns["FCU"], "fcuSpd", -0x6C, 8, "double", 10, nextOffset);
            nextOffset = AddOffset(Patterns["FCU"], "fcuHdgDisplay", -0x20, 4, "int", 9, nextOffset);
            AddOffset(Patterns["FCU"], "fcuHdgFma", -0x60, 4, "int");
            nextOffset = AddOffset(Patterns["FCU"], "fcuAlt", -0x5C, 4, "int", 7, nextOffset);
            nextOffset = AddOffset(Patterns["FCU"], "fcuVsDisplay", -0x18, 4, "int", 10, nextOffset);
            AddOffset(Patterns["FCU"], "fcuVsFma", -0x64, 4, "int");


            //ISIS
            AddOffset(Patterns["ISIS"], "isisStd", -0xC7, 1, "bool", 0);
            nextOffset = AddOffset(Patterns["ISIS"], "isisBaro", -0xEC, 8, "double", 6, nextOffset);

            //COM standby
            nextOffset = AddOffset(Patterns["COM"], "comStandby", -0xC, 4, "int", 8, nextOffset);

            //XPDR
            AddOffset(Patterns["XPDR"], "xpdrDisplay", -0x110, 2, "int");
            nextOffset = AddOffset(Patterns["XPDR"], "xpdrInput", -0x10C, 2, "int", 5, nextOffset);
            AddOffset(Patterns["XPDR"], "xpdrInput2", -0x124, 2, "int");

            //BAT1
            nextOffset = AddOffset(Patterns["BAT1"], "bat1Display", -0x2C, 8, "double", 5, nextOffset);

            //BAT2
            nextOffset = AddOffset(Patterns["BAT2"], "bat2Display", +0x444, 8, "double", 5, nextOffset);
            AddOffset(Patterns["BAT2_2"], "bat2Display2", -0x354, 8, "double");

            //RUDDER
            nextOffset = AddOffset(Patterns["RUDDER1"], "rudderDisplay1", 0x8C, 8, "double", 6, nextOffset);
            AddOffset(Patterns["RUDDER2"], "rudderDisplay2", 0x8C, 8, "double");

            //VS Selected
            IPCOffsets.Add("isAltVs", new Offset<string>(Program.groupName, nextOffset, 2, true));
            Log.Information($"ElementManager: Offset for <isAltVs> is at 0x{nextOffset:X}:{2}:s");
            nextOffset += 2;

            //COM active
            nextOffset = AddOffset(Patterns["COM"], "comActive", -0x24, 4, "int", 8, nextOffset);

            //FC
        }

        public void Dispose()
        {
            foreach (var offset in IPCOffsets.Values)
            {
                offset.Disconnect();
            }
            IPCOffsets.Clear();

            foreach (var pattern in Patterns.Values)
            {
                pattern.Dispose();
            }
            Patterns.Clear();
        }

        private int AddOffset(MemoryPattern pattern, string id, long memOffset, int memSize, string type, int ipcSize = 0, int nextOffset = 0)
        {
            pattern.MemoryOffsets.Add(id, new MemoryOffset(memOffset, memSize, type));
            if (ipcSize > 0)
            {
                IPCOffsets.Add(id, new Offset<string>(Program.groupName, nextOffset, ipcSize, true));
                Log.Information($"ElementManager: Offset for <{id}> is at 0x{nextOffset:X}:{ipcSize}:s");
                return nextOffset + ipcSize;
            }
            else
                return 0;
        }

        public void GenerateValues()
        {
            if (firstUpdate)
            {
                Log.Information($"ElementManager: First Update - Reloading WASM to get all Lvars");
                MSFSVariableServices.Reload();
                firstUpdate = false;
                Thread.Sleep(100);
            }
            bool isLightTest = IPCManager.ReadLVar("S_OH_IN_LT_ANN_LT") == 2;

            UpdateFMA();
            UpdateFCU(Patterns["FCU"], isLightTest);
            UpdateISIS(Patterns["ISIS"]);
            UpdateCom(Patterns["COM"]);
            UpdateXpdr();
            UpdateBatteries();
            UpdateRudder();

            FSUIPCConnection.Process(Program.groupName);
        }

        private void UpdateFMA()
        {
            double switchVS = IPCManager.ReadLVar("S_FCU_VERTICAL_SPEED");
            if (switchVS != lastSwitchVS)
                isAltVs = true;
            lastSwitchVS = switchVS;

            double switchAlt = IPCManager.ReadLVar("S_FCU_ALTITUDE");
            
            if (switchAlt != lastSwitchAlt)
                isAltVs = false;
            lastSwitchAlt = switchAlt;

            if (isAltVs)
                IPCOffsets["isAltVs"].Value = "1";
            else
                IPCOffsets["isAltVs"].Value = "0";
        }

        private void UpdateFCU(MemoryPattern fcu, bool isLightTest)
        {
            bool isModeTrkFpa = IPCManager.ReadLVar("I_FCU_TRACK_FPA_MODE") == 1;
            bool isModeHdgVs = IPCManager.ReadLVar("I_FCU_HEADING_VS_MODE") == 1;
            bool isFcuPowered = isModeHdgVs || isModeTrkFpa;
            bool isModeSpd = IPCManager.ReadLVar("I_FCU_SPEED_MODE") == 1;
            bool isSpdManaged = IPCManager.ReadLVar("I_FCU_SPEED_MANAGED") == 1;
            bool isHdgManaged = IPCManager.ReadLVar("I_FCU_HEADING_MANAGED") == 1;
            bool isAltManaged = IPCManager.ReadLVar("I_FCU_ALTITUDE_MANAGED") == 1;
            bool isAltHundred = IPCManager.ReadLVar("S_FCU_ALTITUDE_SCALE") == 0;

            //SPEED
            string result = "";
            if (isFcuPowered)
            {
                if (isLightTest)
                    result = "8888\n888*";
                else
                {
                    if (isModeSpd)
                        result = "SPD\n";
                    else
                        result = "MACH\n";

                    if (isSpdManaged)
                        result += "---*";
                    else
                    {
                        if (isModeSpd)
                            result += ((int)Math.Round(fcu.MemoryOffsets["fcuSpd"].GetValue())).ToString();
                        else
                            result += "." + ((int)Math.Round(fcu.MemoryOffsets["fcuSpd"].GetValue())).ToString();
                    }
                }
            }
            IPCOffsets["fcuSpd"].Value = result;

            //HDG
            result = "";
            if (isFcuPowered)
            {
                if (isLightTest)
                    result = "888\n888*";
                else
                {
                    if (isModeHdgVs)
                        result = "HDG\n";
                    else
                        result = "TRK\n";

                    string hdgDisp = fcu.MemoryOffsets["fcuHdgDisplay"].GetValue()?.ToString("D3") ?? "000";
                    string hdgFma = fcu.MemoryOffsets["fcuHdgFma"].GetValue()?.ToString("D3") ?? "000";

                    if (isHdgManaged)
                    {
                        if (hdgDisp != "000")
                            result += hdgDisp + "*";
                        else
                            result += "---*";
                    }
                    else
                    {
                        result += hdgFma;
                    }
                }
            }
            IPCOffsets["fcuHdgDisplay"].Value = result;

            //ALT
            result = "";
            if (isFcuPowered)
            {
                if (isLightTest)
                    result = "88888*";
                else
                {
                    result = fcu.MemoryOffsets["fcuAlt"].GetValue()?.ToString("D5") ?? "00100";
                    if (isAltHundred)
                        result = result.Insert(2, " ");
                    if (isAltManaged)
                        result += "*";
                }
            }
            IPCOffsets["fcuAlt"].Value = result;

            //VS
            result = "";
            if (isFcuPowered)
            {
                if (isLightTest)
                    result = "888\n+8888";
                else
                {
                    if (isModeHdgVs)
                        result = "V/S\n";
                    else
                        result = "FPA\n";

                    //int vs = 0;
                    //if (isAltVs)
                    //    vs = fcu.MemoryOffsets["fcuVsFma"].GetValue() ?? 0;
                    //else
                    //    vs = fcu.MemoryOffsets["fcuVsDisplay"].GetValue() ?? 0;

                    //if (!isAltVs)
                    //    result += "-----";
                    int vs = fcu.MemoryOffsets["fcuVsDisplay"].GetValue() ?? 0;
                    bool sourceIsDisplay = vs != 0;
                    if (isAltVs)
                        vs = fcu.MemoryOffsets["fcuVsFma"].GetValue() ?? 0;

                    if (!isAltVs && vs == 0)
                        result += "-----";
                    else if (isModeHdgVs)
                    {
                        if (vs >= 0)
                            result += "+";

                        result += vs.ToString("D4");
                    }
                    else //fpa
                    {
                        float fpa = vs / 1000.0f;
                        if (fpa >= 0.0f)
                            result += "+";

                        result += fpa.ToString("F1", formatInfo);
                    }
                    lastValueVS = vs;
                }
            }
            IPCOffsets["fcuVsDisplay"].Value = result;
        }

        private void UpdateISIS(MemoryPattern isis)
        {
            string result = "";
            if (isis.MemoryOffsets["isisStd"].GetValue() == true)
                result = "STD";
            else
            {
                bool isHpa = IPCManager.ReadLVar("S_FCU_EFIS1_BARO_MODE") == 1;
                if (isHpa)
                {
                    double tmp = isis.MemoryOffsets["isisBaro"].GetValue() ?? 0.0;
                    tmp = Math.Round(tmp, 0);
                    result = string.Format("{0,4:0000}", tmp);
                }
                else
                {
                    double tmp = isis.MemoryOffsets["isisBaro"].GetValue() ?? 0.0;
                    tmp = Math.Round(tmp * 0.029529983071445, 2);
                    result = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", tmp);
                }
            }

            IPCOffsets["isisBaro"].Value = result;
        }

        private void UpdateCom(MemoryPattern com)
        {
            int value = com.MemoryOffsets["comStandby"].GetValue() ?? 0;
            if (value > 0)
                IPCOffsets["comStandby"].Value = value.ToString();
            else
                IPCOffsets["comStandby"].Value = "";

            value = com.MemoryOffsets["comActive"].GetValue() ?? 0;
            if (value > 0)
                IPCOffsets["comActive"].Value = value.ToString();
            else
                IPCOffsets["comActive"].Value = "";
        }

        private void UpdateXpdr()
        {
            int value = Patterns["XPDR"].MemoryOffsets["xpdrDisplay"].GetValue() ?? 0;
            if (value != 0)
            {
                value = Patterns["XPDR"].MemoryOffsets["xpdrInput"].GetValue() ?? 0;
                if (value > 0)
                    IPCOffsets["xpdrInput"].Value = value.ToString();
                else
                {
                    value = Patterns["XPDR"].MemoryOffsets["xpdrDisplay"].GetValue() ?? 0;
                    if (value > 0)
                        IPCOffsets["xpdrInput"].Value = value.ToString();
                    else
                        IPCOffsets["xpdrInput"].Value = "";

                }
            }
            else
            {
                value = Patterns["XPDR"].MemoryOffsets["xpdrInput2"].GetValue() ?? 0;
                if (value > 0)
                    IPCOffsets["xpdrInput"].Value = value.ToString();
                else
                    IPCOffsets["xpdrInput"].Value = "";
            }
        }

        private void UpdateBatteries()
        {
            double value = Patterns["BAT1"].MemoryOffsets["bat1Display"].GetValue() ?? 0.0;
            IPCOffsets["bat1Display"].Value = string.Format(formatInfo, "{0:F1}", value);

            value = Patterns["BAT2"].MemoryOffsets["bat2Display"].GetValue() ?? 0.0;
            if (value != 0)
                IPCOffsets["bat2Display"].Value = string.Format(formatInfo, "{0:F1}", value);
            else
            {
                value = Patterns["BAT2_2"].MemoryOffsets["bat2Display2"].GetValue() ?? 0.0;
                IPCOffsets["bat2Display"].Value = string.Format(formatInfo, "{0:F1}", value);
            }
        }

        private void UpdateRudder()
        {
            double value = Patterns["RUDDER1"].MemoryOffsets["rudderDisplay1"].GetValue() ?? 0.0;
            if (value == 0.0)
                value = Patterns["RUDDER2"].MemoryOffsets["rudderDisplay2"].GetValue() ?? 0.0;
            
            value = Math.Round(value, 2);
            string result;
            if (value <= -0.1)
                result = "L ";
            else if (value >= 0.1)
                result = "R ";
            else
                result = "  ";
            result += string.Format(formatInfo, "{0:F1}", Math.Abs(value));

            IPCOffsets["rudderDisplay1"].Value = result;
        }
    }
}
