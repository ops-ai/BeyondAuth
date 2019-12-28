using System;
using System.Collections.Generic;

namespace Blockchain
{
    public interface IBlockChain
    {
        /// <summary>
        /// Accept a block
        /// </summary>
        /// <param name="block"></param>
        void AcceptBlock(IBlock block);

        /// <summary>
        /// Next block sequence number
        /// </summary>
        long NextBlockNumber { get; }

        /// <summary>
        /// Verify the full chain
        /// </summary>
        void VerifyChain();
    }
}
