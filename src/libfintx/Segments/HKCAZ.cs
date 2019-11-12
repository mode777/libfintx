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
using System.Net.Http;
using System.Threading.Tasks;

namespace libfintx
{
    public static class HKCAZ
    {
        /// <summary>
        /// Transactions in camt053 format
        /// </summary>
        public static async Task<string> Init_HKCAZ(ConnectionContext context, string FromDate, string ToDate, string Startpoint, camtVersion camtVers)
        {
            string segments = string.Empty;

            context.SegmentNumber = 3;

            switch (camtVers)
            {
                case camtVersion.camt052:
                    Log.Write("Starting job HKCAZ: Request transactions in camt052 format");

                    if (String.IsNullOrEmpty(FromDate))
                    {
                        if (String.IsNullOrEmpty(Startpoint))
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt052 + "+N'";
                        }
                        else
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt052 + "+N++++" + Startpoint + "'";
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(Startpoint))
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt052 + "+N+" + FromDate + "+" + ToDate + "'";
                        }
                        else
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt052 + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                        }
                    }

                    context.SegmentNumber = 3;
                    break;
                    
                case camtVersion.camt053:
                    Log.Write("Starting job HKCAZ: Request transactions in camt053 format");

                    if (String.IsNullOrEmpty(FromDate))
                    {
                        if (String.IsNullOrEmpty(Startpoint))
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt053 + "+N'";
                        }
                        else
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt053 + "+N++++" + Startpoint + "'";
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(Startpoint))
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt053 + "+N+" + FromDate + "+" + ToDate + "'";
                        }
                        else
                        {
                            segments = "HKCAZ:" + context.SegmentNumber + ":" + context.Segment.HKCAZ + "+" + context.IBAN + ":" + context.BIC + ":" + context.Account + "::280:" + context.Blz + "+" + Scheme.camt053 + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                        }
                    }

                    break;

                default: // -> Will never happen, only for compiler
                    return string.Empty;
            }

            if (Helper.IsTANRequired(context, "HKCAZ"))
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
