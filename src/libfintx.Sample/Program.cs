﻿using System;
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
            var details = new ConnectionContext
            {
                Client = new HttpClient(),
                Blz = 76550000,
                Account = "760794644",
                IBAN = "DE07765500000760794644",
                Url = "https://banking-by1.s-fints-pt-by.de/fints30",
                UserId = "760794644",
                Pin = "xxxxx",
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

            var data = await libfintx.Main.Synchronization(details);
            HBCIOutput(data.Messages);

            var balance = await libfintx.Main.Balance(details, new TANDialog(WaitForTAN), false);
            HBCIOutput(balance.Messages);
            Console.WriteLine(balance.Data.Balance);

            var results = await libfintx.Main.Transactions_camt(details, new TANDialog(WaitForTAN), false, camtVersion.camt052, new DateTime(2019, 1, 1), DateTime.Now);
            var transactions = results.Data?.SelectMany(x => x.transactions);

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
