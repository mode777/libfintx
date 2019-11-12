using libfintx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static libfintx.HKCDE;

namespace libfintx
{
    public class HKCDL
    {
        /// <summary>
        /// Delete banker's order
        /// </summary>
        public static async Task<string> Init_HKCDL(ConnectionContext context, string OrderId, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime FirstTimeExecutionDay, TimeUnit timeUnit, string Rota, int ExecutionDay)
        {
            Log.Write("Starting job HKCDL: Delete bankers order");

            context.SegmentNumber = 3;

            string segments = "HKCDL:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03+@@";

            var sepaMessage = pain00100103.Create(context.AccountHolder, context.IBAN, context.BIC, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, new DateTime(1999, 1, 1)).Replace("'", "");
            segments = segments.Replace("@@", "@" + sepaMessage.Length + "@") + sepaMessage;

            segments += "++" + OrderId + "+" + FirstTimeExecutionDay.ToString("yyyyMMdd") + ":" + (char)timeUnit + ":" + Rota + ":" + ExecutionDay + "'";

            if (Helper.IsTANRequired(context, "HKCDL"))
            {
                context.SegmentNumber = 4;
                segments = HKTAN.Init_HKTAN(context, segments);
            }

            string message = FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
            var TAN = await FinTSMessage.SendAsync(context.Client, context.Url, message);

            context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(TAN, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(context, TAN);

            return TAN;
        }
    }
}
