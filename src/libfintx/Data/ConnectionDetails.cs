﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace libfintx.Data
{
    public class ConnectionContext
    {
        public CompositeTransaction Transaction { get; set; }
        public UPD UPD { get; set; } = new UPD();
        public BPD BPD { get; set; } = new BPD();
        public List<TANProcess> TANProcesses { get; set; }
        public string SegmentId { get; set; }
        public Segment Segment { get; set; } = new Segment();
        public int SegmentNumber { get; set; }
        public HttpClient Client { get; set; }
        /// <summary>
        /// Url of the HBCI/FinTS endpoint. Can be retrieved from: https://www.hbci-zka.de/institute/institut_auswahl.htm
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// HBCI version number. E.g. '300' for FinTS 3.0
        /// </summary>
        public int HBCIVersion { get; set; }

        /// <summary>
        /// Logon ID/username for the bank account
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Logon-Pin for the bank account
        /// </summary>
        public string Pin { get; set; }


        /// <summary>
        /// Name of the Accountholder
        /// </summary>
        public string AccountHolder { get; set; }

        /// <summary>
        /// Accountnumber of the bank account
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Bankcode of the bank account
        /// </summary>
        public int Blz { get; set; }

        /// <summary>
        /// Bankcode of the bank's headquarter (e.g. to be used in Hypovereinsbank)
        /// </summary>
        public int? BlzHeadquarter { get; set; }

        /// <summary>
        /// BLZ needed for message header (HNVSK, HNSHK) - either BlzHeaderquarter or Blz
        /// </summary>
        public int BlzPrimary => BlzHeadquarter ?? Blz;

        /// <summary>
        /// IBAN of the bank account
        /// </summary>
        public string IBAN { get; set; }

        /// <summary>
        /// BIC of the bank account
        /// </summary>
        public string BIC { get; set; }

        /// <summary>
        /// System ID (Kundensystem-ID)
        /// </summary>
        public string CustomerSystemId { get; set; }
    }
}
