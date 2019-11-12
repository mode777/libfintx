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

using libfintx.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace libfintx
{
    public class BPD
    {
        public string Value { get; set; }

        public HIPINS HIPINS { get; set; }

        public List<HITANS> HITANS { get; set; }

        public HICAZS HICAZS { get; set; }

        public static BPD Parse_BPD(string str)
        {
            var result = new BPD();

            result.Value = str;

            var lines = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var hipins = lines.FirstOrDefault(l => l.StartsWith("HIPINS"));
            result.HIPINS = HIPINS.Parse_HIPINS(hipins ?? string.Empty);

            result.HITANS = new List<HITANS>();
            var list = lines.Where(l => l.StartsWith("HITANS"));
            foreach (var hitans in list)
            {
                var item = libfintx.HITANS.Parse_HITANS(hitans);
                result.HITANS.Add(item);
            }

            var hicazs = lines.FirstOrDefault(l => l.StartsWith("HICAZS"));
            result.HICAZS = HICAZS.Parse_HICAZS(hicazs);

            return result;
        }
    }
}
