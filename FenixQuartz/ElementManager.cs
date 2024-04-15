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
        //protected FenixInterface.FenixInterface FenixGateway;
        public static readonly NumberFormatInfo formatInfo = new CultureInfo("en-US").NumberFormat;
        
        //private bool firstUpdate = true;
        //public float lastSwitchBaroStd;
        public bool isLightTest = false;
        public bool isAltManaged = false;
        public bool isHdgManaged = false;
        public bool isHdgDashed = false;
        public bool isSpdManaged = false;
        public bool isSpdDashed = false;
        public bool isModeSpd = false;
        public bool isModeHdgVs = false;
        public bool isVsDashed = false;
        public bool fcuIsPowered = false;
        //public int speedV1 = 0;
        //public int speedVR = 0;
        //public int speedV2 = 0;
        //public int toFlex = 0;
        public bool isBaroStd = false;

        //private int appTick = 0;

        public ElementManager(List<OutputDefinition> definitions)
        {
            IPCValues = new();
            MemoryValues = new();
            //FenixGateway = new();
            Definitions = definitions;

            //// MEMORY PATTERNS
            MemoryPatterns = new()
            {
                { "FCU-2", new MemoryPattern("00 00 00 00 CE 05 00 00 FF FF FF FF 00 00 00 80") },
                //{ "ISIS-1", new MemoryPattern("49 00 53 00 49 00 53 00 20 00 70 00 6F 00 77 00 65 00 72 00 65 00 64 00") },
                //{ "ISIS-2", new MemoryPattern("46 00 65 00 6E 00 69 00 78 00 42 00 72 00 61 00 6B 00 65 00 46 00 61 00 6E 00 73 00") },
            };

            InitializeScanner();


            //// MEMORY VALUES
            //ISIS
            //AddMemoryValue("isisStd1", MemoryPatterns["ISIS-1"], -0xC7, 1, "bool");
            //AddMemoryValue("isisBaro1", MemoryPatterns["ISIS-1"], -0xEC, 8, "double");
            //AddMemoryValue("isisStd2", MemoryPatterns["ISIS-1"], -0xDF, 1, "bool");
            //AddMemoryValue("isisBaro2", MemoryPatterns["ISIS-1"], -0x104, 8, "double");
            //AddMemoryValue("isisStd3", MemoryPatterns["ISIS-2"], -0x3F, 1, "bool");
            //AddMemoryValue("isisBaro3", MemoryPatterns["ISIS-2"], -0x64, 8, "double");

            //XPDR
            //AddMemoryValue("xpdrDisplay", MemoryPatterns["XPDR-1"], -0x110, 2, "int");
            //AddMemoryValue("xpdrInput", MemoryPatterns["FCU-2"], +0x714, 2, "int");
            //AddMemoryValue("xpdrDigits", MemoryPatterns["FCU-2"], +0x90C, 2, "int");

            //RUDDER
            AddMemoryValue("rudderDashed1", MemoryPatterns["FCU-2"], -0x4D4C, 1, "bool"); //B_FC_RUDDER_TRIM_DASHED
            AddMemoryValue("rudderDashed2", MemoryPatterns["FCU-2"], -0x4D64, 1, "bool");

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

            //////TO L-Vars
            //AddIpcLvar("speedV1");
            //AddIpcLvar("speedVR");
            //AddIpcLvar("speedV2");
            //AddIpcLvar("toFlex");

            IPCManager.SimConnect.SubscribeLvar("S_OH_IN_LT_ANN_LT");
            IPCManager.SimConnect.SubscribeLvar("B_FCU_POWER"); 
            IPCManager.SimConnect.SubscribeLvar("I_FCU_TRACK_FPA_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_HEADING_VS_MODE");
            IPCManager.SimConnect.SubscribeLvar("N_FCU_SPEED");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_SPEED_MODE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_SPEED_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("B_FCU_SPEED_DASHED");
            IPCManager.SimConnect.SubscribeLvar("N_FCU_HEADING");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_HEADING_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("B_FCU_HEADING_DASHED");
            IPCManager.SimConnect.SubscribeLvar("N_FCU_ALTITUDE");
            IPCManager.SimConnect.SubscribeLvar("I_FCU_ALTITUDE_MANAGED");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_ALTITUDE_SCALE");
            IPCManager.SimConnect.SubscribeLvar("N_FCU_VS");
            IPCManager.SimConnect.SubscribeLvar("B_FCU_VERTICALSPEED_DASHED");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_EFIS1_BARO_MODE");
            IPCManager.SimConnect.SubscribeLvar("S_FCU_EFIS1_BARO_STD");
            IPCManager.SimConnect.SubscribeLvar("B_FCU_EFIS1_BARO_STD");
            IPCManager.SimConnect.SubscribeLvar("B_MIP_ISFD_BARO_STD");
            IPCManager.SimConnect.SubscribeLvar("B_MIP_ISFD_BARO_INCH");
            IPCManager.SimConnect.SubscribeLvar("N_MIP_ISFD_BARO_HPA");
            IPCManager.SimConnect.SubscribeLvar("N_MIP_ISFD_BARO_INCH");
            IPCManager.SimConnect.SubscribeLvar("N_MIP_CLOCK_UTC");
            IPCManager.SimConnect.SubscribeLvar("N_MIP_CLOCK_ELAPSED");
            IPCManager.SimConnect.SubscribeLvar("N_MIP_CLOCK_CHRONO");
            IPCManager.SimConnect.SubscribeLvar("N_FREQ_XPDR_SELECTED");
            IPCManager.SimConnect.SubscribeLvar("N_FREQ_STANDBY_XPDR_SELECTED");
            IPCManager.SimConnect.SubscribeLvar("N_PED_XPDR_CHAR_DISPLAYED");
            IPCManager.SimConnect.SubscribeLvar("N_ELEC_VOLT_BAT_1");
            IPCManager.SimConnect.SubscribeLvar("N_ELEC_VOLT_BAT_2");
            IPCManager.SimConnect.SubscribeLvar("N_FC_RUDDER_TRIM_DECIMAL");
            IPCManager.SimConnect.SubscribeSimVar("KOHLSMAN SETTING MB:1", "Millibars");
            SubscribeRmpVars("1");
            SubscribeRmpVars("2");
        }

        private static void SubscribeRmpVars(string com)
        {
            IPCManager.SimConnect.SubscribeLvar($"N_PED_RMP{com}_ACTIVE");
            IPCManager.SimConnect.SubscribeLvar($"N_PED_RMP{com}_STDBY");
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

        //private bool CheckIsisLocation(string num)
        //{
        //    double baro = MemoryValues[$"isisBaro{num}"].GetValue();
        //    return baro > 250 && baro < 4000;
        //}

        //private void CheckMemoryValues()
        //{
        //    if (!(CheckIsisLocation("1") || CheckIsisLocation("2") || CheckIsisLocation("3")))
        //    {
        //        Logger.Log(LogLevel.Information, "ElementManager:CheckMemoryValues", $"Memory Locations changed! Rescanning ...");
        //        System.Threading.Thread.Sleep(500);
        //        Rescan();
        //    }
        //}

        //private void Rescan()
        //{
        //    foreach (var pattern in MemoryPatterns.Values)
        //        pattern.Location = 0;
        //    InitializeScanner();
        //    PrintReport();
        //    Scanner.UpdateBuffers(MemoryValues);
        //}

        private void UpdateSimVars()
        {
            isLightTest = IPCManager.SimConnect.ReadLvar("S_OH_IN_LT_ANN_LT") == 2;
            fcuIsPowered = IPCManager.SimConnect.ReadLvar("B_FCU_POWER") == 1;
            isSpdManaged = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MANAGED") == 1;
            isSpdDashed = IPCManager.SimConnect.ReadLvar("B_FCU_SPEED_DASHED") == 1;
            isAltManaged = IPCManager.SimConnect.ReadLvar("I_FCU_ALTITUDE_MANAGED") == 1;
            isHdgManaged = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_MANAGED") == 1;
            isHdgDashed = IPCManager.SimConnect.ReadLvar("B_FCU_HEADING_DASHED") == 1;
            isModeSpd = IPCManager.SimConnect.ReadLvar("I_FCU_SPEED_MODE") == 1;
            isModeHdgVs = IPCManager.SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1;
            isVsDashed = IPCManager.SimConnect.ReadLvar("B_FCU_VERTICALSPEED_DASHED") == 1;
        }

        //private void UpdateFenixVars()
        //{
        //    if (appTick % 3 == 0)
        //    {
        //        _ = int.TryParse(FenixGateway.FenixGetVariable("aircraft.fms.perf.takeOff.v1"), out speedV1);
        //        _ = int.TryParse(FenixGateway.FenixGetVariable("aircraft.fms.perf.takeOff.v2"), out speedV2);
        //        _ = int.TryParse(FenixGateway.FenixGetVariable("aircraft.fms.perf.takeOff.vr"), out speedVR);
        //        _ = int.TryParse(FenixGateway.FenixGetVariable("aircraft.fms.perf.takeOff.flexTemp"), out toFlex);
        //    }
        //    appTick++;
        //}

        public bool GenerateValues()
        {
            try
            {
                if (!Scanner.UpdateBuffers(MemoryValues))
                {
                    Logger.Log(LogLevel.Error, "ElementManager:GenerateValues", $"UpdateBuffers() failed");
                    return false;
                }
                UpdateSimVars();
                //UpdateFenixVars();
                //CheckMemoryValues();

                UpdateFCU();
                UpdateISIS();
                UpdateCom("1");
                UpdateCom("2");
                UpdateXpdr();
                UpdateBatteries();
                UpdateRudder();
                UpdateClock();
                //UpdateSpeeds();
                UpdateBaro();

                if (!App.useLvars)
                    FSUIPCConnection.Process(App.groupName);

                //if (firstUpdate)
                //    firstUpdate = false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ElementManager:GenerateValues", $"Exception '{ex.GetType()}' - '{ex.Message}' ({ex.StackTrace})");
                return false;
            }
        }

        private void UpdateFCU()
        {
            if (App.rawValues)
            {
                UpdateRawFCU();
                return;
            }
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

                    int value = (int)IPCManager.SimConnect.ReadLvar("N_FCU_SPEED");

                    if (isSpdDashed)
                    {
                        if (isModeSpd)
                            result += "---";
                        else
                            result += "-.--";
                    }
                    else if (isModeSpd)
                        result += value.ToString("D3");
                    else
                        result += "0." + value.ToString();

                    if (isSpdManaged)
                        result += "*";
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

                    if (isHdgDashed)
                        result += "---";
                    else
                        result += ((int)IPCManager.SimConnect.ReadLvar("N_FCU_HEADING")).ToString("D3");

                    if (isHdgManaged)
                        result += "*";
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
                    result = ((int)IPCManager.SimConnect.ReadLvar("N_FCU_ALTITUDE")).ToString("D5");
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

                    int vs = (int)IPCManager.SimConnect.ReadLvar("N_FCU_VS");
                    if (isVsDashed)
                    {
                        if (isModeHdgVs)
                            result += "-----";
                        else
                            result += "--.-";
                    }
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
            float fvalue = IPCManager.SimConnect.ReadLvar("N_FCU_SPEED");

            if (App.scaleMachValue && !isModeSpd)
                fvalue /= 100.0f;

            IPCValues["fcuSpd"].SetValue(fvalue);
            if (isSpdDashed)
                IPCValues["fcuSpdDashed"].SetValue((byte)1);
            else
                IPCValues["fcuSpdDashed"].SetValue((byte)0);

            //HDG
            IPCValues["fcuHdg"].SetValue((int)IPCManager.SimConnect.ReadLvar("N_FCU_HEADING"));
            if (isHdgDashed)
                IPCValues["fcuHdgDashed"].SetValue((byte)1);
            else
                IPCValues["fcuHdgDashed"].SetValue((byte)0);


            //ALT
            IPCValues["fcuAlt"].SetValue((int)IPCManager.SimConnect.ReadLvar("N_FCU_ALTITUDE"));

            //VS
            float vs = IPCManager.SimConnect.ReadLvar("N_FCU_VS");
            if (!isVsDashed)
            {
                if (!isModeHdgVs)
                    vs = (float)Math.Round(vs / 1000.0f, 1);
            }
            else
                vs = 0.0f;

            IPCValues["fcuVs"].SetValue(vs);
            if (isVsDashed)
                IPCValues["fcuVsDashed"].SetValue((byte)1);
            else
                IPCValues["fcuVsDashed"].SetValue((byte)0);

        }

        private void UpdateISIS()
        {
            //double baro = MemoryValues["isisBaro1"].GetValue();
            //bool std = MemoryValues["isisStd1"].GetValue();
            //if (baro < 800 || baro > 1200)
            //{
            //    baro = MemoryValues["isisBaro2"].GetValue();
            //    std = MemoryValues["isisStd2"].GetValue();
            //    if (baro < 800 || baro > 1200)
            //    {
            //        baro = MemoryValues["isisBaro3"].GetValue();
            //        std = MemoryValues["isisStd3"].GetValue();
            //    }
            //}

            double hpa = IPCManager.SimConnect.ReadLvar("N_MIP_ISFD_BARO_HPA");
            double inhg = IPCManager.SimConnect.ReadLvar("N_MIP_ISFD_BARO_INCH");
            bool isInch = IPCManager.SimConnect.ReadLvar("B_MIP_ISFD_BARO_INCH") == 1;
            bool std = IPCManager.SimConnect.ReadLvar("B_MIP_ISFD_BARO_STD") == 1;

            double baro = 0;
            if (!App.rawValues)
            {
                string result;
                if (std)
                    result = "STD";
                else
                {
                    //bool isHpa = IPCManager.SimConnect.ReadLvar("S_FCU_EFIS1_BARO_MODE") == 1;
                    if (!isInch)
                    {
                        baro = Math.Round(hpa, 0);
                        result = string.Format("{0,4:0000}", baro);
                    }
                    else
                    {
                        baro = Math.Round(inhg, 0) / 100.0f;
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
            int valueActive = (int)IPCManager.SimConnect.ReadLvar($"N_PED_RMP{com}_ACTIVE");
            int valueStandby = (int)IPCManager.SimConnect.ReadLvar($"N_PED_RMP{com}_STDBY");

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
            //int input = MemoryValues["xpdrInput"].GetValue() ?? 0;
            int input = (int)IPCManager.SimConnect.ReadLvar("N_FREQ_STANDBY_XPDR_SELECTED");
            int disp = (int)IPCManager.SimConnect.ReadLvar("N_FREQ_XPDR_SELECTED");
            int digits = (int)IPCManager.SimConnect.ReadLvar("N_PED_XPDR_CHAR_DISPLAYED");

            if (isLightTest && disp >= 0)
                result = "8888";
            else if (digits == 0 || disp == -1)
                result = "";
            else if (digits < 4 && input >= 0)
                result = input.ToString($"D{digits}");
            else if (digits == 4)
                result = disp.ToString($"D{digits}");
            else
                result = "";

            if (!App.rawValues)
                IPCValues["xpdrStr"].SetValue(result);
            else
            {
                IPCValues["xpdrDigits"].SetValue((short)digits);
                if (disp == -1)
                    IPCValues["xpdr"].SetValue((short)-1);
                else if (short.TryParse(result, out short shortValue))
                    IPCValues["xpdr"].SetValue(shortValue);
                else
                {
                    IPCValues["xpdr"].SetValue((short)-1);
                }
            }
        }

        private void UpdateBatteries()
        {
            if (!App.rawValues)
                IPCValues["bat1Str"].SetValue(string.Format(formatInfo, "{0:F1}", IPCManager.SimConnect.ReadLvar("N_ELEC_VOLT_BAT_1")));
            else
                IPCValues["bat1"].SetValue(IPCManager.SimConnect.ReadLvar("N_ELEC_VOLT_BAT_1"));

            if (!App.rawValues)
                IPCValues["bat2Str"].SetValue(string.Format(formatInfo, "{0:F1}", IPCManager.SimConnect.ReadLvar("N_ELEC_VOLT_BAT_2")));
            else
                IPCValues["bat2"].SetValue(IPCManager.SimConnect.ReadLvar("N_ELEC_VOLT_BAT_2"));
        }

        private void UpdateRudder()
        { 
            bool isDashed = !(MemoryValues["rudderDashed1"].GetValue() ?? false) && !(MemoryValues["rudderDashed2"].GetValue() ?? false);
            float value = IPCManager.SimConnect.ReadLvar("N_FC_RUDDER_TRIM_DECIMAL") / 10;        

            bool power = IPCManager.SimConnect.ReadLvar("N_PED_RMP2_ACTIVE") != -1;
            if (!App.rawValues)
            {
                string result;
                if (power)
                {
                    if (!isDashed)
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
                        result = "---";
                }
                else
                    result = "";

                IPCValues["rudderStr"].SetValue(result);
            }
            else
            {
                if (power && !isDashed)
                    IPCValues["rudder"].SetValue(value);
                else if (!power)
                    IPCValues["rudder"].SetValue((float)Math.Round(value, -1));
                else if (isDashed)
                    IPCValues["rudder"].SetValue((float)Math.Round(value, -2));
            }
        }

        private void UpdateClock()
        {
            UpdateClock("N_MIP_CLOCK_CHRONO", "clockChrStr", "clockChr");
            UpdateClock("N_MIP_CLOCK_ELAPSED", "clockEtStr", "clockEt");

            float clock = IPCManager.SimConnect.ReadLvar("N_MIP_CLOCK_UTC");
            if (!App.rawValues)
            {
                string result = "";

                if (isLightTest && clock >= 0)
                    IPCValues["clockTime"].SetValue("88:88:88");
                else if (clock >= 0)
                {
                    int hours = (int)(clock / 10000.0f);
                    int minutes = (int)((clock - hours * 10000) / 100);
                    int seconds = (int)(clock - hours * 10000 - minutes * 100);

                    result = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
                }

                IPCValues["clockTimeStr"].SetValue(result);
            }
            else
            {
                if (isLightTest && clock >= 0)
                    IPCValues["clockTime"].SetValue(888888);
                else
                    IPCValues["clockTime"].SetValue((int)clock);
            }
        }

        private void UpdateClock(string lvar, string strName, string numName)
        {
            int num = (int)IPCManager.SimConnect.ReadLvar(lvar);
            int upper = num / 100;
            int lower = num % 100;

            if (!App.rawValues)
            {
                if (isLightTest && num >= 0)
                    IPCValues[strName].SetValue("88:88");
                else if (num >= 0)
                    IPCValues[strName].SetValue(string.Format("{0:D2}:{1:D2}", upper, lower));
                else
                    IPCValues[strName].SetValue("");
            }
            else
            {
                if (isLightTest && num >= 0)
                    IPCValues[numName].SetValue(8888);
                else
                    IPCValues[numName].SetValue(num);
            }
        }

        //private void UpdateSpeeds()
        //{
        //    IPCValues["speedV1"].SetValue(speedV1);
        //    IPCValues["speedVR"].SetValue(speedVR);
        //    IPCValues["speedV2"].SetValue(speedV2);
        //    IPCValues["toFlex"].SetValue(toFlex);
        //}

        public void UpdateBaro()
        {
            //if (firstUpdate)
            //{
            //    lastSwitchBaroStd = IPCManager.SimConnect.ReadLvar("S_FCU_EFIS1_BARO_STD");
            //    return;
            //}

            //float switchBaroStd = IPCManager.SimConnect.ReadLvar("S_FCU_EFIS1_BARO_STD");
            //if (switchBaroStd != lastSwitchBaroStd)
            //{
            //    if (switchBaroStd > lastSwitchBaroStd)
            //        isBaroStd = true;
            //    else
            //        isBaroStd = false;
            //    Logger.Log(LogLevel.Information, "ElementManager:UpdateBaro", $"Baro-Std changed: isBaroStd {isBaroStd}");
            //    lastSwitchBaroStd = switchBaroStd;
            //}
            isBaroStd = IPCManager.SimConnect.ReadLvar("B_FCU_EFIS1_BARO_STD") == 1;

            float pressure = IPCManager.SimConnect.ReadSimVar("KOHLSMAN SETTING MB:1", "Millibars");
            bool isHpa = IPCManager.SimConnect.ReadLvar("S_FCU_EFIS1_BARO_MODE") == 1;

            if (!isHpa)
                pressure = (float)Math.Round(pressure * 0.029529983071445f, 2);
            else
                pressure = (float)Math.Round(pressure, 0);
            
            if (isLightTest)
                pressure = 88.88f;

            if (!App.rawValues)
            {
                string result;
                if (!fcuIsPowered)
                    result = "";
                else if (isBaroStd)
                    result = "Std";
                else if (isLightTest)
                    result = "88.88";
                else
                {
                    if (isHpa)
                        result = string.Format("{0,4:0000}", pressure);
                    else
                        result = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", pressure);
                }

                IPCValues["baroCptStr"].SetValue(result);
            }
            else
            {
                if (!fcuIsPowered)
                    IPCValues["baroCpt"].SetValue((float)-1);
                else
                    IPCValues["baroCpt"].SetValue((float)pressure);

                if (isBaroStd)
                    IPCValues["baroCptStd"].SetValue((byte)1);
                else
                    IPCValues["baroCptStd"].SetValue((byte)0);

                if (isHpa)
                    IPCValues["baroCptMb"].SetValue((byte)1);
                else
                    IPCValues["baroCptMb"].SetValue((byte)0);
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
