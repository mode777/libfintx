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
    public class MyTransactionCamt : BaseMyTransaction
    {

        private readonly TANDialog tanDialog;
        private readonly camtVersion camtVers;
        private readonly DateTime? startDate;
        private readonly DateTime? endDate;
        private readonly bool saveCamtFile;
        private readonly string camt;
        private readonly string startDateStr;
        private readonly string endDateStr;
        private int state = 0;

        public MyTransactionCamt(ConnectionContext context, camtVersion camtVers,
            DateTime? startDate = null, DateTime? endDate = null, bool saveCamtFile = false) 
            : base(context)
        {
            this.camtVers = camtVers;
            this.startDate = startDate;
            this.endDate = endDate;
            this.saveCamtFile = saveCamtFile;
            this.camt = string.Empty;
            this.startDateStr = startDate?.ToString("yyyyMMdd");
            this.endDateStr = endDate?.ToString("yyyyMMdd");
        }

        protected override async Task<HBCIDialogResult> InitTransaction()
        {
            var BankCode = await Transaction.HKCAZ(Context, startDateStr, endDateStr, null, camtVers);
            return new HBCIDialogResult<List<TStatement>>(Helper.Parse_BankCode(BankCode), BankCode);
        }

        protected override async Task<HBCIDialogResult> FinishTransaction()
        {
            var BankCode = Result.RawData;
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
                            var camt052f = camt052File.Save(Context.Account, camt, encoding);

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
                            var camt053f = camt053File.Save(Context.Account, camt, encoding);

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
                BankCode_ = await Transaction.HKCAZ(Context, startDateStr, endDateStr, Startpoint, camtVers);
                Result = new HBCIDialogResult<List<TStatement>>(Helper.Parse_BankCode(BankCode_), BankCode_);
                if (!Result.IsSuccess)
                    return Result.TypedResult<List<TStatement>>();

                BankCode_ = Result.RawData;

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
                            var camt052f_ = camt052File.Save(Context.Account, camt);

                            // Process the camt052 file
                            CAMT052Parser.ProcessFile(camt052f_);

                            // Add all items
                            statements.AddRange(CAMT052Parser.statements);
                            break;
                        case camtVersion.camt053:
                            // Save camt053 statement to file
                            var camt053f_ = camt053File.Save(Context.Account, camt);

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

            return Result.TypedResult(statements);
        }
    }
}