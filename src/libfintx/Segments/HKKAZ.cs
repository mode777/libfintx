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
    public static class HKKAZ
    {
        /// <summary>
        /// Transactions
        /// </summary>
        public static async Task<string> Init_HKKAZ(ConnectionContext context, string FromDate, string ToDate, string Startpoint)
        {
            Log.Write("Starting job HKKAZ: Request transactions");

            string segments = string.Empty;

            context.SegmentNumber = 3;

            if (String.IsNullOrEmpty(FromDate))
            {
                if (String.IsNullOrEmpty(Startpoint))
                {
                    if (Convert.ToInt16(context.Segment.HKKAZ) < 7)
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.Account + "::280:" + context.Blz + "+N'";
                    else
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+N'";
                }
                else
                {
                    if (Convert.ToInt16(context.Segment.HKKAZ) < 7)
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.Account + "::280:" + context.Blz + "+N++++" + Startpoint + "'";
                    else
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+N++++" + Startpoint + "'";
                }
            }
            else
            {
                if (String.IsNullOrEmpty(Startpoint))
                {
                    if (Convert.ToInt16(context.Segment.HKKAZ) < 7)
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.Account + "::280:" + context.Blz + "+N+" + FromDate + "+" + ToDate + "'";
                    else
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+N+" + FromDate + "+" + ToDate + "'";
                }
                else
                {
                    if (Convert.ToInt16(context.Segment.HKKAZ) < 7)
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.Account + "::280:" + context.Blz + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                    else
                        segments = "HKKAZ:" + context.SegmentNumber + ":" + context.Segment.HKKAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                }
            }

            if (Helper.IsTANRequired(context, "HKKAZ"))
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
