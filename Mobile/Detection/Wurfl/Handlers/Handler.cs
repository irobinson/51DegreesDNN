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

using System;
using System.Collections.Generic;
using System.Web;
using FiftyOne.Foundation.Mobile.Detection.Wurfl.Matchers;

#endregion

namespace FiftyOne.Foundation.Mobile.Detection.Wurfl.Handlers
{
    internal abstract class Handler
    {
        #region Constants

        /// <summary>
        /// The default confidence to assign to results from the handler.
        /// </summary>
        private const byte DEFAULT_CONFIDENCE = 5;

        /// <summary>
        /// WURFL uaprof capabilities to check.
        /// </summary>
        private static readonly int[] UAPROF_CAPABILITIES = new[]
                                                                   {
                                                                       Strings.Add("uaprof"),
                                                                       Strings.Add("uaprof2"),
                                                                       Strings.Add("uaprof3")
                                                                   };

        /// <summary>
        /// HTTP headers containing uaprof urls.
        /// </summary>
        private static readonly string[] UAPROF_HEADERS = new[]
                                                              {
                                                                  "profile",
                                                                  "x-wap-profile",
                                                                  "X-Wap-Profile"
                                                              };

        #endregion

        #region Fields

        /// <summary>
        /// A collection of domain names used with uaprof urls.
        /// </summary>
        private readonly List<string> _uaProfDomains = new List<string>();

        /// <summary>
        /// A single collection of all uaprof urls used by devices assigned to this handler.
        /// </summary>
        private readonly SortedDictionary<int, DeviceInfo[]> _uaprofs =
            new SortedDictionary<int, DeviceInfo[]>();

        /// <summary>
        /// A single collection of all useragent strings and devices assigned to this handler.
        /// </summary>
        private readonly SortedDictionary<int, DeviceInfo[]> _useragents =
            new SortedDictionary<int, DeviceInfo[]>();

        #endregion

        #region Properties

        /// <summary>
        /// An array of device ids that must be in the device hierarchy
        /// to enable the handler to support the device. Overriden in 
        /// derived classess to provide an array of root device ids.
        /// </summary>
        protected virtual string[] SupportedRootDeviceIds
        {
            get { return null; }
        }

        /// <summary>
        /// Returns the device values without the hashcode used to rapidly
        /// search for devices based on the userAgent string.
        /// </summary>
        internal SortedDictionary<int, DeviceInfo[]>.ValueCollection UserAgents
        {
            get { return _useragents.Values; }
        }

