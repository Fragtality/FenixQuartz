using FSUIPC;
using Serilog;
using System.Globalization;

namespace PilotsDeck_FNX2PLD
{
    public class ElementManager : IDisposable
    {
        public Dictionary<string, MemoryPattern> MemoryPatterns;
        public Dictionary<string, MemoryValue> MemoryValues;

        private Dictionary<string, IPCOffset> IPCOffsets;
        private int nextOffset = Program.offsetBase;
        private static NumberFormatInfo formatInfo = new CultureInfo("en-US").NumberFormat;
        private bool firstUpdate = true;

        private double lastSwitchVS;
        private int lastValueVS = 0;
        private bool isAltVs = false;
        private double lastSwitchAlt;
        private bool isLightTest = false;

        public ElementManager()
        {
            IPCOffsets = new ();
            MemoryValues = new ();

            //// MEMORY PATTERNS
            MemoryPatterns = new()
            {
                { "FCU-1", new MemoryPattern("46 00 43 00 55 00 20 00 70 00 6F 00 77 00 65 00 72 00 20 00 69 00 6E 00 70 00 75 00 74 00") },
                { "ISIS-1", new MemoryPattern("49 00 53 00 49 00 53 00 20 00 70 00 6F 00 77 00 65 00 72 00 65 00 64 00") },
                { "COM1-1", new MemoryPattern("00 00 00 00 D3 01 00 00 FF FF FF FF 00 00 00 00 00 00 00 00") },
                { "XPDR-1", new MemoryPattern("58 00 50 00 44 00 52 00 20 00 63 00 68 00 61 00 72 00 61 00 63 00 74 00 65 00 72 00 73 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 65 00 64") },
                { "BAT1-1", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80") },
                { "BAT2-1", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 32 00 00 00") },
                { "RUDDER-1", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "RUDDER-2", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00", 2) }
            };


            //// MEMORY VALUES
            //FCU
            AddMemoryValue("fcuSpd", MemoryPatterns["FCU-1"], -0x6C, 8, "double");
            AddMemoryValue("fcuHdgDisplay", MemoryPatterns["FCU-1"], -0x20, 4, "int");
            AddMemoryValue("fcuHdgFma", MemoryPatterns["FCU-1"], -0x60, 4, "int");
            AddMemoryValue("fcuAlt", MemoryPatterns["FCU-1"], -0x5C, 4, "int");
            AddMemoryValue("fcuVsDisplay", MemoryPatterns["FCU-1"], -0x18, 4, "int");
            AddMemoryValue("fcuVsFma", MemoryPatterns["FCU-1"], -0x64, 4, "int");

            //ISIS
            AddMemoryValue("isisStd", MemoryPatterns["ISIS-1"], -0xC7, 1, "bool");
            AddMemoryValue("isisBaro", MemoryPatterns["ISIS-1"], - 0xEC, 8, "double");

            //COM
            AddMemoryValue("comStandby", MemoryPatterns["COM1-1"], -0xC, 4, "int");
            AddMemoryValue("comActive", MemoryPatterns["COM1-1"], -0x24, 4, "int");

            //XPDR
            AddMemoryValue("xpdrDisplay", MemoryPatterns["XPDR-1"], -0x110, 2, "int");
            AddMemoryValue("xpdrInput", MemoryPatterns["XPDR-1"], -0x10C, 2, "int");
            AddMemoryValue("xpdrInput2", MemoryPatterns["XPDR-1"], -0x124, 2, "int");

            //BAT1
            AddMemoryValue("bat1Display", MemoryPatterns["BAT1-1"], -0x2C, 8, "double");

            //BAT2
            AddMemoryValue("bat2Display1", MemoryPatterns["BAT2-1"], -0x284, 8, "double");
            AddMemoryValue("bat2Display2", MemoryPatterns["BAT2-1"], -0x35C, 8, "double");

            //RUDDER
            AddMemoryValue("rudderDisplay1", MemoryPatterns["RUDDER-1"], 0x8C, 8, "double");
            AddMemoryValue("rudderDisplay2", MemoryPatterns["RUDDER-2"], 0x8C, 8, "double");


            //// IPC VALUES - StreamDeck
            if (!Program.rawValues)
            {
                //FCU
                AddIpcOffset("fcuSpdStr", "string", 10);
                AddIpcOffset("fcuHdgStr", "string", 9);
                AddIpcOffset("fcuAltStr", "string", 7);
                AddIpcOffset("fcuVsStr", "string", 10);

                //ISIS
                AddIpcOffset("isisStr", "string", 6);

                //COM standby
                AddIpcOffset("comStandbyStr", "string", 8);

                //XPDR
                AddIpcOffset("xpdrStr", "string", 5);

                //BAT1
                AddIpcOffset("bat1Str", "string", 5);

                //BAT2
                AddIpcOffset("bat2Str", "string", 5);

                //RUDDER
                AddIpcOffset("rudderStr", "string", 6);

                //VS Selected
                AddIpcOffset("isAltVs", "string", 2);

                //COM active
                AddIpcOffset("comActiveStr", "string", 8);
            }
            //// IPC VALUES - Raw Mode
            else
            {
                //FCU
                AddIpcOffset("fcuSpd", "float", 4);
                AddIpcOffset("fcuHdg", "int", 4);
                AddIpcOffset("fcuAlt", "int", 4);
                AddIpcOffset("fcuVs", "float", 4);

                //ISIS
                AddIpcOffset("isisStd", "byte", 1);
                AddIpcOffset("isisBaro", "float", 4);

                //COM standby
                AddIpcOffset("comActive", "int", 4);
                AddIpcOffset("comStandby", "int", 4);

                //XPDR
                AddIpcOffset("xpdr", "short", 2);

                //BAT1
                AddIpcOffset("bat1", "float", 4);

                //BAT2
                AddIpcOffset("bat2", "float", 4);

                //RUDDER
                AddIpcOffset("rudder", "float", 4);
            }
        }

        private void AddMemoryValue(string id, MemoryPattern pattern, long offset, int size, string type, bool castInt = false)
        {
            MemoryValues.Add(id, new MemoryValue(id, pattern, offset, size, type, castInt));
        }

        private void AddIpcOffset(string id, string type, int size)
        {
            IPCOffsets.Add(id, new IPCOffset(id, nextOffset, type, size));
            nextOffset += size;
        }

        public void Dispose()
        {
            foreach (var offset in IPCOffsets.Values)
            {
                if (offset != null && offset.Offset != null)
                    offset.Offset.Disconnect();
            }
            IPCOffsets.Clear();

            foreach (var value in MemoryValues.Values)
            {
                value.Dispose();
            }
            MemoryValues.Clear();
            MemoryPatterns.Clear();
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
            isLightTest = IPCManager.ReadLVar("S_OH_IN_LT_ANN_LT") == 2;

            UpdateFMA();
            UpdateFCU();
            UpdateISIS();
            UpdateCom();
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

            if (!Program.rawValues)
            {
                if (isAltVs)
                    IPCOffsets["isAltVs"].SetValue("1");
                else
                    IPCOffsets["isAltVs"].SetValue("0");
            }
        }

        private void UpdateFCU()
        {
            if (Program.rawValues)
            {
                UpdateRawFCU();
                return;
            }

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
                            result += ((int)Math.Round(MemoryValues["fcuSpd"].GetValue())).ToString();
                        else
                            result += "." + ((int)Math.Round(MemoryValues["fcuSpd"].GetValue())).ToString();
                    }
                }
            }
            IPCOffsets["fcuSpdStr"].SetValue(result);

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

                    string hdgDisp = MemoryValues["fcuHdgDisplay"].GetValue()?.ToString("D3") ?? "000";
                    string hdgFma = MemoryValues["fcuHdgFma"].GetValue()?.ToString("D3") ?? "000";

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
            IPCOffsets["fcuHdgStr"].SetValue(result);

            //ALT
            result = "";
            if (isFcuPowered)
            {
                if (isLightTest)
                    result = "88888*";
                else
                {
                    result = MemoryValues["fcuAlt"].GetValue()?.ToString("D5") ?? "00100";
                    if (isAltHundred)
                        result = result.Insert(2, " ");
                    if (isAltManaged)
                        result += "*";
                }
            }
            IPCOffsets["fcuAltStr"].SetValue(result);

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

                    int vs = MemoryValues["fcuVsDisplay"].GetValue() ?? 0;
                    if (isAltVs)
                        vs = MemoryValues["fcuVsFma"].GetValue() ?? 0;

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
            IPCOffsets["fcuVsStr"].SetValue(result);
        }

