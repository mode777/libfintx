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
using System.Threading.Tasks;

namespace libfintx
{
    public class MyTransactionBalance : BaseMyTransaction
    {
        public MyTransactionBalance(ConnectionContext context) 
            : base(context)
        {
        }

        protected override async Task<HBCIDialogResult> InitTransaction()
        {
            var code = await Transaction.HKSAL(Context);
            return new HBCIDialogResult(Helper.Parse_BankCode(code), code);
        }

        protected override async Task<HBCIDialogResult> FinishTransaction()
        {
            var code = Result.RawData;
            var balance = Helper.Parse_Balance(code);
            return Result.TypedResult(balance);
        }
    }
}