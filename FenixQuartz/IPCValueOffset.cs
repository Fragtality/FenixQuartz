using FSUIPC;
using System;

namespace FenixQuartz
{
    public class IPCValueOffset : IPCValue
    {
        public Offset Offset { get; set; }

        public IPCValueOffset(string id, int address, string type = "int", int size = 4) : base(id, type, size)
        {
            ID = id;
            Type = type;
            Size = size;
            if (type != "string" && type != "byte")
                Offset = new Offset<byte[]>(App.groupName, address, size, true);
            else if (type == "byte")
                Offset = new Offset<byte>(App.groupName, address, true);
            else
                Offset = new Offset<string>(App.groupName, address, size, true);
        }

        public override void SetValue(object value)
        {
            if (Type == "byte")
                Offset.SetValue((byte)value);
            if (Type == "short")
                Offset.SetValue(BitConverter.GetBytes((short)value));
            if (Type == "int")
                Offset.SetValue(BitConverter.GetBytes((int)value));
            if (Type == "float")
                Offset.SetValue(BitConverter.GetBytes((float)value));
            if (Type == "double")
                Offset.SetValue(BitConverter.GetBytes((double)value));
            if (Type == "string")
                Offset.SetValue((string)value);
        }

        public override dynamic GetValue()
        {
            if (Type != "string")
            {
                if (Type == "float" || Type == "double")
                {
                    switch (Size)
                    {
                        case 4:
                            return Offset.GetValue<float>();
                        case 8:
                            return Offset.GetValue<double>();
                        default:
                            return 0.0f;
                    }
                }
                else
                {
                    switch (Size)
                    {
                        case 1:
                            return Offset.GetValue<byte>();
                        case 2:
                            return Offset.GetValue<short>();
                        case 4:
                            return Offset.GetValue<int>();
                        case 8:
                            return Offset.GetValue<long>();
                        default:
                            return 0;
                    }
                }
            }
            else
            {
                return Offset.GetValue<string>(); ;
            }
        }
    }
}