        private void UpdateRawFCU()
        {
            //SPD
            IPCOffsets["fcuSpd"].SetValue((float)Math.Round(MemoryValues["fcuSpd"].GetValue()));

            //HDG
            bool isHdgManaged = IPCManager.ReadLVar("I_FCU_HEADING_MANAGED") == 1;

            if (isHdgManaged)
                IPCOffsets["fcuHdg"].SetValue((int)(MemoryValues["fcuHdgDisplay"].GetValue() ?? 0));
            else
                IPCOffsets["fcuHdg"].SetValue((int)(MemoryValues["fcuHdgFma"].GetValue() ?? 0));

            //ALT
            IPCOffsets["fcuAlt"].SetValue((int)(MemoryValues["fcuAlt"].GetValue() ?? 100));

            //VS
            bool isModeHdgVs = IPCManager.ReadLVar("I_FCU_HEADING_VS_MODE") == 1;
            float vs = MemoryValues["fcuVsDisplay"].GetValue() ?? 0;
            if (isAltVs)
                vs = MemoryValues["fcuVsFma"].GetValue() ?? 0;
            if (!isModeHdgVs)
                vs /= 1000.0f;

            IPCOffsets["fcuVs"].SetValue(vs);
        }

        private void UpdateISIS()
        {
            if (!Program.rawValues)
            {
                string result;
                if (MemoryValues["isisStd"].GetValue() == true)
                    result = "STD";
                else
                {
                    bool isHpa = IPCManager.ReadLVar("S_FCU_EFIS1_BARO_MODE") == 1;
                    if (isHpa)
                    {
                        double tmp = MemoryValues["isisBaro"].GetValue() ?? 0.0;
                        tmp = Math.Round(tmp, 0);
                        result = string.Format("{0,4:0000}", tmp);
                    }
                    else
                    {
                        double tmp = MemoryValues["isisBaro"].GetValue() ?? 0.0;
                        tmp = Math.Round(tmp * 0.029529983071445, 2);
                        result = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", tmp);
                    }
                }

                IPCOffsets["isisStr"].SetValue(result);
            }
            else
            {
                byte value = (bool)MemoryValues["isisStd"].GetValue()? (byte)1 : (byte)0;
                IPCOffsets["isisStd"].SetValue(value);
                IPCOffsets["isisBaro"].SetValue((float)MemoryValues["isisBaro"].GetValue());
            }
        }

