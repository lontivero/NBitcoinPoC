using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using WalletWasabi.KeyManagement;

namespace NBitcoinPoC
{
    class Program
    {
        static void Main(string[] args)
        {
            // parameters
            var walletPath = "/home/lontivero/.walletwasabi/client/Wallets/REAL.json";
            var txHex = File.ReadAllText("tx.hex");
            var index = 56;
            var tx = Transaction.Parse(txHex);


            tx.Inputs[index].WitScript = null;

            IndexedTxIn currentIndexedInput = tx.Inputs.AsIndexedInputs().Skip(index).First();
            // 4. Find the corresponding registered input.
            Coin registeredCoin;
            TxOut output = null;
            OutPoint prevOut = tx.Inputs[index].PrevOut;

            using (var client = new HttpClient{BaseAddress = new Uri("https://api.smartbit.com.au/v1/")})
            {
                using (HttpResponseMessage response = client.GetAsync($"blockchain/tx/{prevOut.Hash}/hex").GetAwaiter().GetResult())
                {
                    string cont = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var json = JObject.Parse(cont);
                    string hex = json["hex"][0]["hex"].ToString();
                    Transaction ttx = Transaction.Parse(hex, Network.Main);
                    output = ttx.Outputs[prevOut.N];
                    registeredCoin = new Coin(prevOut, output);
                }
            }

            var km = KeyManager.FromFile(walletPath);
            var keys = km.GetSecrets("", registeredCoin.ScriptPubKey);

            var pass = false;
            while(!pass)
            {
                var builder = Network.Main.CreateTransactionBuilder();
                var signedCoinJoin = builder
                    .ContinueToBuild(tx)
                    .AddKeys( keys.ToArray())
                    .AddCoins( registeredCoin )
                    .BuildTransaction(true);

                var w = signedCoinJoin.Inputs.Where(x=> x.WitScript != WitScript.Empty).FirstOrDefault();
                tx.Inputs[index].WitScript = signedCoinJoin.Inputs[index].WitScript;


                // 5. Verify if currentIndexedInput is correctly signed, if not, return the specific error.
#if CONSENSUS
                if (!Script.VerifyScriptConsensus(output.ScriptPubKey, tx, (uint)index, output.Value, ScriptVerify.Standard))
                {
                    Console.WriteLine($"Error");
                }
#else
                if (!currentIndexedInput.VerifyScript(registeredCoin, out ScriptError error))
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
            pass = true;
#endif
        }
    }
}
