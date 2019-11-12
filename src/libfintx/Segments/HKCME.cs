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
    public static class HKCME
    {
        /// <summary>
        /// Collective transfer terminated
        /// </summary>
        public static async Task<string> Init_HKCME(ConnectionContext context, List<pain00100203_ct_data> PainData, string NumberofTransactions, decimal TotalAmount, DateTime ExecutionDay)
        {
            Log.Write("Starting job HKCME: Collective transfer money terminated");

            var TotalAmount_ = TotalAmount.ToString().Replace(",", ".");

            context.SegmentNumber = 3;

            string segments = "HKCME:" + context.SegmentNumber + ":1+" + context.IBAN + ":" + context.BIC + TotalAmount_ + ":EUR++" + " + urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.002.03+@@";

            var painMessage = pain00100203.Create(context.AccountHolder, context.IBAN, context.BIC, PainData, NumberofTransactions, TotalAmount, ExecutionDay);

            segments = segments.Replace("@@", "@" + (painMessage.Length - 1) + "@") + painMessage;

            if (Helper.IsTANRequired(context, "HKCME"))
            {
                context.SegmentNumber = 4;
                segments = HKTAN.Init_HKTAN(context, segments);
            }

            string message = FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
            var response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

            context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(response, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(context, response);

            return response;
        }
    }
}
