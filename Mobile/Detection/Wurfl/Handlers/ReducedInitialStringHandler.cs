﻿/* *********************************************************************
 * The contents of this file are subject to the Mozilla Public License 
 * Version 1.1 (the "License"); you may not use this file except in 
 * compliance with the License. You may obtain a copy of the License at 
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS" 
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
 * See the License for the specific language governing rights and 
 * limitations under the License.
 *
 * The Original Code is named .NET Mobile API, first released under 
 * this licence on 11th March 2009.
 * 
 * The Initial Developer of the Original Code is owned by 
 * 51 Degrees Mobile Experts Limited. Portions created by 51 Degrees 
 * Mobile Experts Limited are Copyright (C) 2009 - 2011. All Rights Reserved.
 * 
 * Contributor(s):
 *     James Rosewell <james@51degrees.mobi>
 * 
 * ********************************************************************* */

#region Usings

using FiftyOne.Foundation.Mobile.Detection.Wurfl.Matchers;
using Matcher=FiftyOne.Foundation.Mobile.Detection.Wurfl.Matchers.ReducedInitialString.Matcher;

#endregion

namespace FiftyOne.Foundation.Mobile.Detection.Wurfl.Handlers
{
    internal abstract class ReducedInitialStringHandler : Handler
    {
        protected virtual int CalculateTolerance(string userAgent)
        {
            return FirstSlash(userAgent);
        }

        protected internal override Results Match(string userAgent)
        {
            int tolerance = CalculateTolerance(userAgent);
            // Only perform this check if a tolerance is returned.
            if (tolerance > 0)
                return Matcher.Match(userAgent, this, tolerance);
            return null;
        }

        internal static int FirstSpace(string userAgent)
        {
            int pos = userAgent.IndexOf(" ");
            return pos > -1 ? pos : userAgent.Length;
        }

        internal static int FirstSlash(string userAgent)
        {
            int pos = userAgent.IndexOf("/");
            return pos > -1 ? pos : userAgent.Length;
        }

        internal static int SecondSlash(string userAgent)
        {
            int pos = userAgent.IndexOf("/");
            if (pos > -1)
                pos = userAgent.IndexOf("/", pos + 1);
            return pos > -1 ? pos : userAgent.Length;
        }
    }
}