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
using System.Threading;
using System.Threading.Tasks;

namespace libfintx
{
    public abstract class BaseMyTransaction : IMyTransaction
    {
        private static int lastId = 0;

        public BaseMyTransaction(ConnectionContext context)
        {
            Id = Interlocked.Increment(ref lastId);
            State = TransactionState.NotStarted;
            Context = context;
        }
        
        public int Id { get; }

        public HBCIDialogResult Result { get; protected set; }

        public TransactionState State { get; private set; }
        protected ConnectionContext Context { get; }

        public async Task ContinueAsync(string tan)
        {
            switch (State)
            {
                case TransactionState.NotStarted:
                    State = TransactionState.Running;
                    Result = await InitTransaction();
                    await ContinueWithResult();
                    break;
                case TransactionState.ScaRequired:
                    if (tan != null)
                    {
                        State = TransactionState.Running;
                        Result = await ExecuteTan(tan);
                        if (Result.IsSuccess)
                        {
                            State = TransactionState.Running;
                            await ContinueWithResult();
                        }
                        else
                        {
                            State = TransactionState.Error;
                        }
                        
                    }
                    else
                    {
                        State = TransactionState.Running;
                        Result = await CancelTransaction();
                        State = TransactionState.Cancelled;
                    }
                    break;
                case TransactionState.Error:
                case TransactionState.Running:
                case TransactionState.Fininshed:
                default:
                    break;
            }
        }

        private async Task ContinueWithResult()
        {
            if (!Result.IsSuccess)
            {
                State = TransactionState.Error;
            }
            else if (Result.IsSCARequired)
            {
                State = TransactionState.ScaRequired;
            }
            else
            {
                Result = await FinishTransaction();
                State = TransactionState.Fininshed;                
            }
        }

        protected abstract Task<HBCIDialogResult> InitTransaction();
        protected virtual async Task<HBCIDialogResult> ExecuteTan(string tan)
        {
            var BankCode = await Transaction.TAN(Context, tan);
            var result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);

            return result;
        }

        protected virtual async Task<HBCIDialogResult> CancelTransaction()
        {
            var BankCode = await Transaction.HKEND(Context, Context.Segment.HNHBK);
            return new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
        }

        protected virtual async Task<HBCIDialogResult> FinishTransaction()
        {
            return Result;
        }
    }
}