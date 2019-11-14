using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using libfintx;
using libfintx.Data;

namespace libfintx.Sample
{
    class Program
    {
        static async Task MainAsync(string[] args)
        {
            var context = new ConnectionContext
            {
                Client = new HttpClient(),
                Blz = 76550000,
                Account = "760794644",
                IBAN = "DE07765500000760794644",
                Url = "https://banking-by1.s-fints-pt-by.de/fints30",
                UserId = "760794644",
                Pin = "xxxx",
                HBCIVersion = 300,
                BIC = "BYLADEM1ANS"
            };

            //Console.WriteLine("Please enter your banking credentials!");
            //ConnectionDetails details = new ConnectionDetails();
            //Console.WriteLine("BLZ:");
            //details.Blz = Convert.ToInt32(Console.ReadLine());
            //Console.WriteLine("KontoID:");
            //details.Account = Console.ReadLine();
            //Console.WriteLine("IBAN:");
            //details.IBAN = Console.ReadLine();
            //Console.WriteLine("Institute FinTS Url:");
            //details.Url = Console.ReadLine();
            //Console.WriteLine("Account:");
            //details.UserId = Console.ReadLine();
            //Console.WriteLine("PIN:");
            //details.Pin = Console.ReadLine();
            //details.HBCIVersion = 300;

            var data = await libfintx.Main.Synchronization(context);
            HBCIOutput(data.Messages);

            var balance = await libfintx.Main.Balance(context, new TANDialog(WaitForTAN), false);
            HBCIOutput(balance.Messages);
            Console.WriteLine(balance.Data.Balance);

            var results = await new TransactionsCamt(context, false, camtVersion.camt052, new DateTime(2019, 1, 1), DateTime.Now)
                .ExecuteAsync(new TANDialog(WaitForTAN));

            var transactions = results.Data?.SelectMany(x => x.transactions);
            foreach (var trans in transactions)
            {
                Console.WriteLine($"{trans.inputDate}\t{trans.partnerName}\t{trans.text}\t{trans.amount}");
            }
            Console.ReadLine();
            //HBCIOutput(.Messages);
        }

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static string WaitForTAN(TANDialog tanDialog)
        {
            HBCIOutput(tanDialog.DialogResult.Messages);

            return Console.ReadLine();
        }

        /// <summary>
        /// HBCI-Nachricht ausgeben
        /// </summary>
        /// <param name="hbcimsg"></param>
        private static void HBCIOutput(IEnumerable<HBCIBankMessage> hbcimsg)
        {
            foreach (var msg in hbcimsg)
            {
                Console.WriteLine("Code: " + msg.Code + " | " + "Typ: " + msg.Type + " | " + "Nachricht: " + msg.Message);
            }
        }
    }
}
