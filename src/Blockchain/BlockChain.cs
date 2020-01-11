using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blockchain
{
    public class BlockChain : IBlockChain
    {
        /// <summary>
        /// Reference to current block
        /// </summary>
        public IBlock CurrentBlock { get; private set; }

        /// <summary>
        /// Reference to genesis block
        /// </summary>
        public IBlock HeadBlock { get; private set; }

        /// <summary>
        /// List of blocks. Placeholder block storage
        /// </summary>
        public List<IBlock> Blocks { get; }

        public BlockChain() => Blocks = new List<IBlock>();

        public long NextBlockNumber => throw new NotImplementedException();

        public Task AcceptBlock(IBlock block)
        {
            if (HeadBlock == null)
            {
                // This is the first block, so make it the genesis block.
                HeadBlock = block;
                HeadBlock.PreviousBlockHash = null;
            }

            CurrentBlock = block;
            Blocks.Add(block);

            return Task.CompletedTask;
        }

        public Task<bool> VerifyChain()
        {
            if (HeadBlock == null)
                throw new InvalidOperationException("Genesis block not set.");

            return Task.FromResult(HeadBlock.IsValidChain(null));
        }
    }
}
