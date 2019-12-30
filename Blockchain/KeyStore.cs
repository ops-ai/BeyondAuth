using Cryptography;
using System;

namespace Blockchain
{
    public class KeyStore : IKeyStore
    {
        private DigitalSignature DigitalSignature { get; set; } = new DigitalSignature();

        public byte[] AuthenticatedHashKey { get; private set; }

        public KeyStore(byte[] authenticatedHashKey)
        {
            AuthenticatedHashKey = authenticatedHashKey;
        }

        public string SignBlock(string blockHash)
        {
            return Convert.ToBase64String(DigitalSignature.SignData(Convert.FromBase64String(blockHash)));
        }

        public bool VerifyBlock(string blockHash, string signature)
        {
            return DigitalSignature.VerifySignature(Convert.FromBase64String(blockHash), Convert.FromBase64String(signature));
        }
    }
}