        /// <summary>
        /// The confidence to assign to results from this handler.
        /// </summary>
        internal virtual byte Confidence
        {
            get { return DEFAULT_CONFIDENCE; }
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// The inheriting classes match method.
        /// </summary>
        /// <param name="userAgent">The useragent to match.</param>
        /// <returns>A result set of matching devices.</returns>
        protected internal abstract Results Match(string userAgent);

        /// <summary>
        /// Returns true or false depending on the handlers ability
        /// to match the user agent provided.
        /// </summary>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        protected internal abstract bool CanHandle(string userAgent);

        #endregion

        #region Internal Methods

        /// <summary>
        /// The default device for the handler. If not overriden the default
        /// device for the API will be returned.
        /// </summary>
        internal virtual DeviceInfo DefaultDevice
        {
            get { return Provider.DefaultDevice; }
        }

        /// <summary>
        /// Adds a new device to the handler.
        /// </summary>
        /// <param name="device">device being added to the handler.</param>
        internal virtual void Set(DeviceInfo device)
        {
            SetUserAgent(device);
            SetUaProf(device);
        }

        /// <summary>
        /// Returns the device matching the userAgent string if one is available.
        /// </summary>
        /// <param name="userAgent">userAgent being sought.</param>
        /// <returns>null if no device is found. Otherwise the matching device.</returns>
        internal DeviceInfo GetDeviceInfo(string userAgent)
        {
            DeviceInfo[] devices = GetDeviceInfo(_useragents, userAgent);
            if (devices != null && devices.Length > 0)
            {
                // If only one device available return this one.
                if (devices.Length == 1)
                {
                    return devices[0];
                }
                else
                {
                    // Look at each device for an exact match.
                    foreach (DeviceInfo device in devices)
                    {
                        if (device.UserAgent == userAgent)
                        {
                            return device;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all the devices that match the UA prof provided.
        /// </summary>
        /// <param name="uaprof">UA prof to search for.</param>
        /// <returns>Results containing all the matching devices.</returns>
        internal Results GetResultsFromUAProf(string uaprof)
        {
            DeviceInfo[] devices = GetDeviceInfo(_uaprofs, uaprof);
            if (devices != null && devices.Length > 0)
            {
                // Add the devices to the list of results and return.
                Results results = new Results();
                results.AddRange(devices);
                return results;
            }
            return null;
        }

        /// <summary>
        /// Returns true if the device roots have been specified and this device is
        /// within the branches of the available root devices. 
        /// AND
        /// The useragent string of the device is supported by the handler.
        /// </summary>
        /// <param name="device">Device to be checked.</param>
        /// <returns>True if supported root devices are provided and the device is found
        /// within it and the device's useragent string can be handled. Or true if if
        /// the useragent string can be handled but no supported root devices are provided.
        /// Otherwise false.</returns>
        protected internal virtual bool CanHandle(DeviceInfo device)
        {
            if (SupportedRootDeviceIds == null)
                return CanHandle(device.UserAgent);
            else
                return CanHandleDevice(device) && CanHandle(device.UserAgent);
        }

        /// <summary>
        /// Checks to see if the device specified is on of the supported root
        /// devices. If it's not then the fallback device is checked.
        /// </summary>
        /// <param name="device">Device to be checked.</param>
        /// <returns>True if the device is matched against a root device.
        /// False if the device is null or is not matched.</returns>
        private bool CanHandleDevice(DeviceInfo device)
        {
            if (device != null)
            {
                foreach (string deviceId in SupportedRootDeviceIds)
                {
                    if (deviceId == device.DeviceId)
                    {
                        return true;
                    }
                }
                return CanHandleDevice(device.FallbackDevice);
            }
            return false;
        }

        /// <summary>
        /// <para>
        /// First checks if the useragent from the request can be handled by 
        /// this handler.
        /// </para>
        /// <para>
        /// If the useragent can't be handled then the request is checked to 
        /// determine if a uaprof header field is provided. If so we check
        /// the list of uaprof domains assigned to this handler to see if
        /// they share the same domain.
        /// </para>
        /// </summary>
        /// <param name="request">Request with headers to be processed.</param>
        /// <returns>True if this handler could be able to match the device otherwise false.</returns>
        internal virtual bool CanHandle(HttpRequest request)
        {
            bool canHandle = CanHandle(Provider.GetUserAgent(request));
            if (canHandle == false && _uaProfDomains.Count > 0)
            {
                Uri url = null;
                foreach (string header in UAPROF_HEADERS)
                {
                    string value = request.Headers[header];
                    if (value != null &&
                        Uri.TryCreate(value, UriKind.Absolute, out url) &&
                        _uaProfDomains.Contains(url.Host))
                    {
                        return true;
                    }
                }
            }
            return canHandle;
        }

        /// <summary>
        /// Performs an exact match using the userAgent string. If no results are found
        /// uses the UA prof header parameters to find a list of devices.
        /// </summary>
        /// <param name="request">details of the page request.</param>
        /// <returns>null if no exact match was found. Otherwise the matching devices.</returns>
        internal virtual Results Match(HttpRequest request)
        {
            // Check for an exact match of the user agent string.
            string userAgent = Provider.GetUserAgent(request);
            DeviceInfo device = GetDeviceInfo(userAgent);
            if (device != null)
                return new Results(device);

            // Check to see if we have a uaprof header parameter that will produce
            // an exact match.
            if (request.Headers != null && request.Headers.Count > 0)
            {
                foreach (string header in UAPROF_HEADERS)
                {
                    string value = request.Headers[header];
                    if (String.IsNullOrEmpty(value) == false)
                    {
                        value = CleanUaProf(value);
                        Results results = GetResultsFromUAProf(value);
                        if (results != null && results.Count > 0)
                        {
                            if (EventLog.IsDebug)
                                EventLog.Debug(String.Format("UAProf matched '{0}' devices to header '{1}'.",
                                                             results.Count, value));
                            return results;
                        }
                    }
                }
            }

            // There isn't a UA Prof match so use the handler specific methods.
            return Match(userAgent);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Removes any speech marks from the user agent string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string CleanUaProf(string value)
        {
            return value.Replace("\"", "");
        }

        /// <summary>
        /// Adds the device and it's user agent string to the collection
        /// of user agent strings and devices.
        /// </summary>
        /// <param name="device">New device to add.</param>
        private void SetUserAgent(DeviceInfo device)
        {
            int hashcode = device.UserAgent.GetHashCode();
            lock (_useragents)
            {
                // Does the hashcode already exist?
                if (_useragents.ContainsKey(hashcode))
                {
                    // Does the key already exist?
                    for (int i = 0; i < _useragents[hashcode].Length; i++)
                    {
                        if (_useragents[hashcode][i].UserAgent == device.UserAgent)
                        {
                            // Yes. Update with the new device and then exit.
                            _useragents[hashcode][i] = device;
                            return;
                        }
                    }
                    // No. Expand the array adding the new device.
                    List<DeviceInfo> newList = new List<DeviceInfo>(_useragents[hashcode]);
                    newList.Add(device);
                    _useragents[hashcode] = newList.ToArray();
                }
                else
                {
                    // Add the device to the collection.
                    _useragents.Add(hashcode, new[] {device});
                }
            }
        }

        /// <summary>
        /// Adds the device to the collection of devices with UA prof information.
        /// If the device already exists the previous one is replaced.
        /// </summary>
        /// <param name="device">Device to be added.</param>
        private void SetUaProf(DeviceInfo device)
        {
            foreach (int uaprof in UAPROF_CAPABILITIES)
            {
                string value = Strings.Get(device.GetCapability(uaprof));
                
                // Don't process empty values.
                if (String.IsNullOrEmpty(value)) continue;
                
                // Clean the useragent prof.
                value = CleanUaProf(value);
                Uri url = null;
                
                // If the url is not value don't continue processing.
                if (!Uri.TryCreate(value, UriKind.Absolute, out url)) continue;
                
                // Get the hashcode before locking the list and processing
                // the device and hashcode.
                int hashcode = value.GetHashCode();
                lock (_uaprofs)
                {
                    ProcessUaProf(device, hashcode);
                }

                // Add the domain to the list of domains for the handler.
                lock (_uaProfDomains)
                {
                    if (_uaProfDomains.Contains(url.Host) == false)
                        _uaProfDomains.Add(url.Host);
                }
            }
        }

        private void ProcessUaProf(DeviceInfo device, int hashcode)
        {
            // Does the hashcode already exist?
            if (_uaprofs.ContainsKey(hashcode))
            {
                // Does the key already exist?
                int index;
                for (index = 0; index < _uaprofs[hashcode].Length; index++)
                {
                    if (_uaprofs[hashcode][index].DeviceId != device.DeviceId) continue;
                    // Yes. Update with the new device and then exit.
                    _uaprofs[hashcode][index] = device;
                    return;
                }
                // No. Expand the array adding the new device.
                List<DeviceInfo> newList = new List<DeviceInfo>(_uaprofs[hashcode]) { device };
                _uaprofs[hashcode] = newList.ToArray();
            }
            else
            {
                // Add the device to the collection.
                _uaprofs.Add(hashcode, new[] {device});
            }
        }

        /// <summary>
        /// Returns the devices that match a specific hashcode.
        /// </summary>
        /// <param name="_dictionary">Collection of hashcodes and devices.</param>
        /// <param name="value">Value that's hashcode is being sought.</param>
        /// <returns>Array of devices matching the value.</returns>
        private static DeviceInfo[] GetDeviceInfo(SortedDictionary<int, DeviceInfo[]> _dictionary, string value)
        {
            int hashcode = value.GetHashCode();
            return _dictionary.ContainsKey(hashcode) ? _dictionary[hashcode] : null;
        }

        #endregion
    }
}