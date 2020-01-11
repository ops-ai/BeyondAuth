/* 
* Copyright (c) Marc Clifton
* The Code Project Open License (CPOL) 1.02
* http://www.codeproject.com/info/cpol10.aspx
*/

using Clifton.Blockchain;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Blockchain.Tests
{
    public class MerkleTreeTests
    {
        [Fact]
        public void HashesAreSameTest()
        {
            var h1 = MerkleHash.Create("abc");
            var h2 = MerkleHash.Create("abc");
            Assert.True(h1 == h2);
        }

        [Fact]
        public void CreateNodeTest()
        {
            var node = new MerkleNode();
            Assert.Null(node.Parent);
            Assert.Null(node.LeftNode);
            Assert.Null(node.RightNode);
        }

        /// <summary>
        /// Tests that after setting the left node, the parent hash verifies.
        /// </summary>
        [Fact]
        public void LeftHashVerificationTest()
        {
            var parentNode = new MerkleNode();
            var leftNode = new MerkleNode();
            leftNode.ComputeHash(Encoding.UTF8.GetBytes("abc"));
            parentNode.SetLeftNode(leftNode);
            Assert.True(parentNode.VerifyHash());
        }

        /// <summary>
        /// Tests that after setting both child nodes (left and right), the parent hash verifies.
        /// </summary>
        [Fact]
        public void LeftRightHashVerificationTest()
        {
            var parentNode = CreateParentNode("abc", "def");
            Assert.True(parentNode.VerifyHash());
        }

        [Fact]
        public void NodesEqualTest()
        {
            var parentNode1 = CreateParentNode("abc", "def");
            var parentNode2 = CreateParentNode("abc", "def");
            Assert.True(parentNode1.Equals(parentNode2));
        }

        [Fact]
        public void NodesNotEqualTest()
        {
            var parentNode1 = CreateParentNode("abc", "def");
            var parentNode2 = CreateParentNode("def", "abc");
            Assert.False(parentNode1.Equals(parentNode2));
        }

        [Fact]
        public void VerifyTwoLevelTree()
        {
            var parentNode1 = CreateParentNode("abc", "def");
            var parentNode2 = CreateParentNode("123", "456");
            var rootNode = new MerkleNode();
            rootNode.SetLeftNode(parentNode1);
            rootNode.SetRightNode(parentNode2);
            Assert.True(rootNode.VerifyHash());
        }

        [Fact]
        public void CreateBalancedTreeTest()
        {
            var tree = new MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("abc"));
            tree.AppendLeaf(MerkleHash.Create("def"));
            tree.AppendLeaf(MerkleHash.Create("123"));
            tree.AppendLeaf(MerkleHash.Create("456"));
            tree.BuildTree();
            Assert.NotNull(tree.RootNode);
        }

        [Fact]
        public void CreateUnbalancedTreeTest()
        {
            var tree = new MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("abc"));
            tree.AppendLeaf(MerkleHash.Create("def"));
            tree.AppendLeaf(MerkleHash.Create("123"));
            tree.BuildTree();
            Assert.NotNull(tree.RootNode);
        }

        // A Merkle audit path for a leaf in a Merkle Hash Tree is the shortest
        // list of additional nodes in the Merkle Tree required to compute the
        // Merkle Tree Hash for that tree.
        [Fact]
        public void AuditTest()
        {
            // Build a tree, and given the root node and a leaf hash, verify that the we can reconstruct the root hash.
            var tree = new MerkleTree();
            var l1 = MerkleHash.Create("abc");
            var l2 = MerkleHash.Create("def");
            var l3 = MerkleHash.Create("123");
            var l4 = MerkleHash.Create("456");
            tree.AppendLeaves(new MerkleHash[] { l1, l2, l3, l4 });
            var rootHash = tree.BuildTree();

            var auditTrail = tree.AuditProof(l1);
            Assert.True(MerkleTree.VerifyAudit(rootHash, l1, auditTrail));

            auditTrail = tree.AuditProof(l2);
            Assert.True(MerkleTree.VerifyAudit(rootHash, l2, auditTrail));

            auditTrail = tree.AuditProof(l3);
            Assert.True(MerkleTree.VerifyAudit(rootHash, l3, auditTrail));

            auditTrail = tree.AuditProof(l4);
            Assert.True(MerkleTree.VerifyAudit(rootHash, l4, auditTrail));
        }

        [Fact]
        public void FixingOddNumberOfLeavesByAddingTreeTest()
        {
            var tree = new MerkleTree();
            var l1 = MerkleHash.Create("abc");
            var l2 = MerkleHash.Create("def");
            var l3 = MerkleHash.Create("123");
            tree.AppendLeaves(new MerkleHash[] { l1, l2, l3 });
            var rootHash = tree.BuildTree();
            tree.AddTree(new MerkleTree());
            var rootHashAfterFix = tree.BuildTree();
            Assert.True(rootHash == rootHashAfterFix);
        }

        [Fact]
        public void FixingOddNumberOfLeavesManuallyTest()
        {
            var tree = new MerkleTree();
            var l1 = MerkleHash.Create("abc");
            var l2 = MerkleHash.Create("def");
            var l3 = MerkleHash.Create("123");
            tree.AppendLeaves(new MerkleHash[] { l1, l2, l3 });
            var rootHash = tree.BuildTree();
            tree.FixOddNumberLeaves();
            var rootHashAfterFix = tree.BuildTree();
            Assert.True(rootHash != rootHashAfterFix);
        }

        [Fact]
        public void AddTreeTest()
        {
            var tree = new MerkleTree();
            var l1 = MerkleHash.Create("abc");
            var l2 = MerkleHash.Create("def");
            var l3 = MerkleHash.Create("123");
            tree.AppendLeaves(new MerkleHash[] { l1, l2, l3 });
            var rootHash = tree.BuildTree();

            var tree2 = new MerkleTree();
            var l5 = MerkleHash.Create("456");
            var l6 = MerkleHash.Create("xyzzy");
            var l7 = MerkleHash.Create("fizbin");
            var l8 = MerkleHash.Create("foobar");
            tree2.AppendLeaves(new MerkleHash[] { l5, l6, l7, l8 });
            tree2.BuildTree();
            var rootHashAfterAddTree = tree.AddTree(tree2);

            Assert.True(rootHash != rootHashAfterAddTree);
        }

        private void ForEachWithIndex<T>(List<T> collection, Action<int, T> action)
        {
            var n = 0;
            collection.ForEach(t =>
            {
                action(n++, t);
            });
        }

        // Merkle consistency proofs prove the append-only property of the tree.
        [Fact]
        public void ConsistencyTest()
        {
            // Start with a tree with 2 leaves:
            var tree = new MerkleTree();
            var startingNodes = tree.AppendLeaves(new MerkleHash[]
                {
                    MerkleHash.Create("1"),
                    MerkleHash.Create("2"),
                });

            // startingNodes.ForEachWithIndex((n, i) => n.Text = i.ToString());

            var firstRoot = tree.BuildTree();

            var oldRoots = new List<MerkleHash>() { firstRoot };

            // Add a new leaf and verify that each time we add a leaf, we can get a consistency check
            // for all the previous leaves.
            for (var i = 2; i < 100; i++)
            {
                tree.AppendLeaf(MerkleHash.Create(i.ToString())); //.Text=i.ToString();
                tree.BuildTree();

                // After adding a leaf, verify that all the old root hashes exist.
                ForEachWithIndex(oldRoots, (n, oldRootHash) =>
                {
                    var proof = tree.ConsistencyProof(n + 2);
                    MerkleHash hash, lhash, rhash;

                    if (proof.Count > 1)
                    {
                        lhash = proof[proof.Count - 2].Hash;
                        var hidx = proof.Count - 1;
                        hash = rhash = MerkleTree.ComputeHash(lhash, proof[hidx].Hash);
                        hidx -= 2;

                        while (hidx >= 0)
                        {
                            lhash = proof[hidx].Hash;
                            hash = rhash = MerkleTree.ComputeHash(lhash, rhash);

                            --hidx;
                        }
                    }
                    else
                    {
                        hash = proof[0].Hash;
                    }

                    Assert.True(hash == oldRootHash, "Old root hash not found for index " + i + " m = " + (n + 2).ToString());

                });

                // Then we add this root hash as the next old root hash to check.
                oldRoots.Add(tree.RootNode.Hash);
            }
        }

        private MerkleNode CreateParentNode(string leftData, string rightData)
        {
            var parentNode = new MerkleNode();
            var leftNode = new MerkleNode();
            var rightNode = new MerkleNode();
            leftNode.ComputeHash(Encoding.UTF8.GetBytes(leftData));
            rightNode.ComputeHash(Encoding.UTF8.GetBytes(rightData));
            parentNode.SetLeftNode(leftNode);
            parentNode.SetRightNode(rightNode);

            return parentNode;
        }
    }
}
