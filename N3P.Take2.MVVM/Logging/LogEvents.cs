using System;

namespace N3P.MVVM.Logging
{
    [Flags]
    public enum LogEvents
    {
        BeforeGet = 0x01,
        AfterGet = 0x02,
        BeforeSet = 0x04,
        AfterSet = 0x08
    }
}