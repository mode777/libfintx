using libfintx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx
{
    public static class HKCDB
    {
        /// <summary>
        /// Get bankers orders
        /// </summary>
        public static async Task<string> Init_HKCDB(ConnectionContext context)
        {
            Log.Write("Starting job HKCDB: Get bankers order");

            context.SegmentNumber = 3;

            string segments = "HKCDB:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03'";

            if (Helper.IsTANRequired(context, "HKCDB"))
            {
                context.SegmentNumber = 4;
                segments = HKTAN.Init_HKTAN(context, segments);
            }

            string message = FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
            string response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

            context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(response, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(context, response);

            return response;
        }
    }
}
