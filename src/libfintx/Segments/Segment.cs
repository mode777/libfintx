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

using System.Reflection;
using System.Linq;
using System;
using System.Text;
using libfintx.Util;

namespace libfintx
{
    public class Segment
    {
        /// <summary>
        /// TAN
        /// </summary>
        public string HIRMS { get; set; }
        public string HIRMSf { get; set; }

        /// <summary>
        /// TAN
        /// </summary>
        public string HITANS { get; set; }

        /// <summary>
        /// TAN
        /// </summary>
        public string HITAN { get; set; }

        /// <summary>
        /// DialogID
        /// </summary>
        public string HNHBK { get; set; }

        /// <summary>
        /// SystemID
        /// </summary>
        public string HISYN { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string HNHBS { get; set; }

        /// <summary>
        /// Segment
        /// </summary>
        public string HISALS { get; set; }
        public string HISALSf { get; set; }

        /// <summary>
        /// Transactions
        /// </summary>
        public string HKKAZ { get; set; }

        /// <summary>
        /// Transactions camt053
        /// </summary>
        public string HKCAZ { get { return "1"; } }

        /// <summary>
        /// TAN Medium Name
        /// </summary>
        public string HITAB { get; set; }

        /// <summary>
        /// PAIN version
        /// </summary>
        public int HISPAS { get; set; }

        public string AsString()
        {
            StringBuilder sb = new StringBuilder();

            var propList = typeof(Segment)
                .GetProperties(BindingFlags.Public)
                .Where(f => f.PropertyType == typeof(string));

            foreach (var prop in propList)
            {
                sb.AppendLine($"{prop.Name}: {prop.GetValue(null, null)}");
            }

            return sb.ToString();
        }
    }
}
