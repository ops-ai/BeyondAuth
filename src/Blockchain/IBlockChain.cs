using System.Threading.Tasks;

namespace Blockchain
{
    public interface IBlockChain
    {
        /// <summary>
        /// Accept a block
        /// </summary>
        /// <param name="block"></param>
        Task AcceptBlock(IBlock block);

        /// <summary>
        /// Next block sequence number
        /// </summary>
        long NextBlockNumber { get; }

        /// <summary>
        /// Verify the full chain
        /// </summary>
        Task<bool> VerifyChain();
    }
}
