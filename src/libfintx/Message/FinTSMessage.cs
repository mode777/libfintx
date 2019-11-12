﻿/*	
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

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace libfintx
{
    public static class FinTSMessage
    {
        /// <summary>
        /// Create FinTS message
        /// </summary>
        /// <param name="Version"></param>
        /// <param name="MsgNum"></param>
        /// <param name="DialogID"></param>
        /// <param name="BLZ"></param>
        /// <param name="UserID"></param>
        /// <param name="PIN"></param>
        /// <param name="SystemID"></param>
        /// <param name="Segments"></param>
        /// <param name="HIRMS_TAN"></param>
        /// <param name="SegmentNum"></param>
        /// <returns></returns>
        public static string Create(int Version, string MsgNum, string DialogID, int BLZ, string UserID, string PIN, 
            string SystemID, string Segments, string HIRMS_TAN, int SegmentNum)
        {
            if (String.IsNullOrEmpty(MsgNum))
                MsgNum = "1";

            MsgNum += "";
            DialogID += "";

            var HEAD_LEN = 29;
            var TRAIL_LEN = 11;

            Random Rnd = new Random();
            int RndNr = Rnd.Next();

            var encHead = string.Empty;
            var sigHead = string.Empty;
            var sigTrail = string.Empty;

            var secRef = Math.Round(Convert.ToDecimal(RndNr.ToString().Replace("-", "")) * 999999 + 1000000);

            string date = Convert.ToString(DateTime.Now.Year) + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd");
            string time = Convert.ToString(DateTime.Now.TimeOfDay).Substring(0, 8).Replace(":", "");

            string TAN_ = string.Empty;

            if (HIRMS_TAN != null)
            {
                if (HIRMS_TAN.Length >= 10)
                {
                    var split = HIRMS_TAN.Split(':');
                    if (split.Length == 2)
                    {
                        HIRMS_TAN = split[0];
                        TAN_ = ":" + split[1];
                    }
                }
            }

            if (Version == 220)
            {
                encHead = "HNVSK:" + Enc.SECFUNC_ENC_PLAIN + ":2+998+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";

                Log.Write(encHead.Replace(UserID, "XXXXXX"));

                sigHead = string.Empty;

                if (HIRMS_TAN == null)
                {
                    sigHead = "HNSHK:2:3+" + "900" + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:999:1+6:10:16+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    Log.Write(sigHead.Replace(UserID, "XXXXXX"));
                }

                else
                {
                    sigHead = "HNSHK:2:3+" + HIRMS_TAN + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:999:1+6:10:16+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    Log.Write(sigHead.Replace(UserID, "XXXXXX"));
                }

                if (String.IsNullOrEmpty(TAN_))
                {
                    sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + PIN + "'";

                    Log.Write("HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + "XXXXXX" + "'");
                }

                else
                {
                    sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + PIN + TAN_ + "'";

                    Log.Write("HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + "XXXXXX" + "XXXXXX" + "'");
                }
            }
            else if (Version == 300)
            {
                if (HIRMS_TAN == null)
                    encHead = "HNVSK:998:3+PIN:1+" + Enc.SECFUNC_ENC_PLAIN + "+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";
                else
                    encHead = "HNVSK:998:3+PIN:2+" + Enc.SECFUNC_ENC_PLAIN + "+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";

                Log.Write(encHead.Replace(UserID, "XXXXXX"));

                if (HIRMS_TAN == null)
                {
                    sigHead = "HNSHK:2:4+PIN:1+" + Sig.SECFUNC_SIG_PT_1STEP + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:999:1+6:10:16+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    Log.Write(sigHead.Replace(UserID, "XXXXXX"));
                }
                else
                {
                    var SECFUNC = HIRMS_TAN.Equals("999") ? "1" : "2";

                    sigHead = "HNSHK:2:4+PIN:" + SECFUNC + "+" + HIRMS_TAN + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:999:1+6:10:16+" + SEG_Country.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    Log.Write(sigHead.Replace(UserID, "XXXXXX"));
                }

                if (String.IsNullOrEmpty(TAN_))
                {
                    sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + PIN + "'";

                    Log.Write("HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + "XXXXXX" + "'");
                }

                else
                {
                    sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + PIN + TAN_ + "'";

                    Log.Write("HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + "XXXXXX" + "XXXXXX" + "'");
                }
            }
            else
            {
                Log.Write("HBCI version not supported");

                return string.Empty;
            }

            Segments = sigHead + Segments + sigTrail;

            var payload = Helper.Encrypt(Segments);

            if (HIRMS_TAN == null)
                Log.Write(payload.Replace(UserID, "XXXXXX").Replace(PIN, "XXXXXX"));
            else if (!String.IsNullOrEmpty(TAN_))
                Log.Write(payload.Replace(UserID, "XXXXXX").Replace(PIN, "XXXXXX").Replace(TAN_, "XXXXXX"));

            var msgLen = HEAD_LEN + TRAIL_LEN + MsgNum.Length * 2 + DialogID.Length + payload.Length + encHead.Length;

            var paddedLen = ("000000000000").Substring(0, 12 - Convert.ToString(msgLen).Length) + Convert.ToString(msgLen);

            var msgHead = string.Empty;

            if (Version == 220)
            {
                msgHead = "HNHBK:1:3+" + paddedLen + "+" + ("220") + "+" + DialogID + "+" + MsgNum + "'";

                Log.Write(msgHead);
            }
            else if (Version == 300)
            {
                msgHead = "HNHBK:1:3+" + paddedLen + "+" + ("300") + "+" + DialogID + "+" + MsgNum + "'";

                Log.Write(msgHead);
            }
            else
            {
                Log.Write("HBCI version not supported");

                return string.Empty;
            }

            var msgEnd = "HNHBS:" + Convert.ToString(SegmentNum + 2) + ":1+" + MsgNum + "'";

            Log.Write(msgEnd);

            UserID = string.Empty;
            PIN = null;

            return msgHead + encHead + payload + msgEnd;
        }

        /// <summary>
        /// Send FinTS message
        /// </summary>
        /// <param name="url"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<string> SendAsync(HttpClient client, string url, string message)
        {
            Log.Write("Connect to FinTS Server");
            Log.Write("Url: " + url);

            // Warning:
            // This writes plain message incl. PIN, UserID and TAN human readable into a textfile!
            if (Trace.Enabled)
                Trace.Write(message);

            try
            {
                var encoding = Encoding.GetEncoding("ISO-8859-1");

                byte[] toEncodeAsBytes = encoding.GetBytes(message);
                var b64 = Encoding.ASCII.GetBytes(Convert.ToBase64String(toEncodeAsBytes));

                var response = await client.PostAsync(url, new ByteArrayContent(b64));
                var content = await response.Content.ReadAsStringAsync();
                var responseMsg = encoding.GetString(Convert.FromBase64String(content));

                // Warning:
                // This writes plain message incl. PIN, UserID and TAN human readable into a textfile!
                if (Trace.Enabled)
                    Trace.Write(responseMsg);

                return responseMsg;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fehler beim Versenden der HBCI-Nachricht.", ex);
            }
        }
    }
}