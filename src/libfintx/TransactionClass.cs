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
using System.Threading.Tasks;

namespace libfintx
{
    public class TransactionClass
    {
        protected async Task<HBCIDialogResult> Init(ConnectionContext context, bool anonymous)
        {
            if (context.SegmentId == null)
                context.SegmentId = "HKIDN";

            HBCIDialogResult result;
            string BankCode;
            try
            {
                if (context.CustomerSystemId == null)
                {
                    result = await Synchronization(context);
                    if (!result.IsSuccess)
                    {
                        Log.Write("Synchronisation failed.");
                        return result;
                    }
                }
                else
                {
                    context.Segment.HISYN = context.CustomerSystemId;
                }
                BankCode = await Transaction.INI(context, anonymous);
            }
            finally
            {
                context.SegmentId = null;
            }

            var bankMessages = Helper.Parse_BankCode(BankCode);
            result = new HBCIDialogResult(bankMessages, BankCode);
            if (!result.IsSuccess)
                Log.Write("Initialisation failed: " + result);

            return result;
        }

        /// <summary>
        /// Synchronize bank connection
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <returns>
        /// Customer System ID
        /// </returns>
        public async Task<HBCIDialogResult<string>> Synchronization(ConnectionContext context)
        {
            string BankCode = await Transaction.HKSYN(context);

            var messages = Helper.Parse_BankCode(BankCode);

            return new HBCIDialogResult<string>(messages, BankCode, context.Segment.HISYN);
        }

        protected async Task<HBCIDialogResult> ProcessSCA(ConnectionContext context, HBCIDialogResult result, TANDialog tanDialog)
        {
            tanDialog.DialogResult = result;
            if (result.IsSCARequired)
            {
                var tan = Helper.WaitForTAN(context, result, tanDialog);
                if (tan == null)
                {
                    var BankCode = await Transaction.HKEND(context, context.Segment.HNHBK);
                    result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
                }
                else
                {
                    result = await TAN(context, tan);
                }
            }

            return result;
        }

        /// <summary>
        /// Confirm order with TAN
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <param name="TAN"></param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> TAN(ConnectionContext context, string TAN)
        {
            var BankCode = await Transaction.TAN(context, TAN);
            var result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);

            return result;
        }
    }
}