        private void UpdateCom()
        {
            int valueStandby = MemoryValues["comStandby"].GetValue() ?? 0;
            int valueActive = MemoryValues["comActive"].GetValue() ?? 0;

            if (!Program.rawValues)
            {
                if (valueStandby > 0)
                    IPCOffsets["comStandbyStr"].SetValue(valueStandby.ToString());
                else
                    IPCOffsets["comStandbyStr"].SetValue("");

                if (valueActive > 0)
                    IPCOffsets["comActiveStr"].SetValue(valueActive.ToString());
                else
                    IPCOffsets["comActiveStr"].SetValue("");
            }
            else
            {
                IPCOffsets["comActive"].SetValue(valueActive);
                IPCOffsets["comStandby"].SetValue(valueStandby);
            }
        }

        private void UpdateXpdr()
        {
            string result = "";
            int value = MemoryValues["xpdrDisplay"].GetValue() ?? 0;
            if (value != 0)
            {
                value = MemoryValues["xpdrInput"].GetValue() ?? 0;
                if (value > 0)
                    result = value.ToString();
                else
                {
                    value = MemoryValues["xpdrDisplay"].GetValue() ?? 0;
                    if (value > 0)
                        result = value.ToString();
                }
            }
            else
            {
                value = MemoryValues["xpdrInput2"].GetValue() ?? 0;
                if (value > 0)
                    result = value.ToString();
            }

            if (!Program.rawValues)
                IPCOffsets["xpdrStr"].SetValue(result);
            else
            {
                if (short.TryParse(result, out short shortValue))
                    IPCOffsets["xpdr"].SetValue(shortValue);
                else
                {
                    IPCOffsets["xpdr"].SetValue((short)0);
                }
            }
        }

        private void UpdateBatteries()
        {
            double value = MemoryValues["bat1Display"].GetValue() ?? 0.0;
            if (!Program.rawValues)
                IPCOffsets["bat1Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
            else
                IPCOffsets["bat1"].SetValue((float)Math.Round(value,1));

            value = MemoryValues["bat2Display1"].GetValue() ?? 0.0;
            if (value < 10.0 && value > 40.0)
                value = MemoryValues["bat2Display2"].GetValue() ?? 0.0;

            if (value != 0)
            {
                if (!Program.rawValues)
                    IPCOffsets["bat2Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
                else
                    IPCOffsets["bat2"].SetValue((float)Math.Round(value,1));
            }
        }

        private void UpdateRudder()
        {
            double value = MemoryValues["rudderDisplay1"].GetValue() ?? 0.0;
            if (value == 0.0)
                value = MemoryValues["rudderDisplay2"].GetValue() ?? 0.0;
            
            value = Math.Round(value, 2);
            if (!Program.rawValues)
            {
                string result;
                if (value <= -0.1)
                    result = "L ";
                else if (value >= 0.1)
                    result = "R ";
                else
                    result = "  ";
                result += string.Format(formatInfo, "{0:F1}", Math.Abs(value));

                IPCOffsets["rudderStr"].SetValue(result);
            }
            else
            {
                IPCOffsets["rudder"].SetValue((float)Math.Round(value, 1));
            }
        }

        public void PrintReport()
        {
            foreach (var value in MemoryValues.Values)
            {
                ulong location = MemoryScanner.CalculateLocation(value.Pattern.Location, value.PatternOffset);
                Log.Information($"ElementManager: MemoryValue <{value.ID}> is at Address 0x{location:X} ({location:d})");
            }

            if (Program.rawValues)
            {
                foreach (var offset in IPCOffsets.Values)
                {
                    Log.Information($"ElementManager: FSUIPC Offset <{offset.ID}> is at Address 0x{offset.Offset.Address:X} (Type: {offset.Type}, Size: {offset.Size})");
                }
            }
            else
            {
                foreach (var offset in IPCOffsets.Values)
                {
                    Log.Information($"ElementManager: FSUIPC Offset <{offset.ID}> is at Address 0x{offset.Offset.Address:X}:s:{offset.Size}");
                }
            }
        }
    }
}
