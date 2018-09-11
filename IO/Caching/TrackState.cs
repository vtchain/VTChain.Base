using System;
using System.Collections.Generic;
using System.Text;

namespace VTChain.Base.IO.Caching
{
    public enum TrackState : byte
    {
        None,
        Added,
        Changed,
        Deleted
    }
}
