using System;
using System.Collections.Generic;
using System.Text;

namespace VTChain.Base.IO.Caching
{
    public interface ITrackable<TKey>
    {
        TKey Key { get; }
        TrackState TrackState { get; set; }
    }
}
