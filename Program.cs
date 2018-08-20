using System;
using NBitcoin;

namespace NBitcoinPoC
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1();
        }

        static void Test1()
        {
            const int P2wpkhInputSizeInBytes = 41;

            var fundingKey = new Key();
            var fundingAddress = fundingKey.PubKey.GetSegwitAddress(Network.Main);
            var destinationAddress = new Key().PubKey.GetSegwitAddress(Network.Main);
            var changeAddress = new Key().PubKey.GetSegwitAddress(Network.Main);

            var coinOrigin = CreateTransactionWithCoin(Money.Coins(2), fundingAddress.ScriptPubKey);
            var tb0 = new TransactionBuilder();
            tb0.AddCoins(coinOrigin);
            tb0.Send(destinationAddress, Money.Coins(1));
            tb0.SetChange(changeAddress);
            tb0.AddKeys(fundingKey);
            var signedTx = tb0.BuildTransaction(true);

            var signedVs = signedTx.GetVirtualSize();
            var signedRs = signedTx.GetSerializedSize();
            var signedTotalSize = signedTx.GetSerializedSize(TransactionOptions.Witness);
            var signedStrippedSize = signedTx.GetSerializedSize(TransactionOptions.None);

            var tb1 = new TransactionBuilder();
            tb1.AddCoins(coinOrigin);
            tb1.Send(destinationAddress, Money.Coins(1));
            tb1.SetChange(changeAddress);
            var unsignedTx = tb1.BuildTransaction(true);

            var unsignedVs = unsignedTx.GetVirtualSize();
            var unsignedRs = unsignedTx.GetSerializedSize();
            var unsignedTotalSize = unsignedTx.GetSerializedSize(TransactionOptions.Witness);
            var unsignedStrippedSize = unsignedTx.GetSerializedSize(TransactionOptions.None);

            var estimatedVirtaulSize = unsignedTx.GetVirtualSize() + (P2wpkhInputSizeInBytes * unsignedTx.Inputs.Count);

            // Less tha 5%?
            var isGoodEstimator = estimatedVirtaulSize > 0.95 * signedVs && estimatedVirtaulSize < 1.05 * signedVs;
            Console.WriteLine(isGoodEstimator); 
        }

        static Transaction CreateTransactionWithCoin(Money amount, Script scriptPubKey)
        {
            var tx = Transaction.Create(Network.Main);
            tx.Version = Transaction.CURRENT_VERSION;
            tx.LockTime = LockTime.Zero;
            tx.AddInput(new TxIn()
            {
                ScriptSig = new Script(OpcodeType.OP_0, OpcodeType.OP_0),
                Sequence = Sequence.Final
            });
            tx.AddOutput(amount, scriptPubKey);
            return tx;

        }
    }
}
