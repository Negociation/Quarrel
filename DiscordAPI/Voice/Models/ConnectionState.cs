﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_UWP.Voice
{
    public enum ConnectionState : byte
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }
}