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
    public static class HKSYN
    {
        public static async Task<string> Init_HKSYN(ConnectionContext context)
        {
            Log.Write("Starting Synchronisation");

            string segments;

            if (context.HBCIVersion == 220)
            {
                string segments_ = 
                    "HKIDN:" + 3 + ":2+280:" + context.BlzPrimary + "+" + context.UserId + "+0+1'" +
                    "HKVVB:" + 4 + ":2+0+0+0+" + Program.ProductId + "+" + Program.Version + "'" +
                    "HKSYN:" + 5 + ":2+0'";

                segments = segments_;
            }
            else if (context.HBCIVersion == 300)
            {
                string segments_ = 
                    "HKIDN:" + 3 + ":2+280:" + context.BlzPrimary + "+" + context.UserId + "+0+1'" +
                    "HKVVB:" + 4 + ":3+0+0+0+" + Program.ProductId + "+" + Program.Version + "'" +
                    "HKSYN:" + 5 + ":3+0'";

                segments = segments_;
            }
            else
            {
                //Since connectionDetails is a re-usable object, this shouldn't be cleared.
                //connectionDetails.UserId = string.Empty;
                //connectionDetails.Pin = null;

                Log.Write("HBCI version not supported");

                throw new Exception("HBCI version not supported");
            }

            context.SegmentNumber = 5;

            string message = FinTSMessage.Create(context.HBCIVersion, "1", "0", context.BlzPrimary, context.UserId, context.Pin, SYS.SETVal(0), segments, null, context.SegmentNumber);
            string response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

            Helper.Parse_Segment(context, response);

            return response;
        }
    }
}
