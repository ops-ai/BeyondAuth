using Cryptography;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Blockchain
{
    public class KeyStore : IKeyStore
    {
        public KeyStore(byte[] authenticatedHashKey)
        {
            AuthenticatedHashKey = authenticatedHashKey;

            var ecKeyPairGenerator = new ECKeyPairGenerator();
            var ecKeyGenParams = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256k1, new SecureRandom());
            ecKeyPairGenerator.Init(ecKeyGenParams);
            var keyPair = ecKeyPairGenerator.GenerateKeyPair();

            _privateKey = keyPair.Private as ECPrivateKeyParameters;
            PublicKey = keyPair.Public as ECPublicKeyParameters;
        }

        public byte[] AuthenticatedHashKey { get; private set; }

        public ECPublicKeyParameters PublicKey { get; private set; }

        private ECPrivateKeyParameters _privateKey;

        public string SignBlock(string blockHash) => DigitalSignature.SignData(_privateKey, blockHash);

        public bool VerifyBlock(string blockHash, string signature) => DigitalSignature.VerifySignature(blockHash, PublicKey, signature);
    }
}
