using System;
using System.Collections.Generic;
using System.Text;
using VTChain.Base.DataType;
using VTChain.Base.IO;

namespace VTChain.Base.NetWork
{
    public interface IInventory : IVerifiable
    {
        UInt256 Hash { get; }

        InventoryType InventoryType { get; }

        bool Verify();
    }
}
