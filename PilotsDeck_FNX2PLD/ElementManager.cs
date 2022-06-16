using FSUIPC;
using Serilog;
using System.Globalization;

namespace PilotsDeck_FNX2PLD
{
    public class ElementManager
    {
        public Dictionary<string, MemoryPattern> Patterns;

        private Dictionary<string, Offset<string>> IPCOffsets;
        private static NumberFormatInfo formatInfo = new CultureInfo("en-US").NumberFormat;
        private bool firstUpdate = true;
        //private int nextOffset;

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
                //{ "BAT2", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 32") },
                { "BAT2", new MemoryPattern("61 00 69 00 72 00 63 00 72 00 61 00 66 00 74 00 2E 00 65 00 6C 00 65 00 63 00 74 00 72 00 69 00 63 00 61 00 6C 00 2E 00 62 00 61 00 74 00 74 00 65 00 72 00 79 00 31 00") },
                { "RUDDER1", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "RUDDER2", new MemoryPattern("46 00 43 00 20 00 52 00 75 00 64 00 64 00 65 00 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00", 2) }
            };

            //FCU
            nextOffset = AddOffset(Patterns["FCU"], "fcuSpd", -0x6C, 8, "double", 10, nextOffset);
            nextOffset = AddOffset(Patterns["FCU"], "fcuHdg", -0x60, 4, "int", 9, nextOffset);
            nextOffset = AddOffset(Patterns["FCU"], "fcuAlt", -0x5C, 4, "int", 7, nextOffset);
            nextOffset = AddOffset(Patterns["FCU"], "fcuVS", -0x18, 4, "int", 10, nextOffset);


            //ISIS
            AddOffset(Patterns["ISIS"], "isisStd", -0x67, 1, "bool", 0);
            nextOffset = AddOffset(Patterns["ISIS"], "isisBaro", -0x8C, 8, "double", 5, nextOffset);

            //COM
            nextOffset = AddOffset(Patterns["COM"], "comStandby", -0xC, 4, "int", 8, nextOffset);

            //XPDR
            AddOffset(Patterns["XPDR"], "xpdrDisplay", -0x110, 2, "int");
            nextOffset = AddOffset(Patterns["XPDR"], "xpdrInput", -0x10C, 2, "int", 5, nextOffset);

            //BAT1
            nextOffset = AddOffset(Patterns["BAT1"], "bat1Display", -0x2C, 8, "double", 5, nextOffset);

            //BAT2
            //AddOffset(Patterns["BAT2"], "bat2DisplayA", -0x284, 8, "double");
            //nextOffset = AddOffset(Patterns["BAT2"], "bat2DisplayB", -0x544, 8, "double", 5, nextOffset);
            nextOffset = AddOffset(Patterns["BAT2"], "bat2Display", +0x444, 8, "double", 5, nextOffset);

            //RUDDER
            nextOffset = AddOffset(Patterns["RUDDER1"], "rudderDisplay1", -0xEC, 8, "double", 6, nextOffset);
            AddOffset(Patterns["RUDDER2"], "rudderDisplay2", -0xEC, 8, "double");
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
                MSFSVariableServices.Reload();
                firstUpdate = false;
                Thread.Sleep(100);
            }
            bool isLightTest = FSUIPCConnection.ReadLVar("S_OH_IN_LT_ANN_LT") == 2;

            UpdateFCU(Patterns["FCU"], isLightTest);
            UpdateISIS(Patterns["ISIS"]);
            UpdateCom(Patterns["COM"]);
            UpdateXpdr();
            UpdateBatteries();
            UpdateRudder();

            FSUIPCConnection.Process(Program.groupName);
        }

        private void UpdateFCU(MemoryPattern fcu, bool isLightTest)
        {
            bool isModeTrkFpa = FSUIPCConnection.ReadLVar("I_FCU_TRACK_FPA_MODE") == 1;
            bool isModeHdgVs = FSUIPCConnection.ReadLVar("I_FCU_HEADING_VS_MODE") == 1;
            bool isFcuPowered = isModeHdgVs || isModeTrkFpa;
            bool isModeSpd = FSUIPCConnection.ReadLVar("I_FCU_SPEED_MODE") == 1;
            bool isSpdManaged = FSUIPCConnection.ReadLVar("I_FCU_SPEED_MANAGED") == 1;
            bool isHdgManaged = FSUIPCConnection.ReadLVar("I_FCU_HEADING_MANAGED") == 1;
            bool isAltManaged = FSUIPCConnection.ReadLVar("I_FCU_ALTITUDE_MANAGED") == 1;
            bool isAltHundred = FSUIPCConnection.ReadLVar("S_FCU_ALTITUDE_SCALE") == 0;

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
                            result += ((int)fcu.MemoryOffsets["fcuSpd"].GetValue()).ToString();
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

                    if (isHdgManaged)
                        result += "---*";
                    else
                        result += fcu.MemoryOffsets["fcuHdg"].GetValue()?.ToString("D3") ?? "000";
                }
            }
            IPCOffsets["fcuHdg"].Value = result;

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

                    int vs = fcu.MemoryOffsets["fcuVS"].GetValue() ?? 0;
                    if (isAltManaged && vs == 0)
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
                }
            }
            IPCOffsets["fcuVS"].Value = result;
        }

        private void UpdateISIS(MemoryPattern isis)
        {
            string result = "";
            if (isis.MemoryOffsets["isisStd"].GetValue() == true)
                result = "STD";
            else
                result = isis.MemoryOffsets["isisBaro"].GetValue()?.ToString() ?? "";

            IPCOffsets["isisBaro"].Value = result;
        }

        private void UpdateCom(MemoryPattern com)
        {
            double value = com.MemoryOffsets["comStandby"].GetValue() ?? 0.0;
            if (value > 0.0)
                IPCOffsets["comStandby"].Value = value.ToString();
            else
                IPCOffsets["comStandby"].Value = "";
        }

        private void UpdateXpdr()
        {
            int value = Patterns["XPDR"].MemoryOffsets["xpdrInput"].GetValue() ?? 0;
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

        private void UpdateBatteries()
        {
            double value = Patterns["BAT1"].MemoryOffsets["bat1Display"].GetValue() ?? 0.0;
            IPCOffsets["bat1Display"].Value = string.Format(formatInfo, "{0:F1}", value);

            //value = Patterns["BAT2"].MemoryOffsets["bat2DisplayA"].GetValue() ?? 0.0;
            //if (value == 0.0)
            //    value = Patterns["BAT2"].MemoryOffsets["bat2DisplayB"].GetValue() ?? 0.0;
            //IPCOffsets["bat2DisplayB"].Value = string.Format(formatInfo, "{0:F1}", value);
            value = Patterns["BAT2"].MemoryOffsets["bat2Display"].GetValue() ?? 0.0;
            IPCOffsets["bat2Display"].Value = string.Format(formatInfo, "{0:F1}", value);
        }

        private void UpdateRudder()
        {
            double value = Patterns["RUDDER1"].MemoryOffsets["rudderDisplay1"].GetValue() ?? 0.0;
            if (value == 0.0)
                value = Patterns["RUDDER2"].MemoryOffsets["rudderDisplay2"].GetValue() ?? 0.0;
            
            string result;
            if (value < 0.0)
                result = "L ";
            else if (value > 0.0)
                result = "R ";
            else
                result = "  ";
            result += string.Format(formatInfo, "{0:F1}", Math.Abs(value));

            IPCOffsets["rudderDisplay1"].Value = result;
        }
    }
}
