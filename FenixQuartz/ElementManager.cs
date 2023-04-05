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

        private float lastSwitchVS;
        private float lastSwitchAlt;
        private bool lastVSdashed = false;
        private bool lastALTmanaged = false;
        private bool lastHDGmanaged = false;
        private bool isAltVsMode = false;
        private bool isLightTest = false;

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
                { "COM1-1", new MemoryPattern("00 00 00 00 D3 01 00 00 FF FF FF FF 00 00 00 00 00 00 00 00") },
                { "XPDR-1", new MemoryPattern("58 00 50 00 44 00 52 00 20 00 63 00 68 00 61 00 72 00 61 00 63 00 74 00 65 00 72 00 73 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 65 00 64") },
                { "BAT1-1", new MemoryPattern("42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00") },
                { "BAT2-1", new MemoryPattern("61 00 69 00 72 00 63 00 72 00 61 00 66 00 74 00 2E 00 65 00 6C 00 65 00 63 00 74 00 72 00 69 00 63 00 61 00 6C 00 2E 00 62 00 61 00 74 00 74 00 65 00 72 00 79 00 31 00 2E") },
                { "BAT2-2", new MemoryPattern("00 00 42 00 61 00 74 00 74 00 65 00 72 00 79 00 20 00 32 00") },
                { "RUDDER-1", new MemoryPattern("00 00 52 00 75 00 64 00 64 00 65 00 72 00 20 00 74 00 72 00 69 00 6D 00 20 00 64 00 69 00 73 00 70 00 6C 00 61 00 79 00 20 00 64 00 61 00 73 00 68 00 65 00 64 00") },
                //{ "MCDU-1", new MemoryPattern("20 29 6B CE F8 7F 00 00 80 67 FD 09 00 00 00 00") },
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

            //COM1
            AddMemoryValue("com1Standby", MemoryPatterns["FCU-2"], +0x45C, 4, "int");
            AddMemoryValue("com1Active", MemoryPatterns["FCU-2"], +0x444, 4, "int");

            //COM2
            AddMemoryValue("com2Standby", MemoryPatterns["FCU-2"], +0x3FC, 4, "int");
            AddMemoryValue("com2Active", MemoryPatterns["FCU-2"], +0x4D4, 4, "int");

            //XPDR
            AddMemoryValue("xpdrDisplay", MemoryPatterns["XPDR-1"], -0x110, 2, "int");
            AddMemoryValue("xpdrInput", MemoryPatterns["FCU-2"], +0x714, 2, "int");

            //BAT1
            AddMemoryValue("bat1Display", MemoryPatterns["BAT1-1"], -0x2C, 8, "double");

            //BAT2
            if (!App.ignoreBatteries)
            {
                AddMemoryValue("bat2Display1", MemoryPatterns["BAT2-1"], +0x51C, 8, "double");
                AddMemoryValue("bat2Display2", MemoryPatterns["BAT2-2"], -0x282, 8, "double");
            }

            //RUDDER
            AddMemoryValue("rudderDisplay1", MemoryPatterns["RUDDER-1"], 0xB9E, 8, "double");
            AddMemoryValue("rudderDisplay2", MemoryPatterns["RUDDER-1"], 0xBCE, 8, "double");
            AddMemoryValue("rudderDisplay3", MemoryPatterns["RUDDER-1"], 0xB1E, 8, "double");

            //CHR / ET
            AddMemoryValue("clockCHR", MemoryPatterns["FCU-2"], -0x54, 4, "int");
            AddMemoryValue("clockET", MemoryPatterns["FCU-2"], -0x3C, 4, "int");

            //TO Speeds
            //AddMemoryValue("speedV1", MemoryPatterns["MCDU-1"], +0x2EC, 4, "int");
            //AddMemoryValue("speedVR", MemoryPatterns["MCDU-1"], +0x2FC, 4, "int");
            //AddMemoryValue("speedV2", MemoryPatterns["MCDU-1"], +0x2F4, 4, "int");


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

        private void CheckAndRescan()
        {
            int value = MemoryValues["fcuAlt"].GetValue();
            if (value < 100 || value > 43000)
            {
                Logger.Log(LogLevel.Information, "ElementManager:CheckAndRescan", $"Memory Locations changed! Rescanning ...");
                foreach (var pattern in MemoryPatterns.Values)
                    pattern.Location = 0;
                InitializeScanner();
                Scanner.UpdateBuffers(MemoryValues);
            }
        }

        //private static int modcounter = 0;
        public bool GenerateValues()
        {
            try
            {
                //if (modcounter % 50 == 0 && modcounter > 0)
                //{
                //    Logger.Log(LogLevel.Debug, "ElementManager:GenerateValues", $"V1: {MemoryValues["speedV1"].GetValue()}");
                //    Logger.Log(LogLevel.Debug, "ElementManager:GenerateValues", $"VR: {MemoryValues["speedVR"].GetValue()}");
                //    Logger.Log(LogLevel.Debug, "ElementManager:GenerateValues", $"V2: {MemoryValues["speedV2"].GetValue()}");
                    
                //}
                //modcounter++;

                if (!Scanner.UpdateBuffers(MemoryValues))
                {
                    Logger.Log(LogLevel.Error, "ElementManager:GenerateValues", $"UpdateBuffers() failed");
                    return false;
                }
                CheckAndRescan();

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
            float switchAlt = IPCManager.SimConnect.ReadLvar("S_FCU_ALTITUDE");
            bool isDashed = MemoryValues["fcuVsDashed"].GetValue();
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

            bool altManaged = IPCManager.SimConnect.ReadLvar("I_FCU_ALTITUDE_MANAGED") == 1;
            if (!altManaged && altManaged != lastALTmanaged && lastVSdashed != isDashed && !isDashed && !firstUpdate)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (Alt-Dot changed and Dashed changed)");
            }

            bool hdgManaged = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_MANAGED") == 1;
            if (hdgManaged != lastHDGmanaged && lastVSdashed != isDashed && !isDashed)
            {
                isAltVsMode = true;
                Logger.Log(LogLevel.Debug, "ElementManager:UpdateFMA", $"Setting isAltVsMode to TRUE (Hdg-Dot changed and Dashed changed)");
            }

            lastHDGmanaged = hdgManaged;
            lastALTmanaged = altManaged;
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

            bool isModeTrkFpa = IPCManager.SimConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 1;
            bool isModeHdgVs = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1;
            bool isFcuPowered = isModeHdgVs || isModeTrkFpa;
            bool isModeSpd = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MODE") == 1;
            bool isSpdManaged = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MANAGED") == 1;
            bool isHdgManaged = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_MANAGED") == 1;
            bool isAltManaged = IPCManager.SimConnect.ReadLvar("I_FCU_ALTITUDE_MANAGED") == 1;
            bool isAltHundred = IPCManager.SimConnect.ReadLvar("S_FCU_ALTITUDE_SCALE") == 0;
            
            //SPEED
            string result = "";
            if (isFcuPowered)
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
            if (isFcuPowered)
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
            if (isFcuPowered)
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
            if (isFcuPowered)
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
                    if (isAltVsMode)
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
                if (isAltVsMode)
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
                IPCValues["isisStd"].SetValue(std);
                IPCValues["isisBaro"].SetValue((float)baro);
            }
        }

        private void UpdateCom(string com)
        {
            //int valueStandby = MemoryValues[$"com{com}Standby"].GetValue() ?? 0;
            //if (valueStandby == 0 && com == "1")
            //    valueStandby = MemoryValues[$"com{com}Standby2"].GetValue() ?? 0;
            //int valueActive = MemoryValues[$"com{com}Active"].GetValue() ?? 0;
            //if (valueActive == 0 && com == "1")
            //    valueActive = MemoryValues[$"com{com}Active2"].GetValue() ?? 0;

            int valueActive = MemoryValues[$"com{com}Active"].GetValue() ?? 0;
            int valueStandby = MemoryValues[$"com{com}Standby"].GetValue() ?? 0;

            //if (com == "1" && valueActive != (ofActiveFreq.Value / 1000))
            //{
            //    valueActive = MemoryValues[$"com{com}Active2"].GetValue() ?? 0;
            //    valueStandby = MemoryValues[$"com{com}Standby2"].GetValue() ?? 0;
            //}

            bool courseMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_VOR") == 1 || IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_ILS") == 1;
            bool adfMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_ADF") == 1;
            bool hfMode = IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_HF1") == 1 || IPCManager.SimConnect.ReadLvar($"I_PED_RMP{com}_HF2") == 1;

            if (!App.rawValues)
            {
                if (courseMode)
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
                IPCValues[$"com{com}Active"].SetValue(valueActive);
                IPCValues[$"com{com}Standby"].SetValue(valueStandby);
            }
        }

        private void UpdateXpdr()
        {
            string result;
            int value = MemoryValues["xpdrInput"].GetValue() ?? 0;

            if (value <= 0)
                value = MemoryValues["xpdrDisplay"].GetValue() ?? 0;

            result = value.ToString();
            //if (value != 0)
            //{
            //    value = MemoryValues["xpdrInput"].GetValue() ?? 0;
            //    if (value > 0)
            //        result = value.ToString();
            //    else
            //    {
            //        value = MemoryValues["xpdrDisplay"].GetValue() ?? 0;
            //        if (value > 0)
            //            result = value.ToString();
            //    }
            //}
            //else
            //{
            //    value = MemoryValues["xpdrInput2"].GetValue() ?? 0;
            //    if (value > 0)
            //        result = value.ToString();
            //}

            if (!App.rawValues)
                IPCValues["xpdrStr"].SetValue(result);
            else
            {
                if (short.TryParse(result, out short shortValue))
                    IPCValues["xpdr"].SetValue(shortValue);
                else
                {
                    IPCValues["xpdr"].SetValue((short)0);
                }
            }
        }

        private void UpdateBatteries()
        {
            double value = MemoryValues["bat1Display"].GetValue() ?? 0.0;
            if (!App.rawValues)
                IPCValues["bat1Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
            else
                IPCValues["bat1"].SetValue((float)Math.Round(value,1));

            value = MemoryValues["bat2Display1"].GetValue() ?? 0.0;
            if (value == 0.0 || MemoryValues["bat2Display1"].IsTinyValue())
                value = MemoryValues["bat2Display2"].GetValue() ?? 0.0;

            if (value != 0)
            {
                if (!App.rawValues)
                    IPCValues["bat2Str"].SetValue(string.Format(formatInfo, "{0:F1}", value));
                else
                    IPCValues["bat2"].SetValue((float)Math.Round(value,1));
            }
        }

        private void UpdateRudder()
        {
            double value = MemoryValues["rudderDisplay1"].GetValue();
            if (MemoryValues["rudderDisplay1"].IsTinyValue())
            {
                value = MemoryValues["rudderDisplay2"].GetValue();
                if (MemoryValues["rudderDisplay2"].IsTinyValue())
                    value = MemoryValues["rudderDisplay3"].GetValue();
            }

            value = Math.Round(value, 2);
            if (!App.rawValues)
            {
                string space = (Math.Abs(value) >= 10 ? "" : " ");
                string result;
                if (value <= -0.1)
                    result = "L" + space;
                else if (value >= 0.1)
                    result = "R" + space;
                else
                    result = " " + space;
                result += string.Format(formatInfo, "{0:F1}", Math.Abs(value));

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
                if (num != -1)
                    IPCValues[strName].SetValue(string.Format("{0:D2}:{1:D2}", upper, lower));
                else
                    IPCValues[strName].SetValue("");
            }
            else
            {
                IPCValues[numName].SetValue(num);
            }
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
