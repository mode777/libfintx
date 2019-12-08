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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libfintx
{
    public class CompositeTransaction
    {
        private readonly IEnumerator<IMyTransaction> transactions;
        private IMyTransaction current;
        private Queue<HBCIDialogResult> results = new Queue<HBCIDialogResult>();

        public CompositeTransaction(params IMyTransaction[] transactions)
        {
            this.transactions = ((IEnumerable<IMyTransaction>)transactions).GetEnumerator();
            this.transactions.MoveNext();
            current = this.transactions.Current; 
        }

        public bool TryDequeue(out HBCIDialogResult result)
        {
            if(results.Count == 0)
            {
                result = null;
                return false;
            }
            else
            {
                result = results.Dequeue();
                return true;
            }

        }

        public TransactionState State => current?.State ?? TransactionState.Fininshed;

        public IMyTransaction Current => current;

        public async Task ContinueAsync(string tan)
        {
            if (current == null)
                return;

            await current.ContinueAsync(tan);

            if(current.State == TransactionState.Fininshed)
            {
                results.Enqueue(current.Result);

                if (this.transactions.MoveNext())
                {                    
                    current = this.transactions.Current;
                    await ContinueAsync(null);
                }
            }

        }
    }
}