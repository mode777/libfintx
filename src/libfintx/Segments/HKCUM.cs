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
    public static class HKCUM
    {
        /// <summary>
        /// Rebooking
        /// </summary>
        public static async Task<string> Init_HKCUM(ConnectionContext context, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage)
        {
            Log.Write("Starting job HKCUM: Rebooking money");

            context.SegmentNumber = 3;

            string segments = "HKCUM:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.002.03+@@";

            var message = pain00100203.Create(context.AccountHolder, context.IBAN, context.BIC, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, new DateTime(1999,1,1));

            segments = segments.Replace("@@", "@" + (message.Length - 1) + "@") + message;

            if (Helper.IsTANRequired(context, "HKCUM"))
            {
                context.SegmentNumber = 4;
                segments = HKTAN.Init_HKTAN(context, segments);
            }

            var TAN = await FinTSMessage.SendAsync(context.Client, context.Url, FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber));

            context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(TAN, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(context, TAN);

            return TAN;
        }
    }
}
