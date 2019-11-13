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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace libfintx
{
    public class TransactionsCamt : TransactionClass
    {



        /// <summary>
        /// Account transactions in camt format
        /// </summary>
        /// <param name="context">context object must atleast contain the fields: Url, HBCIVersion, UserId, Pin, Blz, Account, IBAN, BIC</param>  
        /// <param name="anonymous"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>
        /// Transactions
        /// </returns>
        public async Task<HBCIDialogResult<List<TStatement>>> ExecuteAsync(ConnectionContext context, TANDialog tanDialog, bool anonymous, camtVersion camtVers,
            DateTime? startDate = null, DateTime? endDate = null, bool saveCamtFile = false)
        {
            // --- Step1
            HBCIDialogResult result = await Init(context, anonymous);
            if (!result.IsSuccess)
                return result.TypedResult<List<TStatement>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<TStatement>>();
            // ---/Step1
            
            // ---Step2
            // Plain camt message
            var camt = string.Empty;

            var startDateStr = startDate?.ToString("yyyyMMdd");
            var endDateStr = endDate?.ToString("yyyyMMdd");

            // Success
            var BankCode = await Transaction.HKCAZ(context, startDateStr, endDateStr, null, camtVers);
            result = new HBCIDialogResult<List<TStatement>>(Helper.Parse_BankCode(BankCode), BankCode);
            if (!result.IsSuccess)
                return result.TypedResult<List<TStatement>>();

            result = await ProcessSCA(context, result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<List<TStatement>>();
            // ---/Step2

            // ---Step3
            return await Process(context, result, camtVers, saveCamtFile, startDateStr, endDateStr);
            // ---/Step3
        }

        private async Task<HBCIDialogResult<List<TStatement>>> Process(ConnectionContext context, HBCIDialogResult result, camtVersion camtVers, bool saveCamtFile, string startDateStr, string endDateStr)
        {
            var BankCode = result.RawData;
            List<TStatement> statements = new List<TStatement>();

            TCAM052TParser CAMT052Parser = null;
            TCAM053TParser CAMT053Parser = null;
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            string BankCode_ = BankCode;
            string camt = string.Empty;

            // Es kann sein, dass in der payload mehrere Dokumente enthalten sind
            var xmlStartIdx = BankCode_.IndexOf("<?xml version=");
            var xmlEndIdx = BankCode_.IndexOf("</Document>") + "</Document>".Length;
            while (xmlStartIdx >= 0)
            {
                if (xmlStartIdx > xmlEndIdx)
                    break;

                camt = "<?xml version=" + Helper.Parse_String(BankCode_, "<?xml version=", "</Document>") + "</Document>";

                switch (camtVers)
                {
                    case camtVersion.camt052:
                        if (CAMT052Parser == null)
                            CAMT052Parser = new TCAM052TParser();

                        if (saveCamtFile)
                        {
                            // Save camt052 statement to file
                            var camt052f = camt052File.Save(context.Account, camt, encoding);

                            // Process the camt052 file
                            CAMT052Parser.ProcessFile(camt052f);
                        }
                        else
                        {
                            CAMT052Parser.ProcessDocument(camt, encoding);
                        }

                        statements.AddRange(CAMT052Parser.statements);
                        break;
                    case camtVersion.camt053:
                        if (CAMT053Parser == null)
                            CAMT053Parser = new TCAM053TParser();

                        if (saveCamtFile)
                        {
                            // Save camt053 statement to file
                            var camt053f = camt053File.Save(context.Account, camt, encoding);

                            // Process the camt053 file
                            CAMT053Parser.ProcessFile(camt053f);
                        }
                        else
                        {
                            CAMT053Parser.ProcessDocument(camt, encoding);
                        }

                        statements.AddRange(CAMT053Parser.statements);
                        break;
                }

                BankCode_ = BankCode_.Substring(xmlEndIdx);
                xmlStartIdx = BankCode_.IndexOf("<?xml version");
                xmlEndIdx = BankCode_.IndexOf("</Document>") + "</Document>".Length;
            }

            BankCode_ = BankCode;

            while (BankCode_.Contains("+3040::"))
            {
                string Startpoint = new Regex(@"\+3040::[^:]+:(?<startpoint>[^']+)'").Match(BankCode_).Groups["startpoint"].Value;
                BankCode_ = await Transaction.HKCAZ(context, startDateStr, endDateStr, Startpoint, camtVers);
                result = new HBCIDialogResult<List<TStatement>>(Helper.Parse_BankCode(BankCode_), BankCode_);
                if (!result.IsSuccess)
                    return result.TypedResult<List<TStatement>>();

                BankCode_ = result.RawData;

                // Es kann sein, dass in der payload mehrere Dokumente enthalten sind
                xmlStartIdx = BankCode_.IndexOf("<?xml version=");
                xmlEndIdx = BankCode_.IndexOf("</Document>") + "</Document>".Length;

                while (xmlStartIdx >= 0)
                {
                    if (xmlStartIdx > xmlEndIdx)
                        break;

                    camt = "<?xml version=" + Helper.Parse_String(BankCode_, "<?xml version=", "</Document>") + "</Document>";

                    switch (camtVers)
                    {
                        case camtVersion.camt052:
                            // Save camt052 statement to file
                            var camt052f_ = camt052File.Save(context.Account, camt);

                            // Process the camt052 file
                            CAMT052Parser.ProcessFile(camt052f_);

                            // Add all items
                            statements.AddRange(CAMT052Parser.statements);
                            break;
                        case camtVersion.camt053:
                            // Save camt053 statement to file
                            var camt053f_ = camt053File.Save(context.Account, camt);

                            // Process the camt053 file
                            CAMT053Parser.ProcessFile(camt053f_);

                            // Add all items to existing statement
                            statements.AddRange(CAMT053Parser.statements);
                            break;
                    }

                    BankCode_ = BankCode_.Substring(xmlEndIdx);
                    xmlStartIdx = BankCode_.IndexOf("<?xml version");
                    xmlEndIdx = BankCode_.IndexOf("</Document>") + "</Document>".Length;
                }
            }

            return result.TypedResult(statements);
        }
    }
}