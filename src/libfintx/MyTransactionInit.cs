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
    public class MyTransactionInit : BaseMyTransaction
    {
        private readonly bool anonymous;

        public MyTransactionInit(ConnectionContext context, bool anonymous) 
            : base(context)
        {
            this.anonymous = anonymous;
        }

        protected override async Task<HBCIDialogResult> InitTransaction()
        {
            if (Context.SegmentId == null)
                Context.SegmentId = "HKIDN";

            HBCIDialogResult result;
            string BankCode;
            try
            {
                if (Context.CustomerSystemId == null)
                {
                    result = await Synchronization();
                    if (!result.IsSuccess)
                    {
                        Log.Write("Synchronisation failed.");
                        return result;
                    }
                }
                else
                {
                    Context.Segment.HISYN = Context.CustomerSystemId;
                }
                BankCode = await Transaction.INI(Context, anonymous);
            }
            finally
            {
                Context.SegmentId = null;
            }

            var bankMessages = Helper.Parse_BankCode(BankCode);
            result = new HBCIDialogResult(bankMessages, BankCode);
            if (!result.IsSuccess)
                Log.Write("Initialisation failed: " + result);

            return result;
        }

        private async Task<HBCIDialogResult<string>> Synchronization()
        {
            string BankCode = await Transaction.HKSYN(Context);

            var messages = Helper.Parse_BankCode(BankCode);

            return new HBCIDialogResult<string>(messages, BankCode, Context.Segment.HISYN);
        }
    }
}