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
    public static class INI
    {
        /// <summary>
        /// INI
        /// </summary>
        public static async Task<string> Init_INI(ConnectionContext context, bool anonymous)
        {
            if (!anonymous)
            {
                /// <summary>
                /// Sync
                /// </summary>
                try
                {
                    string segments;

                    context.SegmentNumber = 5;

                    /// <summary>
                    /// INI
                    /// </summary>
                    if (context.HBCIVersion == 220)
                    {
                        string segments_ =
                            "HKIDN:" + 3 + ":2+280:" + context.BlzPrimary + "+" + context.UserId + "+" + context.Segment.HISYN + "+1'" +
                            "HKVVB:" + 4 + ":2+0+0+0+" + Program.ProductId + "+" + Program.Version + "'";

                        segments = segments_;
                    }
                    else if (context.HBCIVersion == 300)
                    {
                        string segments_ =
                            "HKIDN:" + 3 + ":2+280:" + context.BlzPrimary + "+" + context.UserId + "+" + context.Segment.HISYN + "+1'" +
                            "HKVVB:" + 4 + ":3+0+0+0+" + Program.ProductId + "+" + Program.Version + "'";

                        if (context.Segment.HITANS != null && context.Segment.HITANS.Substring(0, 3).Equals("6+4"))
                            segments_ = HKTAN.Init_HKTAN(context, segments_);

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

                    var message = FinTSMessage.Create(context.HBCIVersion, "1", "0", context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
                    var response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

                    Helper.Parse_Segment(context, response);

                    context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(response, "HITAN:", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

                    return response;
                }
                catch (Exception ex)
                {
                    //Since connectionDetails is a re-usable object, this shouldn't be cleared.
                    //connectionDetails.UserId = string.Empty;
                    //connectionDetails.Pin = null;

                    Log.Write(ex.ToString());

                    throw new Exception("Software error", ex);
                }
            }
            else
            {
                /// <summary>
                /// Sync
                /// </summary>
                try
                {
                    Log.Write("Starting Synchronisation anonymous");

                    string segments;

                    if (context.HBCIVersion == 300)
                    {
                        string segments_ = 
                            "HKIDN:" + 2 + ":2+280:" + context.BlzPrimary + "+" + "9999999999" + "+0+0'" +
                            "HKVVB:" + 3 + ":3+0+0+1+" + Program.ProductId + "+" + Program.Version + "'";

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

                    context.SegmentNumber = 4;

                    string message = FinTSMessageAnonymous.Create(context.HBCIVersion, "1", "0", context.Blz, context.UserId, context.Pin, SYS.SETVal(0), segments, null, context.SegmentNumber);
                    string response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

                    var messages = Helper.Parse_Segment(context, response);
                    var result = new HBCIDialogResult(messages, response);
                    if (!result.IsSuccess)
                    {
                        Log.Write("Synchronisation anonymous failed. " + result);
                        return response;
                    }

                    // Sync OK
                    Log.Write("Synchronisation anonymous ok");

                    /// <summary>
                    /// INI
                    /// </summary>
                    if (context.HBCIVersion == 300)
                    {
                        string segments__ = 
                            "HKIDN:" + 3 + ":2+280:" + context.BlzPrimary + "+" + context.UserId + "+" + context.Segment.HISYN + "+1'" +
                            "HKVVB:" + 4 + ":3+0+0+0+" + Program.ProductId + "+" + Program.Version + "'" +
                            "HKSYN:" + 5 + ":3+0'";

                        segments = segments__;
                    }
                    else
                    {
                        Log.Write("HBCI version not supported");

                        throw new Exception("HBCI version not supported");
                    }

                    context.SegmentNumber = 5;

                    message = FinTSMessage.Create(context.HBCIVersion, "1", "0", context.BlzPrimary, context.UserId, context.Pin, context.Segment.HISYN, segments, context.Segment.HIRMS, context.SegmentNumber);
                    response = await FinTSMessage.SendAsync(context.Client, context.Url, message);

                    Helper.Parse_Segment(context, response);

                    context.Segment.HITAN = Helper.Parse_String(Helper.Parse_String(response, "HITAN:", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

                    return response;
                }
                catch (Exception ex)
                {
                    //Since connectionDetails is a re-usable object, this shouldn't be cleared.
                    //connectionDetails.UserId = string.Empty;
                    //connectionDetails.Pin = null;

                    Log.Write(ex.ToString());

                    DEBUG.Write("Software error: " + ex.ToString());

                    throw new Exception("Software error: " + ex.ToString());
                }
            }
        }
    }
}
