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
    public static class TAN
    {
        /// <summary>
        /// TAN
        /// </summary>
        public static async Task<string> Send_TAN(ConnectionContext context, string TAN)
        {
            Log.Write("Starting TAN process");

            string segments = string.Empty;

            var HITANS = !String.IsNullOrEmpty(context.Segment.HITANS.Substring(0, 1)) ? Int32.Parse(context.Segment.HITANS.Substring(0, 1)) : 0;

            if (String.IsNullOrEmpty(context.Segment.HITAB)) // TAN Medium Name not set
            {
                // Version 2
                if (HITANS == 2)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N'";
                // Version 3
                else if (HITANS == 3)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N'";
                // Version 4
                else if (HITANS == 4)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N'";
                // Version 5
                else if (HITANS == 5)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++++" + context.Segment.HITAN + "++N'";
                // Version 6
                else if (HITANS == 6)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++++" + context.Segment.HITAN + "+N'";
                else // default
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++++" + context.Segment.HITAN + "++N'";
            }
            else
            {
                // Version 2
                if (HITANS == 2)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N++++" + context.Segment.HITAB + "'";
                // Version 3
                else if (HITANS == 3)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N++++" + context.Segment.HITAB + "'";
                // Version 4
                else if (HITANS == 4)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N++++" + context.Segment.HITAB + "'";
                // Version 5
                else if (HITANS == 5)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++++" + context.Segment.HITAN + "++N++++" + context.Segment.HITAB + "'";
                // Version 6
                else if (HITANS == 6)
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++++" + context.Segment.HITAN + "+N++++" + context.Segment.HITAB + "'";
                else // default
                    segments = "HKTAN:" + 3 + ":" + context.Segment.HITANS.Substring(0, 1) + "+2++" + context.Segment.HITAN + "++N++++" + context.Segment.HITAB + "'";
            }

            context.SegmentNumber = 3;

            string message = FinTSMessage.Create(context.HBCIVersion, context.Segment.HNHBS, context.Segment.HNHBK, context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS + ":" + TAN, context.SegmentNumber);
            return await FinTSMessage.SendAsync(context.Client, context.Url, message);
        }
    }
}
