namespace FenixQuartz
{
    public class IPCValue
    {
        public virtual string ID { get; set; } = "";
        public virtual string Type { get; set; } = "int";
        public virtual int Size { get; set; } = 4;

        public IPCValue(string id, string type = "int", int size = 4)
        {
            ID = id;
            Type = type;
            Size = size;
        }

        public virtual void SetValue(object value)
        {

        }

        public virtual dynamic GetValue()
        {
            return 0;
        }
    }
}
