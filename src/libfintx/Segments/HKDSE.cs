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

using libfintx.Data;
using System;

namespace libfintx
{
    public static class HKDSE
    {
        /// <summary>
        /// Collect
        /// </summary>
        public static string Init_HKDSE(ConnectionDetails connectionDetails, string Payer, string PayerIBAN, string PayerBIC, decimal Amount, string Usage, DateTime SettlementDate, string MandateNumber, DateTime MandateDate, string CreditorIDNumber)
        {
            Log.Write("Starting job HKDSE: Collect money");

            SEG.NUM = SEGNUM.SETInt(4);

            string segments = "HKDSE:" + SEG.NUM + ":1+" + connectionDetails.IBAN + ":" + connectionDetails.BIC + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.008.002.02+@@";

            var message = pain00800202.Create(connectionDetails.AccountHolder, connectionDetails.IBAN, connectionDetails.BIC, Payer, PayerIBAN, PayerBIC, Amount, Usage, SettlementDate, MandateNumber, MandateDate, CreditorIDNumber);

            segments = segments.Replace("@@", "@" + (message.Length - 1) + "@") + message;

            if (Helper.IsTANRequired("HKDSE"))
            {
                SEG.NUM = SEGNUM.SETInt(4);
                segments = HKTAN.Init_HKTAN(segments);
            }

            var TAN = FinTSMessage.Send(connectionDetails.Url, FinTSMessage.Create(connectionDetails.HBCIVersion, Segment.HNHBS, Segment.HNHBK, connectionDetails.BlzPrimary, connectionDetails.UserId, connectionDetails.Pin, Segment.HISYN, segments, Segment.HIRMS, SEG.NUM));

            Segment.HITAN = Helper.Parse_String(Helper.Parse_String(TAN, "HITAN", "'").Replace("?+", "??"), "++", "+").Replace("??", "?+");

            Helper.Parse_Message(TAN);

            return TAN;
        }
    }
}
