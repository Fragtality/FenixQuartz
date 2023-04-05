using System;

namespace FenixQuartz
{
    public class IPCValueLvar : IPCValue
    {
        float lastValue = float.NaN;

        public IPCValueLvar(string id, bool noSubscription = true) : base(App.lvarPrefix + id, "float", 4)
        {
            if (!noSubscription)
                IPCManager.SimConnect.SubscribeLvar(ID);
        }

        public override void SetValue(object value)
        {
            float fValue = float.NaN;
            if (value is byte @byte)
                fValue = Convert.ToSingle(@byte);
            if (value is short @short)
                fValue = Convert.ToSingle(@short);
            if (value is int @int)
                fValue = Convert.ToSingle(@int);
            if (value is float @single)
                fValue = @single;
            if (value is double @double)
                fValue = Convert.ToSingle(@double);
            if (value is string)
                float.TryParse(value as string, ElementManager.formatInfo, out fValue);

            if (!float.IsNaN(fValue) && fValue != lastValue)
            {
                IPCManager.SimConnect.WriteLvar(ID, fValue);
                lastValue = fValue;
            }
        }

        public override dynamic GetValue()
        {
            return IPCManager.SimConnect.ReadLvar(ID);
        }
    }
}
