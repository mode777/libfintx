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

using System.Collections.Generic;

using static libfintx.INI;
using static libfintx.HKEND;
using static libfintx.HKSAL;
using static libfintx.HKKAZ;
using static libfintx.HKCCS;
using static libfintx.HKCSE;
using static libfintx.HKCCM;
using static libfintx.HKCME;
using static libfintx.HKCUM;
using static libfintx.HKDSE;
using static libfintx.HKDME;
using static libfintx.HKPPD;
using static libfintx.HKCDE;
using static libfintx.HKCDN;
using static libfintx.HKCDL;
using static libfintx.HKCSB;
using static libfintx.HKCDB;
using static libfintx.HKSYN;
using static libfintx.TAN;
using static libfintx.TAN4;
using static libfintx.HKTAB;
using static libfintx.HKCAZ;
using libfintx.Data;
using System;
using System.Threading.Tasks;

namespace libfintx
{
    public static class Transaction
    {
        public static async Task<string> INI(ConnectionContext context, bool Anonymous)
        {
            return await Init_INI(context, Anonymous);
        }

        public static async Task<string> HKEND(ConnectionContext context, string dialogId)
        {
            return await Init_HKEND(context, dialogId);
        }

        public static async Task<string> HKSYN(ConnectionContext context)
        {
            return await Init_HKSYN(context);
        }

        public static async Task<string> HKSAL(ConnectionContext context)
        {
            return await Init_HKSAL(context);
        }

        public static async Task<string> HKKAZ(ConnectionContext context, string FromDate, string ToDate, string Startpoint)
        {
            return await Init_HKKAZ(context, FromDate, ToDate, Startpoint);
        }

        public static async Task<string> HKCCS(ConnectionContext context, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage)
        {
            return await Init_HKCCS(context, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage);
        }

        public static async Task<string> HKCSE(ConnectionContext context, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime ExecutionDay)
        {
            return await Init_HKCSE(context, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, ExecutionDay);
        }

        public static async Task<string> HKCCM(ConnectionContext context, List<pain00100203_ct_data> PainData, string NumberofTransactions, decimal TotalAmount)
        {
            return await Init_HKCCM(context, PainData, NumberofTransactions, TotalAmount);
        }

        public static async Task<string> HKCME(ConnectionContext context, List<pain00100203_ct_data> PainData, string NumberofTransactions, decimal TotalAmount, DateTime ExecutionDay)
        {
            return await Init_HKCME(context, PainData, NumberofTransactions, TotalAmount, ExecutionDay);
        }

        public static async Task<string> HKCUM(ConnectionContext context, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage)
        {
            return await Init_HKCUM(context, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage);
        }

        public static async Task<string> HKDSE(ConnectionContext context, string Payer, string PayerIBAN, string PayerBIC, decimal Amount, string Usage,
            DateTime SettlementDate, string MandateNumber, DateTime MandateDate, string CeditorIDNumber)
        {
            return await Init_HKDSE(context, Payer, PayerIBAN, PayerBIC, Amount, Usage, SettlementDate, MandateNumber, MandateDate, CeditorIDNumber);
        }

        public static async Task<string> HKDME(ConnectionContext context, DateTime SettlementDate, List<pain00800202_cc_data> PainData, string NumberofTransactions, decimal TotalAmount)
        {
            return await Init_HKDME(context, SettlementDate, PainData, NumberofTransactions, TotalAmount);
        }

        public static async Task<string> HKPPD(ConnectionContext context, int MobileServiceProvider, string PhoneNumber, int Amount)
        {
            return await Init_HKPPD(context, MobileServiceProvider, PhoneNumber, Amount);
        }

        public static async Task<string> HKCDE(ConnectionContext context, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime FirstTimeExecutionDay, TimeUnit TimeUnit, string Rota, int ExecutionDay)
        {
            return await Init_HKCDE(context, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, FirstTimeExecutionDay, TimeUnit, Rota, ExecutionDay);
        }

        public static async Task<string> HKCDN(ConnectionContext context, string OrderId, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime FirstTimeExecutionDay, TimeUnit TimeUnit, string Rota, int ExecutionDay)
        {
            return await Init_HKCDN(context, OrderId, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, FirstTimeExecutionDay, TimeUnit, Rota, ExecutionDay);
        }

        public static async Task<string> HKCDL(ConnectionContext context, string OrderId, string Receiver, string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime FirstTimeExecutionDay, TimeUnit TimeUnit, string Rota, int ExecutionDay)
        {
            return await Init_HKCDL(context, OrderId, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, FirstTimeExecutionDay, TimeUnit, Rota, ExecutionDay);
        }

        public static async Task<string> HKCSB(ConnectionContext context)
        {
            return await Init_HKCSB(context);
        }

        public static async Task<string> HKCDB(ConnectionContext context)
        {
            return await Init_HKCDB(context);
        }

        public static async Task<string> TAN(ConnectionContext context, string TAN)
        {
            return await Send_TAN(context, TAN);
        }

        public static async Task<string> TAN4(ConnectionContext context, string TAN, string MediumName)
        {
            return await Send_TAN4(context, TAN, MediumName);
        }

        public static async Task<string> HKTAB(ConnectionContext context)
        {
            return await Init_HKTAB(context);
        }

        public static async Task<string> HKCAZ(ConnectionContext context, string FromDate, string ToDate, string Startpoint, camtVersion camtVers)
        {
            return await Init_HKCAZ(context, FromDate, ToDate, Startpoint, camtVers);
        }
    }
}
