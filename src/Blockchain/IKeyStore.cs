using Org.BouncyCastle.Crypto.Parameters;

namespace Blockchain
{
    public interface IKeyStore
    {
        /// <summary>
        /// HMAC
        /// </summary>
        byte[] AuthenticatedHashKey { get; }

        /// <summary>
        /// Public Key
        /// </summary>
        ECPublicKeyParameters PublicKey { get; }

        /// <summary>
        /// Sign a block
        /// </summary>
        /// <param name="blockHash">The block's hash</param>
        /// <returns></returns>
        string SignBlock(string blockHash);

        /// <summary>
        /// Verify a block
        /// </summary>
        /// <param name="blockHash">The block's hash</param>
        /// <param name="signature">Signature</param>
        /// <returns></returns>
        bool VerifyBlock(string blockHash, string signature);
    }
}
