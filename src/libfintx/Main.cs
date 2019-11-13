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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static libfintx.HKCDE;
using System.Threading.Tasks;

namespace libfintx
{
    public class Main
    {
        /// <summary>
        /// Resets all temporary values. Should be used when switching to another bank connection.
        /// </summary>
        public static void Reset(ConnectionContext context)
        {
            context.Segment = new Segment();
            TransactionConsole.Output = null;
        }

        /// <summary>
        /// Synchronize bank connection
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <returns>
        /// Customer System ID
        /// </returns>
        public static async Task<HBCIDialogResult<string>> Synchronization(ConnectionContext context)
        {
            string BankCode = await Transaction.HKSYN(context);

            var messages = Helper.Parse_BankCode(BankCode);

            return new HBCIDialogResult<string>(messages, BankCode, context.Segment.HISYN);
        }

        private static async Task<HBCIDialogResult> Init(ConnectionContext context, bool anonymous)
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
        /// Account balance
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, Account, IBAN, BIC</param>
        /// <param name="Anonymous"></param>
        /// <returns>
        /// Structured information about balance, creditline and used currency
        /// </returns>
        public static async Task<HBCIDialogResult<AccountBalance>> Balance(ConnectionContext context, TANDialog tanDialog, bool anonymous)
        {
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            // Success
            var BankCode = await Transaction.HKSAL(context);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            BankCode = result.RawData;
            var balance = Helper.Parse_Balance(BankCode);
            return result.TypedResult(balance);
        }

        public static async Task<HBCIDialogResult<List<AccountInformations>>> Accounts(ConnectionContext context, TANDialog tanDialog, bool anonymous)
        {
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result.TypedResult<List<AccountInformations>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<AccountInformations>>();

            return new HBCIDialogResult<List<AccountInformations>>(result.Messages, context.UPD.Value, context.UPD.HIUPD.AccountList);
        }

        /// <summary>
        /// Account transactions in SWIFT-format
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, Account, IBAN, BIC</param>  
        /// <param name="anonymous"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public static async Task<HBCIDialogResult<List<SWIFTStatement>>> Transactions(ConnectionContext context, TANDialog tanDialog, bool anonymous, DateTime? startDate = null, DateTime? endDate = null, bool saveMt940File = false)
        {
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result.TypedResult<List<SWIFTStatement>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<SWIFTStatement>>();

            var startDateStr = startDate?.ToString("yyyyMMdd");
            var endDateStr = endDate?.ToString("yyyyMMdd");

            // Success
            var BankCode = await Transaction.HKKAZ(context, startDateStr, endDateStr, null);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result.TypedResult<List<SWIFTStatement>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<SWIFTStatement>>();

            BankCode = result.RawData;
            StringBuilder Transactions = new StringBuilder();

            var regex = new Regex(@"HIKAZ:.+?@\d+@(\n|\r|\r\n)*(?<payload>.+?)('HNSHA|''HNHBS)", RegexOptions.Singleline);
            var match = regex.Match(BankCode);
            if (match.Success)
                Transactions.Append(match.Groups["payload"].Value);

            string BankCode_ = BankCode;
            while (BankCode_.Contains("+3040::"))
            {
                Helper.Parse_Message(context, BankCode_);

                var Startpoint = new Regex(@"\+3040::[^:]+:(?<startpoint>[^']+)'").Match(BankCode_).Groups["startpoint"].Value;

                BankCode_ = await Transaction.HKKAZ(context, startDateStr, endDateStr, Startpoint);
                result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode_), BankCode_);
                if (!result.IsSuccess)
                    return result.TypedResult<List<SWIFTStatement>>();

                result = await ProcessSCA(context, result, tanDialog);
                if (!result.IsSuccess)
                    return result.TypedResult<List<SWIFTStatement>>();

                BankCode_ = result.RawData;
                match = regex.Match(BankCode_);
                if (match.Success)
                    Transactions.Append(match.Groups["payload"].Value);
            }

            var swiftStatements = new List<SWIFTStatement>();

            swiftStatements.AddRange(new MT940().Serialize(Transactions.ToString(), context.Account, saveMt940File));

