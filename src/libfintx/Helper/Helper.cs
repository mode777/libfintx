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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using libfintx.Data;

namespace libfintx
{
    public static class Helper
    {
        /// <summary>
        /// Regex pattern for HIRMG/HIRMS messages.
        /// </summary>
        private const string PatternResultMessage = @"(\d{4}):.*?:(.+)";

        /// <summary>
        /// Pad zeros
        /// </summary>
        /// <returns></returns>
        private static byte[] PadZero()
        {
            var buffer = new byte[16];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }

            return buffer;
        }

        /// <summary>
        /// Combine byte arrays
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];

            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);

            return ret;
        }

        /// <summary>
        /// Encode to Base64
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        static public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            string returnValue = Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Decode from Base64
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            string returnValue = Encoding.ASCII.GetString(encodedDataAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Decode from Base64 default
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        static public string DecodeFrom64EncodingDefault(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            string returnValue = Encoding.GetEncoding("ISO-8859-1").GetString(encodedDataAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Encrypt -> HNVSD
        /// </summary>
        /// <param name="Segments"></param>
        /// <returns></returns>
        static public string Encrypt(string Segments)
        {
            return "HNVSD:999:1+@" + Segments.Length + "@" + Segments + "'";
        }

        /// <summary>
        /// Extract value from string
        /// </summary>
        /// <param name="StrSource"></param>
        /// <param name="StrStart"></param>
        /// <param name="StrEnd"></param>
        /// <returns></returns>
        static public string Parse_String(string StrSource, string StrStart, string StrEnd)
        {
            int Start, End;

            if (StrSource.Contains(StrStart) && StrSource.Contains(StrEnd))
            {
                Start = StrSource.IndexOf(StrStart, 0) + StrStart.Length;
                End = StrSource.IndexOf(StrEnd, Start);

                return StrSource.Substring(Start, End - Start);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parsing segment -> UPD, BPD
        /// </summary>
        /// <param name="context"></param>
        /// <param name="segmentStr"></param>
        /// <returns></returns>
        public static List<HBCIBankMessage> Parse_Segment(ConnectionContext context, string segmentStr)
        {
            try
            {
                List<HBCIBankMessage> result = new List<HBCIBankMessage>();

                string[] values = segmentStr.Split('\'');

                string msg = string.Join(Environment.NewLine, values);

                string bpd = string.Empty;
                string upd = string.Empty;

                var bpaMatch = Regex.Match(msg, @"(HIBPA.+?)\b(HNHBS|HISYN|HIUPA)\b", RegexOptions.Singleline);
                if (bpaMatch.Success)
                    bpd = bpaMatch.Groups[1].Value;

                var upaMatch = Regex.Match(msg, @"(HIUPA.+?)\b(HITAN|HNHBS)\b", RegexOptions.Singleline);
                if (upaMatch.Success)
                    upd = upaMatch.Groups[1].Value;

                // BPD
                SaveBPD(context.Blz, bpd);
                context.BPD = BPD.Parse_BPD(bpd);

                // UPD
                SaveUPD(context.Blz, context.UserId, upd);
                context.UPD = UPD.Parse_UPD(upd);

                foreach (var item in values)
                {
                    if (item.Contains("HIRMG"))
                    {
                        // HIRMG:2:2+9050::Die Nachricht enthÃ¤lt Fehler.+9800::Dialog abgebrochen+9010::Initialisierung fehlgeschlagen, Auftrag nicht bearbeitet.
                        // HIRMG:2:2+9800::Dialogabbruch.

                        string[] HIRMG_messages = item.Split('+');
                        foreach (var HIRMG_message in HIRMG_messages)
                        {
                            var message = Parse_BankCode_Message(HIRMG_message);
                            if (message != null)
                                result.Add(message);
                        }
                    }

                    if (item.Contains("HIRMS"))
                    {
                        // HIRMS:3:2:2+9942::PIN falsch. Zugang gesperrt.'
                        string[] HIRMS_messages = item.Split('+');
                        foreach (var HIRMS_message in HIRMS_messages)
                        {
                            var message = Parse_BankCode_Message(HIRMS_message);
                            if (message != null)
                                result.Add(message);
                        }

                        var securityMessage = result.FirstOrDefault(m => m.Code == "3920");
                        if (securityMessage != null)
                        {
                            string message = securityMessage.Message;

                            string TAN = string.Empty;
                            string TANf = string.Empty;

                            string[] procedures = Regex.Split(message, @"\D+");

                            foreach (string value in procedures)
                            {
                                int i;
                                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out i))
                                {
                                    if (Convert.ToString(i).StartsWith("9"))
                                    {
                                        if (String.IsNullOrEmpty(TAN))
                                            TAN = i.ToString();

                                        if (String.IsNullOrEmpty(TANf))
                                            TANf = i.ToString();
                                        else
                                            TANf += $";{i}";
                                    }
                                }
                            }
                            if (string.IsNullOrEmpty(context.Segment.HIRMS))
                            {
                                context.Segment.HIRMS = TAN;
                            }
                            else
                            {
                                if (!TANf.Contains(context.Segment.HIRMS))
                                    throw new Exception($"Invalid HIRMS/Tan-Mode {context.Segment.HIRMS} detected. Please choose one of the allowed modes: {TANf}");
                            }
                            context.Segment.HIRMSf = TANf;

                            // Parsing TAN processes
                            if (!String.IsNullOrEmpty(context.Segment.HIRMS))
                                Parse_TANProcesses(context, bpd);

                        }
                    }

                    if (item.Contains("HNHBK"))
                    {
                        var ID = Parse_String(item.ToString(), "+1+", ":1");
                        context.Segment.HNHBK = ID;
                    }

                    if (item.Contains("HISYN"))
                    {
                        var ID = item.Substring(item.IndexOf("+") + 1);
                        context.Segment.HISYN = ID;

                        Log.Write("Customer System ID: " + ID);
                    }

                    if (item.Contains("HNHBS"))
                    {
                        var item_ = item + "'";

                        var MSG = Parse_String(item_.Replace("HNHBS:", ""), "+", "'");

                        if (MSG.Equals("0") || MSG == null)
                            context.Segment.HNHBS = "2";
                        else
                            context.Segment.HNHBS = Convert.ToString(Convert.ToInt16(MSG) + 1);
                    }

                    if (item.Contains("HISALS"))
                    {
                        var SEG = Parse_String(item.Replace("HISALS:", ""), ":", ":");

                        context.Segment.HISALS = SEG;

                        context.Segment.HISALSf = item;
                    }

                    if (item.Contains("HITANS"))
                    {
                        var TAN = Parse_String(item.Replace("HITANS:", ""), ":", "+").Replace(":", "+");

                        context.Segment.HITANS = TAN;
                    }

                    if (item.Contains("HKKAZ"))
                    {
                        string pattern = @"HKKAZ;.*?;";
                        Regex rgx = new Regex(pattern);
                        string sentence = item;

                        foreach (Match match in rgx.Matches(sentence))
                        {
                            var VER = Parse_String(match.Value, "HKKAZ;", ";");

                            if (String.IsNullOrEmpty(context.Segment.HKKAZ))
                                context.Segment.HKKAZ = VER;
                            else
                            {
                                if (int.Parse(VER) > int.Parse(context.Segment.HKKAZ))
                                {
                                    context.Segment.HKKAZ = VER;
                                }
                            }
                        }
                    }

                    if (item.Contains("HISPAS"))
                    {
                        if (item.Contains("pain.001.001.03"))
                            context.Segment.HISPAS = 1;
                        else if (item.Contains("pain.001.002.03"))
                            context.Segment.HISPAS = 2;
                        else if (item.Contains("pain.001.003.03"))
                            context.Segment.HISPAS = 3;

                        if (context.Segment.HISPAS == 0)
                            context.Segment.HISPAS = 3; // -> Fallback. Most banks accept the newest pain version
                    }
                }

                // Fallback if HKKAZ is not delivered by BPD (eg. Postbank)
                if (String.IsNullOrEmpty(context.Segment.HKKAZ))
                    context.Segment.HKKAZ = "5";

                return result;
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());

                throw new InvalidOperationException($"Software error.", ex);
            }
        }

        /// <summary>
        /// Parsing bank message
        /// </summary>
        /// <param name="msgStr"></param>
        /// <returns></returns>
        public static bool Parse_Message(ConnectionContext context, string msgStr)
        {
            try
            {
                String[] values = msgStr.Split('\'');

                foreach (var item in values)
                {
                    if (item.Contains("HNHBS"))
                    {
                        var item_ = item + "'";

                        var MSG = Parse_String(item_.Replace("HNHBS:", ""), "+", "'");

                        if (MSG.Equals("0") || MSG == null)
                            context.Segment.HNHBS = "2";
                        else
                            context.Segment.HNHBS = Convert.ToString(Convert.ToInt16(MSG) + 1);
                    }
                }

                if (!String.IsNullOrEmpty(context.Segment.HNHBS))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());

                return false;
            }
        }

        /// <summary>
        /// Parse balance
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public static AccountBalance Parse_Balance(string Message)
        {
            var hirms = Message.Substring(Message.IndexOf("HIRMS") + 5);
            hirms = hirms.Substring(0, (hirms.Contains("'") ? hirms.IndexOf('\'') : hirms.Length));
            var hirmsParts = hirms.Split(':');

            AccountBalance balance = new AccountBalance();
            balance.Message = hirmsParts[hirmsParts.Length - 1];

            if (Message.Contains("+0020::"))
            {
                var hisal = Message.Substring(Message.IndexOf("HISAL") + 5);
                hisal = hisal.Substring(0, (hisal.Contains("'") ? hisal.IndexOf('\'') : hisal.Length));
                var hisalParts = hisal.Split('+');

                balance.Successful = true;

                var hisalAccountParts = hisalParts[1].Split(':');
                if (hisalAccountParts.Length == 4)
                {
                    balance.AccountType = new AccountInformations()
                    {
                        Accountnumber = hisalAccountParts[0],
                        Accountbankcode = hisalAccountParts.Length > 3 ? hisalAccountParts[3] : null,
                        Accounttype = hisalParts[2],
                        Accountcurrency = hisalParts[3],
                        Accountbic = !string.IsNullOrEmpty(hisalAccountParts[1]) ? hisalAccountParts[1] : null
                    };
                }
                else if (hisalAccountParts.Length == 2)
                {
                    balance.AccountType = new AccountInformations()
                    {
                        Accountiban = hisalAccountParts[0],
                        Accountbic = hisalAccountParts[1]
                    };
                }

                var hisalBalanceParts = hisalParts[4].Split(':');
                balance.Balance = Convert.ToDecimal($"{(hisalBalanceParts[0] == "D" ? "-" : "")}{hisalBalanceParts[1]}");


                //from here on optional fields / see page 46 in "FinTS_3.0_Messages_Geschaeftsvorfaelle_2015-08-07_final_version.pdf"
                if (hisalParts.Length > 5 && hisalParts[5].Contains(":"))
                {
                    var hisalMarkedBalanceParts = hisalParts[5].Split(':');
                    balance.MarkedTransactions = Convert.ToDecimal($"{(hisalMarkedBalanceParts[0] == "D" ? "-" : "")}{hisalMarkedBalanceParts[1]}");
                }

                if (hisalParts.Length > 6 && hisalParts[6].Contains(":"))
                {
                    balance.CreditLine = Convert.ToDecimal(hisalParts[6].Split(':')[0].TrimEnd(','));
                }

                if (hisalParts.Length > 7 && hisalParts[7].Contains(":"))
                {
                    balance.AvailableBalance = Convert.ToDecimal(hisalParts[7].Split(':')[0].TrimEnd(','));
                }

                /* ---------------------------------------------------------------------------------------------------------
                 * In addition to the above fields, the following fields from HISAL could also be implemented:
                 * 
                 * - 9/Bereits verfügter Betrag
                 * - 10/Überziehung
                 * - 11/Buchungszeitpunkt
                 * - 12/Fälligkeit 
                 * 
                 * Unfortunately I'm missing test samples. So I drop support unless we get test messages for this fields.
                 ------------------------------------------------------------------------------------------------------------ */
            }
            else
            {
                balance.Successful = false;

                string msg = string.Empty;
                for (int i = 1; i < hirmsParts.Length; i++)
                {
                    msg = msg + "??" + hirmsParts[i].Replace("::", ": ");
                }
                Log.Write(msg);
            }

            return balance;
        }

        /// <summary>
        /// Parse tan processes
        /// </summary>
        /// <returns></returns>
        private static bool Parse_TANProcesses(ConnectionContext context, string bpd)
        {
            try
            {
                List<TANProcess> list = new List<TANProcess>();

                string[] processes = context.Segment.HIRMSf.Split(';');

                // Examples from bpd

                // 944:2:SECUREGO:
                // 920:2:smsTAN:
                // 920:2:BestSign:

                foreach (var process in processes)
                {
                    string pattern = process + ":.*?:.*?:(?'name'.*?):.*?:(?'name2'.*?):";

                    Regex rgx = new Regex(pattern);

                    foreach (Match match in rgx.Matches(bpd))
                    {
                        int i = 0;

                        if (!process.Equals("999")) // -> PIN/TAN step 1
                        {
                            if (int.TryParse(match.Groups["name2"].Value, out i))
                                list.Add(new TANProcess { ProcessNumber = process, ProcessName = match.Groups["name"].Value });
                            else
                                list.Add(new TANProcess { ProcessNumber = process, ProcessName = match.Groups["name2"].Value });
                        }
                    }
                }

                context.TANProcesses = list;

                return true;
            }
            catch { return false; }
        }

        public static List<string> Parse_TANMedium(string BankCode)
        {
            List<string> result = new List<string>();
            if (BankCode.Contains("+A:1"))
            {
                var tanMedium = Parse_String(BankCode + "'", "+A:1", "'").Replace(":", "");
                if (!string.IsNullOrWhiteSpace(tanMedium))
                    result.Add(tanMedium);
            }
            else
            {
                // HITAB:4:4:3+0+M:1:::::::::::mT?:MFN1:********0340'
                // HITAB:5:4:3+0+M:2:::::::::::Unregistriert 1::01514/654321::::::+M:1:::::::::::Handy:*********4321:::::::
                // HITAB:4:4:3+0+M:1:::::::::::mT?:MFN1:********0340+G:1:SO?:iPhone:00:::::::::SO?:iPhone''

                // For easier matching, replace '?:' by some special character
                BankCode = BankCode.Replace("?:", @"\");

                foreach (Match match in Regex.Matches(BankCode, @"\+[MG]:1:(?<Kartennummer>[^:]*):(?<Kartenfolgenummer>[^:]*):+(?<Bezeichnung>[^+:]+)"))
                {
                    result.Add(match.Groups["Bezeichnung"].Value.Replace(@"\", "?:"));
                }
            }

            return result;
        }

        static FlickerRenderer flickerCodeRenderer = null;

        /// <summary>
        /// Fill given <code>TANDialog</code> and wait for user to enter a TAN.
        /// </summary>
        /// <param name="BankCode"></param>
        /// <param name="pictureBox"></param>
        /// <param name="flickerImage"></param>
        /// <param name="flickerWidth"></param>
        /// <param name="flickerHeight"></param>
        /// <param name="renderFlickerCodeAsGif"></param>
        public static string WaitForTAN(ConnectionContext context, HBCIDialogResult dialogResult, TANDialog tanDialog)
        {
            var BankCode_ = "HIRMS" + Helper.Parse_String(dialogResult.RawData, "'HIRMS", "'");
            String[] values = BankCode_.Split('+');
            foreach (var item in values)
            {
                if (!item.StartsWith("HIRMS"))
                    TransactionConsole.Output = item.Replace("::", ": ");
            }

            var HITAN = "HITAN" + Helper.Parse_String(dialogResult.RawData.Replace("?'", "").Replace("?:", ":").Replace("<br>", Environment.NewLine).Replace("?+", "??"), "'HITAN", "'");

            string HITANFlicker = string.Empty;

            var processes = context.TANProcesses;

            var processname = string.Empty;

            foreach (var item in processes)
            {
                if (item.ProcessNumber.Equals(context.Segment.HIRMS))
                    processname = item.ProcessName;
            }

            // Smart-TAN plus optisch
            // chipTAN optisch
            if (processname.Equals("Smart-TAN plus optisch") || processname.Contains("chipTAN optisch"))
            {
                HITANFlicker = HITAN;
            }

            String[] values_ = HITAN.Split('+');

            int i = 1;

            foreach (var item in values_)
            {
                i = i + 1;

                if (i == 6)
                {
                    TransactionConsole.Output = TransactionConsole.Output + "??" + item.Replace("::", ": ").TrimStart();

                    TransactionConsole.Output = TransactionConsole.Output.Replace("??", " ")
                            .Replace("0030: ", "")
                            .Replace("1.", Environment.NewLine + "1.")
                            .Replace("2.", Environment.NewLine + "2.")
                            .Replace("3.", Environment.NewLine + "3.")
                            .Replace("4.", Environment.NewLine + "4.")
                            .Replace("5.", Environment.NewLine + "5.")
                            .Replace("6.", Environment.NewLine + "6.")
                            .Replace("7.", Environment.NewLine + "7.")
                            .Replace("8.", Environment.NewLine + "8.");
                }
            }

            // chipTAN optisch
            if (processname.Contains("chipTAN optisch"))
            {
                string FlickerCode = string.Empty;

                FlickerCode = "CHLGUC" + Helper.Parse_String(HITAN, "CHLGUC", "CHLGTEXT") + "CHLGTEXT";

                FlickerCode flickerCode = new FlickerCode(FlickerCode);
                flickerCodeRenderer = new FlickerRenderer(flickerCode.Render(), tanDialog.PictureBox);
                if (!tanDialog.RenderFlickerCodeAsGif)
                {
                    RUN_flickerCodeRenderer();

                    Action action = STOP_flickerCodeRenderer;
                    TimeSpan span = new TimeSpan(0, 0, 0, 50);

                    ThreadStart start = delegate { RunAfterTimespan(action, span); };
                    Thread thread = new Thread(start);
                    thread.Start();
                }
                else
                {
                    tanDialog.FlickerImage = flickerCodeRenderer.RenderAsGif(tanDialog.FlickerWidth, tanDialog.FlickerHeight);
                }
            }

            // Smart-TAN plus optisch
            if (processname.Equals("Smart-TAN plus optisch"))
            {
                HITANFlicker = HITAN.Replace("?@", "??");

                string FlickerCode = string.Empty;

                String[] values__ = HITANFlicker.Split('@');

                int ii = 1;

                foreach (var item in values__)
                {
                    ii = ii + 1;

                    if (ii == 4)
                        FlickerCode = item;
                }

                FlickerCode flickerCode = new FlickerCode(FlickerCode.Trim());
                flickerCodeRenderer = new FlickerRenderer(flickerCode.Render(), tanDialog.PictureBox);
                if (!tanDialog.RenderFlickerCodeAsGif)
                {
                    RUN_flickerCodeRenderer();

                    Action action = STOP_flickerCodeRenderer;
                    TimeSpan span = new TimeSpan(0, 0, 0, 50);

                    ThreadStart start = delegate { RunAfterTimespan(action, span); };
                    Thread thread = new Thread(start);
                    thread.Start();
                }
                else
                {
                    tanDialog.FlickerImage = flickerCodeRenderer.RenderAsGif(tanDialog.FlickerWidth, tanDialog.FlickerHeight);
                }
            }

            // Smart-TAN photo
            if (processname.Equals("Smart-TAN photo"))
            {
                var PhotoCode = Parse_String(dialogResult.RawData, ".+@", "'HNSHA");

                var mCode = new MatrixCode(PhotoCode.Substring(5, PhotoCode.Length - 5));

                tanDialog.MatrixImage = mCode.CodeImage;
                mCode.Render(tanDialog.PictureBox);
            }

            // PhotoTAN
            if (processname.Equals("photoTAN-Verfahren"))
            {
                // HITAN:5:5:4+4++nmf3VmGQDT4qZ20190130091914641+Bitte geben Sie die photoTan ein+@3031@       image/pngÃŠÂ‰PNG
                var match = Regex.Match(dialogResult.RawData, @"HITAN.+@\d+@(.+)'HNHBS", RegexOptions.Singleline);
                if (match.Success)
                {
                    var PhotoBinary = match.Groups[1].Value;

                    var mCode = new MatrixCode(PhotoBinary);

                    tanDialog.MatrixImage = mCode.CodeImage;
                    mCode.Render(tanDialog.PictureBox);
                }
            }

            return tanDialog.WaitForTAN();
        }

        /// <summary>
        /// Parse a single bank result message.
        /// </summary>
        /// <param name="BankCodeMessage"></param>
        /// <returns></returns>
        public static HBCIBankMessage Parse_BankCode_Message(string BankCodeMessage)
        {
            var match = Regex.Match(BankCodeMessage, PatternResultMessage);
            if (match.Success)
            {
                var code = match.Groups[1].Value;
                var message = match.Groups[2].Value;

                message = message.Replace("?:", ":");

                return new HBCIBankMessage(code, message);
            }
            return null;
        }

        /// <summary>
        /// Parse bank error codes
        /// </summary>
        /// <param name="BankCode"></param>
        /// <returns>Banks messages with "??" as seperator.</returns>
        public static List<HBCIBankMessage> Parse_BankCode(string BankCode)
        {
            List<HBCIBankMessage> result = new List<HBCIBankMessage>();

            string[] segments = BankCode.Split('\'');
            foreach (var segment in segments)
            {
                if (segment.Contains("HIRMG") || segment.Contains("HIRMS"))
                {
                    string[] messages = segment.Split('+');
                    foreach (var HIRMG_message in messages)
                    {
                        var message = Parse_BankCode_Message(HIRMG_message);
                        if (message != null)
                            result.Add(message);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// RUN Flicker Code Rendering
        /// </summary>
        private static void RUN_flickerCodeRenderer()
        {
            flickerCodeRenderer.Start();
        }

        /// <summary>
        /// STOP Flicker Code Rendering
        /// </summary>
        public static void RunAfterTimespan(Action action, TimeSpan span)
        {
            Thread.Sleep(span);
            action();
        }

        private static void STOP_flickerCodeRenderer()
        {
            flickerCodeRenderer.Stop();
        }

        /// <summary>
        /// Make filename valid
        /// </summary>
        public static string MakeFilenameValid(string value)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value.Replace(" ", "_");
        }

        public static string GetProgramBaseDir()
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Program.Buildname == null)
            {
                throw new InvalidOperationException("Der Wert von Program.Buildname muss gesetzt sein.");
            }

            var buildname = Program.Buildname.StartsWith(".") ? Program.Buildname : $".{Program.Buildname}";

            return Path.Combine(userHome, buildname);
        }

        private static string GetBPDDir()
        {
            var dir = GetProgramBaseDir();
            return Path.Combine(dir, "BPD");
        }

        private static string GetBPDFile(string dir, int BLZ)
        {
            return Path.Combine(dir, "280_" + BLZ + ".bpd");
        }

        private static string GetUPDDir()
        {
            var dir = GetProgramBaseDir();
            return Path.Combine(dir, "UPD");
        }

        private static string GetUPDFile(string dir, int BLZ, string UserID)
        {
            return Path.Combine(dir, "280_" + BLZ + "_" + UserID + ".upd");
        }

        public static void SaveUPD(int BLZ, string UserID, string upd)
        {
            string dir = GetUPDDir();
            Directory.CreateDirectory(dir);
            var file = GetUPDFile(dir, BLZ, UserID);
            if (!File.Exists(file))
            {
                using (File.Create(file)) { };
            }
            File.WriteAllText(file, upd);
        }

        public static string GetUPD(int BLZ, string UserID)
        {
            var dir = GetUPDDir();
            var file = GetUPDFile(dir, BLZ, UserID);
            var content = File.Exists(file) ? File.ReadAllText(file) : string.Empty;

            return content;
        }

        public static void SaveBPD(int BLZ, string upd)
        {
            string dir = GetBPDDir();
            Directory.CreateDirectory(dir);
            var file = GetBPDFile(dir, BLZ);
            if (!File.Exists(file))
            {
                using (File.Create(file)) { };
            }
            File.WriteAllText(file, upd);
        }

        public static string GetBPD(int BLZ)
        {
            var dir = GetBPDDir();
            var file = GetBPDFile(dir, BLZ);
            var content = File.Exists(file) ? File.ReadAllText(file) : string.Empty;

            return content;
        }

        public static bool IsTANRequired(ConnectionContext context, string gvName)
        {
            var HIPINS = context.BPD.HIPINS;
            return HIPINS != null && HIPINS.IsTANRequired(gvName);
        }
    }
}


