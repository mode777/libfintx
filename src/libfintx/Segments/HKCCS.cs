/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (c) 2016 - 2018 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
 * 	
 * 	libfintx is free software; you can redistribute it and/or
 *	modify it under the terms of the GNU Lesser General Public
 * 	License as published by the Free Software Foundation; either
 * 	version 2.1 of the License, or (at your option) any later version.
 *	
 * 	libfintx is distributed in the hope that it will be useful,
 * 	but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * 	Lesser General Public License for more details.
 *	
 * 	You should have received a copy of the GNU Lesser General Public
 * 	License along with libfintx; if not, write to the Free Software
 * 	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 * 	
 */

using libfintx.Data;
using System;
using System.Threading.Tasks;

namespace libfintx
{
    public static class HKCCS
    {
        /// <summary>
        /// Transfer
        /// </summary>
        public static async Task<string> Init_HKCCS(ConnectionContext context, string ReceiverName, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage)
        {
            Log.Write("Starting job HKCCS: Transfer money");

            string segments = string.Empty;

            string sepaMessage = string.Empty;

            context.SegmentNumber = 3;

            if (context.Segment.HISPAS == 1)
            {
                segments = "HKCCS:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03+@@";
                sepaMessage = pain00100103.Create(context.AccountHolder, context.IBAN, context.BIC, ReceiverName, ReceiverIBAN, ReceiverBIC, Amount, Usage, new DateTime(1999, 1, 1));
            }
            else if (context.Segment.HISPAS == 2)
            {
                segments = "HKCCS:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.002.03+@@";
                sepaMessage = pain00100203.Create(context.AccountHolder, context.IBAN, context.BIC, ReceiverName, ReceiverIBAN, ReceiverBIC, Amount, Usage, new DateTime(1999, 1, 1));
            }  
            else if (context.Segment.HISPAS == 3)
            {
                segments = "HKCCS:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.003.03+@@";
                sepaMessage = pain00100303.Create(context.AccountHolder, context.IBAN, context.BIC, ReceiverName, ReceiverIBAN, ReceiverBIC, Amount, Usage, new DateTime(1999, 1, 1));
            }

            segments = segments.Replace("@@", "@" + (sepaMessage.Length - 1) + "@") + sepaMessage;

            if (Helper.IsTANRequired(context, "HKCCS"))
            {
                context.SegmentNumber = 4;
                segments = HKTAN.Init_HKTAN(context, segments);
            }

            var message = FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
            var response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

            context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(response, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(context, response);

            return response;
        }
    }
}
