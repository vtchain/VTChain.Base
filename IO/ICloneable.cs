using System;
using System.Collections.Generic;
using System.Text;

namespace VTChain.Base.IO
{
    public interface ICloneable<T>
    {
        T Clone();
        void FromReplica(T replica);
    }
}
