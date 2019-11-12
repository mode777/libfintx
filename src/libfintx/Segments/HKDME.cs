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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libfintx
{
    public static class HKDME
    {
        /// <summary>
        /// Collective collect
        /// </summary>
        public static async Task<string> Init_HKDME(ConnectionContext context, DateTime SettlementDate, List<pain00800202_cc_data> PainData, string NumberofTransactions, decimal TotalAmount)
        {
            Log.Write("Starting job HKDME: Collective collect money");

            context.SegmentNumber = 3;

            var TotalAmount_ = TotalAmount.ToString().Replace(",", ".");

            string segments = "HKDME:" + context.SegmentNumber + ":2+" + context.IBAN + ":" + context.BIC + "+" + TotalAmount_ + ":EUR++" + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.008.002.02+@@";

            var message = pain00800202.Create(context.AccountHolder, context.IBAN, context.BIC, SettlementDate, PainData, NumberofTransactions, TotalAmount);

            segments = segments.Replace("@@", "@" + (message.Length - 1) + "@") + message;

            if (Helper.IsTANRequired(context, "HKDME"))
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
