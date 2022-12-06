using FSUIPC;
using System.Text;

namespace PilotsDeck_FNX2PLD
{
    public class IPCOffset
    {
        public string ID { get; set; } = "";
        public string Type { get; set; } = "int";
        public int Size { get; set; } = 4;
        public Offset Offset { get; set; }

        public IPCOffset(string id, int address, string type = "int", int size = 4)
        {
            ID = id;
            Type = type;
            Size = size;
            if (type != "string" && type != "byte")
                Offset = new Offset<byte[]>(Program.groupName, address, size, true);
            else if (type == "byte")
                Offset = new Offset<byte>(Program.groupName, address, true);
            else
                Offset = new Offset<string>(Program.groupName, address, size, true);
        }

        public void SetValue(object value)
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
    }
}
