using System;
using System.Collections.Generic;
using System.Text;

namespace VTChain.Base.Cryptography
{
    public interface ICrypto
    {
        byte[] Hash160(byte[] message);

        byte[] Hash256(byte[] message);

        bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey);
    }
}