            return result.TypedResult(swiftStatements);
        }

        /// <summary>
        /// Account transactions in simplified libfintx-format
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, Account, IBAN, BIC</param>  
        /// <param name="anonymous"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public static async Task<HBCIDialogResult<List<AccountTransaction>>> TransactionsSimple(ConnectionContext context, TANDialog tanDialog, bool anonymous, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await Transactions(context, tanDialog, anonymous, startDate, endDate);
            if (!result.IsSuccess)
                return result.TypedResult<List<AccountTransaction>>();

            var transactionList = new List<AccountTransaction>();
            foreach (var swiftStatement in result.Data)
            {
                foreach (var swiftTransaction in swiftStatement.SWIFTTransactions)
                {
                    transactionList.Add(new AccountTransaction()
                    {
                        OwnerAccount = swiftStatement.accountCode,
                        OwnerBankcode = swiftStatement.bankCode,
                        PartnerBIC = swiftTransaction.bankCode,
                        PartnerIBAN = swiftTransaction.accountCode,
                        PartnerName = swiftTransaction.partnerName,
                        RemittanceText = swiftTransaction.description,
                        TransactionType = swiftTransaction.text,
                        TransactionDate = swiftTransaction.inputDate,
                        ValueDate = swiftTransaction.valueDate
                    });
                }
            }

            return result.TypedResult(transactionList);
        }

        /// <summary>
        /// Transfer money - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> Transfer(ConnectionContext context, TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, string HIRMS, bool anonymous)
        {
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCCS(context, receiverName, receiverIBAN, receiverBIC, amount, purpose);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Transfer money at a certain time - General method
        /// </summary>       
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="executionDay"></param>
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> Transfer_Terminated(ConnectionContext context, TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, DateTime executionDay, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCSE(context, receiverName, receiverIBAN, receiverBIC, amount, purpose, executionDay);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collective transfer money - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> CollectiveTransfer(ConnectionContext context, TANDialog tanDialog, List<pain00100203_ct_data> painData,
            string numberOfTransactions, decimal totalAmount, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCCM(context, painData, numberOfTransactions, totalAmount);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collective transfer money terminated - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>
        /// <param name="ExecutionDay"></param>
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param> 
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> CollectiveTransfer_Terminated(ConnectionContext context, TANDialog tanDialog, List<pain00100203_ct_data> painData,
            string numberOfTransactions, decimal totalAmount, DateTime executionDay, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCME(context, painData, numberOfTransactions, totalAmount, executionDay);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Rebook money from one to another account - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>  
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> Rebooking(ConnectionContext context, TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCUM(context, receiverName, receiverIBAN, receiverBIC, amount, purpose);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collect money from another account - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="payerName">Name of the payer</param>
        /// <param name="payerIBAN">IBAN of the payer</param>
        /// <param name="payerBIC">BIC of the payer</param>         
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>    
        /// <param name="settlementDate"></param>
        /// <param name="mandateNumber"></param>
        /// <param name="mandateDate"></param>
        /// <param name="creditorIdNumber"></param>
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> Collect(ConnectionContext context, TANDialog tanDialog, string payerName, string payerIBAN, string payerBIC,
            decimal amount, string purpose, DateTime settlementDate, string mandateNumber, DateTime mandateDate, string creditorIdNumber,
            string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKDSE(context, payerName, payerIBAN, payerBIC, amount, purpose, settlementDate, mandateNumber, mandateDate, creditorIdNumber);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collective collect money from other accounts - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>  
        /// <param name="settlementDate"></param>
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>        
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> CollectiveCollect(ConnectionContext context, TANDialog tanDialog, DateTime settlementDate, List<pain00800202_cc_data> painData,
           string numberOfTransactions, decimal totalAmount, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKDME(context, settlementDate, painData, numberOfTransactions, totalAmount);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Load mobile phone prepaid card - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC</param>  
        /// <param name="mobileServiceProvider"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="amount">Amount to transfer</param>            
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> Prepaid(ConnectionContext context, TANDialog tanDialog, int mobileServiceProvider, string phoneNumber,
            int amount, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKPPD(context, mobileServiceProvider, phoneNumber, amount);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Submit bankers order - General method
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC, AccountHolder</param>       
        /// <param name="receiverName"></param>
        /// <param name="receiverIBAN"></param>
        /// <param name="receiverBIC"></param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="firstTimeExecutionDay"></param>
        /// <param name="timeUnit"></param>
        /// <param name="rota"></param>
        /// <param name="executionDay"></param>
        /// <param name="HIRMS">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <param name="pictureBox">Picturebox which shows the TAN</param>
        /// <param name="anonymous"></param>
        /// <param name="flickerImage">(Out) reference to an image object that shall receive the FlickerCode as GIF image</param>
        /// <param name="flickerWidth">Width of the flicker code</param>
        /// <param name="flickerHeight">Height of the flicker code</param>
        /// <param name="renderFlickerCodeAsGif">Renders flicker code as GIF, if 'true'</param>
        /// <returns>
        /// Bank return codes
        /// </returns>

        public static async Task<HBCIDialogResult> SubmitBankersOrder(ConnectionContext context, TANDialog tanDialog, string receiverName, string receiverIBAN,
           string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, TimeUnit timeUnit, string rota,
           int executionDay, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCDE(context, receiverName, receiverIBAN, receiverBIC, amount, purpose, firstTimeExecutionDay, timeUnit, rota, executionDay);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        public static async Task<HBCIDialogResult> ModifyBankersOrder(ConnectionContext context, TANDialog tanDialog, string OrderId, string receiverName, string receiverIBAN,
           string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, TimeUnit timeUnit, string rota,
           int executionDay, string HIRMS, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCDN(context, OrderId, receiverName, receiverIBAN, receiverBIC, amount, purpose, firstTimeExecutionDay, timeUnit, rota, executionDay);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        public static async Task<HBCIDialogResult> DeleteBankersOrder(ConnectionContext context, TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN,
            string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, HKCDE.TimeUnit timeUnit, string rota, int executionDay, string HIRMS,
            bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!String.IsNullOrEmpty(HIRMS))
                context.Segment.HIRMS = HIRMS;

            var BankCode = await Transaction.HKCDL(context, orderId, receiverName, receiverIBAN, receiverBIC, amount, purpose, firstTimeExecutionDay, timeUnit, rota, executionDay);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

            return result;
        }

        /// <summary>
        /// Get banker's orders
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC</param>         
        /// <param name="anonymous"></param>
        /// <returns>
        /// Banker's orders
        /// </returns>
        public static async Task<HBCIDialogResult<List<BankersOrder>>> GetBankersOrders(ConnectionContext context, TANDialog tanDialog, bool anonymous)
        {
            var result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result.TypedResult<List<BankersOrder>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<BankersOrder>>();

            // Success
            var BankCode = await Transaction.HKCDB(context);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result.TypedResult<List<BankersOrder>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<BankersOrder>>();

            BankCode = result.RawData;
            var startIdx = BankCode.IndexOf("HICDB");
            if (startIdx < 0)
                return result.TypedResult<List<BankersOrder>>();

            List<BankersOrder> data = new List<BankersOrder>();

            var BankCode_ = BankCode.Substring(startIdx);
            for (; ; )
            {
                var match = Regex.Match(BankCode_, @"HICDB.+?(<\?xml.+?</Document>)\+(.*?)\+(\d*):([MW]):(\d+):(\d+)", RegexOptions.Singleline);
                if (match.Success)
                {
                    var xml = match.Groups[1].Value;
                    // xml ist UTF-8
                    xml = Converter.ConvertEncoding(xml, Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8);

                    var orderId = match.Groups[2].Value;

                    var firstExecutionDateStr = match.Groups[3].Value;
                    DateTime? firstExecutionDate = !string.IsNullOrWhiteSpace(firstExecutionDateStr) ? DateTime.ParseExact(firstExecutionDateStr, "yyyyMMdd", CultureInfo.InvariantCulture) : default(DateTime?);

                    var timeUnitStr = match.Groups[4].Value;
                    TimeUnit timeUnit = timeUnitStr == "M" ? TimeUnit.Monthly : TimeUnit.Weekly;

                    var rota = match.Groups[5].Value;

                    var executionDayStr = match.Groups[6].Value;
                    int executionDay = Convert.ToInt32(executionDayStr);

                    var painData = pain00100103_ct_data.Create(xml);

                    if (firstExecutionDate.HasValue && executionDay > 0)
                    {
                        var item = new BankersOrder(orderId, painData, firstExecutionDate.Value, timeUnit, rota, executionDay);
                        data.Add(item);
                    }
                }

                var endIdx = BankCode_.IndexOf("'");
                if (BankCode_.Length <= endIdx + 1)
                    break;

                BankCode_ = BankCode_.Substring(endIdx + 1);
                startIdx = BankCode_.IndexOf("HICDB");
                if (startIdx < 0)
                    break;
            }

            // Success
            return result.TypedResult(data);
        }

        /// <summary>
        /// Get terminated transfers
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, IBAN, BIC</param>         
        /// <param name="anonymous"></param>
        /// <returns>
        /// Banker's orders
        /// </returns>
        public static async Task<HBCIDialogResult> GetTerminatedTransfers(ConnectionContext context, TANDialog tanDialog, bool anonymous)
        {
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result;

            // Success
            var BankCode = await Transaction.HKCSB(context);
            result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result;

            result = await ProcessSCA(context, result, tanDialog);

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
        public static async Task<HBCIDialogResult> TAN(ConnectionContext context, string TAN)
        {
            var BankCode = await Transaction.TAN(context, TAN);
            var result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);

            return result;
        }

        /// <summary>
        /// Confirm order with TAN
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <param name="TAN"></param>
        /// <param name="MediumName"></param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public static async Task<HBCIDialogResult> TAN4(ConnectionContext context, string TAN, string MediumName)
        {
            var BankCode = await Transaction.TAN4(context, TAN, MediumName);
            var result = new HBCIDialogResult(Helper.Parse_BankCode(BankCode), BankCode);

            return result;
        }

        /// <summary>
        /// Request tan medium name
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <returns>
        /// TAN Medium Name
        /// </returns>
        public static async Task<HBCIDialogResult<List<string>>> RequestTANMediumName(ConnectionContext context)
        {
            context.SegmentId = "HKTAB";

            HBCIDialogResult result = await Init(context, false);
            if (!result.IsSuccess)
                return result.TypedResult<List<string>>();

            // Should not be needed when processing HKTAB
            //result = await ProcessSCA(context, result, tanDialog);
            //if (!result.IsSuccess)
            //    return result.TypedResult<List<string>>();

            var BankCode = await Transaction.HKTAB(context);
            result = new HBCIDialogResult<List<string>>(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result.TypedResult<List<string>>();

            // Should not be needed when processing HKTAB
            //result = await ProcessSCA(context, result, tanDialog);
            //if (!result.IsSuccess)
            //    return result.TypedResult<List<string>>();

            BankCode = result.RawData;
            var BankCode_ = "HITAB" + Helper.Parse_String(BankCode, "'HITAB", "'");
            return result.TypedResult(Helper.Parse_TANMedium(BankCode_));
        }

        /// <summary>
        /// TAN scheme
        /// </summary>
        /// <returns>
        /// TAN mechanism
        /// </returns>
        public static string TAN_Scheme(ConnectionContext context)
        {
            return context.Segment.HIRMSf;
        }

        /// <summary>
        /// Set assembly information
        /// </summary>
        /// <param name="Buildname"></param>
        /// <param name="Version"></param>
        public static void Assembly(string Buildname, string Version)
        {
            Program.Buildname = Buildname;
            Program.Version = Version;

            Log.Write(Buildname);
            Log.Write(Version);
        }


        /// <summary>
        /// Set assembly information automatically
        /// </summary>
        public static void Assembly()
        {
            var assemInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            Program.Buildname = assemInfo.Name;
            Program.Version = $"{assemInfo.Version.Major}.{assemInfo.Version.Minor}";

            Log.Write(Program.Buildname);
            Log.Write(Program.Version);
        }


        /// <summary>
        /// Get assembly buildname
        /// </summary>
        /// <returns>
        /// Buildname
        /// </returns>
        public static string Buildname()
        {
            return Program.Buildname;
        }

        /// <summary>
        /// Get assembly version
        /// </summary>
        /// <returns>
        /// Version
        /// </returns>
        public static string Version()
        {
            return Program.Version;
        }

        /// <summary>
        /// Transactions output console
        /// </summary>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public static string Transaction_Output()
        {
            return TransactionConsole.Output;
        }

        /// <summary>
        /// Enable / Disable Tracing
        /// </summary>
        public static void Tracing(bool Enabled, bool Formatted = false, int maxFileSizeMB = 10)
        {
            Trace.Enabled = Enabled;
            Trace.Formatted = Formatted;
            Trace.MaxFileSize = maxFileSizeMB;
        }

        /// <summary>
        /// Enable / Disable Debugging
        /// </summary>
        public static void Debugging(bool Enabled)
        {
            DEBUG.Enabled = Enabled;
        }

        /// <summary>
        /// Enable / Disable Logging
        /// </summary>
        public static void Logging(bool Enabled, int maxFileSizeMB = 10)
        {
            Log.Enabled = Enabled;
            Log.MaxFileSize = maxFileSizeMB;
        }

        private static async Task<HBCIDialogResult> ProcessSCA(ConnectionContext context, HBCIDialogResult result, TANDialog tanDialog)
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
    }
}