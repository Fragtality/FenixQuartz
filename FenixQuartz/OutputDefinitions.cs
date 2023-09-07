using System.Collections.Generic;

namespace FenixQuartz
{
    public class OutputDefinition
    {
        public string ID { get; set; } = "";
        public string Type { get; set; } = "";
        public int Size { get; set; } = 1;
        public int Offset { get; set; } = 0;

        public OutputDefinition(string ID, string Type = "float", int Size = 4, int Offset = 0)
        {
            this.ID = ID;
            this.Type = Type;
            this.Size = Size;
            this.Offset = Offset;
        }

        public override string ToString()
        {
            if (!App.rawValues)
                //return string.Format("Using FSUIPC Offset: {0,-16}     0x{1:X}:{2}:s", $"'{ID}'", Offset, Size);
                return string.Format("Using FSUIPC Offset: {0,-16}     0x{1:X} Type: {2,-8}  Size: {3}", $"'{ID}'", Offset, Type, Size);
            else if (!App.useLvars)
                return string.Format("Using FSUIPC Offset: {0,-16}     0x{1:X} Type: {2,-8}  Size: {3}", $"'{ID}'", Offset, Type, Size);
            else
                return $"Using L-Var  L:{App.lvarPrefix + ID}";
        }

        public static OutputDefinition AddIpcOffset(string ID, string Type, int Size, ref int nextOffset)
        {
            var def = new OutputDefinition(ID, Type, Size, nextOffset);
            nextOffset += Size;
            return def;
        }

        public static List<OutputDefinition> CreateDefinitions()
        {
            List<OutputDefinition> definitions = new();
            int nextOffset = App.offsetBase;

            //// STRING VALUES - StreamDeck
            if (!App.rawValues)
            {
                //FCU
                definitions.Add(AddIpcOffset("fcuSpdStr", "string", 10, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuHdgStr", "string", 9, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuAltStr", "string", 8, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuVsStr", "string", 10, ref nextOffset));

                //ISIS
                definitions.Add(AddIpcOffset("isisStr", "string", 6, ref nextOffset));

                //COM1 standby
                definitions.Add(AddIpcOffset("com1StandbyStr", "string", 8, ref nextOffset));

                //XPDR
                definitions.Add(AddIpcOffset("xpdrStr", "string", 5, ref nextOffset));

                if (!App.ignoreBatteries)
                {
                    //BAT1
                    definitions.Add(AddIpcOffset("bat1Str", "string", 5, ref nextOffset));

                    //BAT2
                    definitions.Add(AddIpcOffset("bat2Str", "string", 5, ref nextOffset));
                }

                //RUDDER
                definitions.Add(AddIpcOffset("rudderStr", "string", 6, ref nextOffset));

                //VS Selected
                definitions.Add(AddIpcOffset("isVsActive", "string", 2, ref nextOffset));

                //COM1 active
                definitions.Add(AddIpcOffset("com1ActiveStr", "string", 8, ref nextOffset));

                //COM2
                definitions.Add(AddIpcOffset("com2StandbyStr", "string", 8, ref nextOffset));
                definitions.Add(AddIpcOffset("com2ActiveStr", "string", 8, ref nextOffset));

                //CHR + ET
                definitions.Add(AddIpcOffset("clockChrStr", "string", 6, ref nextOffset));
                definitions.Add(AddIpcOffset("clockEtStr", "string", 6, ref nextOffset));

                //BARO
                definitions.Add(AddIpcOffset("baroCptStr", "string", 6, ref nextOffset));
            }
            //// RAW VALUES (Offset)
            else if (!App.useLvars)
            {
                //FCU
                definitions.Add(AddIpcOffset("fcuSpd", "float", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuHdg", "int", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuAlt", "int", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuVs", "float", 4, ref nextOffset));

                //ISIS
                definitions.Add(AddIpcOffset("isisStd", "byte", 1, ref nextOffset));
                definitions.Add(AddIpcOffset("isisBaro", "float", 4, ref nextOffset));

                //COM1
                definitions.Add(AddIpcOffset("com1Active", "int", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("com1Standby", "int", 4, ref nextOffset));

                //XPDR
                definitions.Add(AddIpcOffset("xpdr", "short", 2, ref nextOffset));

                if (!App.ignoreBatteries)
                {
                    //BAT1
                    definitions.Add(AddIpcOffset("bat1", "float", 4, ref nextOffset));

                    //BAT2
                    definitions.Add(AddIpcOffset("bat2", "float", 4, ref nextOffset));
                }
                
                //RUDDER
                definitions.Add(AddIpcOffset("rudder", "float", 4, ref nextOffset));

                //FCU Dashes
                definitions.Add(AddIpcOffset("fcuSpdDashed", "byte", 1, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuHdgDashed", "byte", 1, ref nextOffset));
                definitions.Add(AddIpcOffset("fcuVsDashed", "byte", 1, ref nextOffset));

                //COM2
                definitions.Add(AddIpcOffset("com2Active", "int", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("com2Standby", "int", 4, ref nextOffset));

                //VS Selected
                definitions.Add(AddIpcOffset("isVsActive", "byte", 1, ref nextOffset));

                //CHR + ET
                definitions.Add(AddIpcOffset("clockChr", "int", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("clockEt", "int", 4, ref nextOffset));

                //XPDR Digits
                definitions.Add(AddIpcOffset("xpdrDigits", "short", 2, ref nextOffset));

                //BARO
                definitions.Add(AddIpcOffset("baroCpt", "float", 4, ref nextOffset));
                definitions.Add(AddIpcOffset("baroCptStd", "byte", 1, ref nextOffset));
                definitions.Add(AddIpcOffset("baroCptMb", "byte", 1, ref nextOffset));
            }
            //// RAW VALUES (L-Var)
            else
            {
                //FCU
                definitions.Add(new OutputDefinition("fcuSpd"));
                definitions.Add(new OutputDefinition("fcuHdg"));
                definitions.Add(new OutputDefinition("fcuAlt"));
                definitions.Add(new OutputDefinition("fcuVs"));
                definitions.Add(new OutputDefinition("isVsActive"));

                //FCU Dashes
                definitions.Add(new OutputDefinition("fcuSpdDashed"));
                definitions.Add(new OutputDefinition("fcuHdgDashed"));
                definitions.Add(new OutputDefinition("fcuVsDashed"));

                //ISIS
                definitions.Add(new OutputDefinition("isisStd"));
                definitions.Add(new OutputDefinition("isisBaro"));

                //COM1
                definitions.Add(new OutputDefinition("com1Active"));
                definitions.Add(new OutputDefinition("com1Standby"));

                //COM2
                definitions.Add(new OutputDefinition("com2Active"));
                definitions.Add(new OutputDefinition("com2Standby"));

                //XPDR
                definitions.Add(new OutputDefinition("xpdr"));
                definitions.Add(new OutputDefinition("xpdrDigits"));

                if (!App.ignoreBatteries)
                {
                    //BAT1
                    definitions.Add(new OutputDefinition("bat1"));

                    //BAT2
                    definitions.Add(new OutputDefinition("bat2"));
                }

                //RUDDER
                definitions.Add(new OutputDefinition("rudder"));

                //CHR + ET
                definitions.Add(new OutputDefinition("clockChr"));
                definitions.Add(new OutputDefinition("clockEt"));

                //BARO
                definitions.Add(new OutputDefinition("baroCpt"));
                definitions.Add(new OutputDefinition("baroCptStd"));
                definitions.Add(new OutputDefinition("baroCptMb"));
            }


            return definitions;
        }
    }
}
