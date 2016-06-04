//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;
using EHang.CopterManagement;
using EHang.Copters;
using Windows.Networking.Proximity;
using Windows.Devices.Bluetooth.Rfcomm;
using EHang.Communication;
using System.Threading;
using Windows.UI.Popups;
using Windows.Data.Xml.Dom;
namespace CopterHelper
{
    /// <summary>
    /// Provides access to stored location data. 
    /// </summary>
    public static class CopterDataStore
    {
        private const string dataFileName = "EHangAppData.txt";

        /// <summary>
        /// Gets a list of four sample locations randomply positioned around the user's current 
        /// location or around the Microsoft main campus if the Geolocator is unavailable. 
        /// </summary>
        /// <returns>The sample locations.</returns>
        public static async Task<List<CopterData>> GetSampleCopterDataAsync()
        {
            var locations =new List<CopterData>();
            try
            {


                var center = (await CopterHelper.GetCurrentLocationAsync())?.Position ??
                    new BasicGeoposition { Latitude = 47.640068, Longitude = -122.129858 };

                int latitudeRange = 36000;
                int longitudeRange = 53000;
                var random = new Random();
                Func<int, double, double> getCoordinate = (range, midpoint) =>
                    (random.Next(range) - (range / 2)) * 0.00000005 + midpoint;

                XmlDocument doc = await CopterHelper.LoadXmlFile("config", "copters.xml");

                 var nodelist = doc.DocumentElement.SelectNodes("/copters/copter");
               string[] copterNames = new string[nodelist.Count];
                int i = 0;
                foreach(var node in nodelist)
                {
                    copterNames[i] = node.Attributes.GetNamedItem("name").NodeValue.ToString();
                    i++;
                }
                locations =new List<CopterData>();
                foreach (var node in nodelist)
                {
                    CopterData dt = new CopterData();
                    dt.Name = node.Attributes.GetNamedItem("name").NodeValue.ToString();
                    dt.Type = node.Attributes.GetNamedItem("type").NodeValue.ToString();
                    dt.Hostname = node.Attributes.GetNamedItem("hostname").NodeValue.ToString();
                   
                    if (dt.Type == "fake")
                    {
                        dt.Copter = CreateFakeCopter(dt.Name, dt.Position.Latitude, dt.Position.Longitude);
                        dt.Position = new BasicGeoposition
                        {
                            Latitude = getCoordinate(latitudeRange, center.Latitude),
                            Longitude = getCoordinate(longitudeRange, center.Longitude)
                        };
                    }
                    else if (dt.Type == "bluetooth")
                    {
                        dt.Copter = CreateBluetoothCopter(dt.Hostname, dt.Name);
                        dt.Position = center;
                    }
                    locations.Add(dt);
                }


          

            }
            catch (Exception e)
            {
                throw;
            }
            return locations;
        }

        #region copter
        private static ICopter CreateFakeCopter(string coptername, double copterlatitude, double copterlongitude)
        {
            var copter = new FakeCopter();
            copter.SetProperties(id: coptername,name:coptername, latitude: copterlatitude, longitude: copterlongitude);
            return copter;
        }

     

        public static  async Task<ICopter> Foo()
        {
            try
            {
                // 获取已配对的 VR 眼镜/G-BOX 列表。注意：由于 UWP 的限制，需要先在系统设置中配对，密码是 1234。
                string deviceid = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
                var services = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(deviceid);
                var service = services[0];
                var peers = await FindBluetoothPeersAsync();
                var peer = peers.FirstOrDefault();
                var rfcommService = await RfcommDeviceService.FromIdAsync(service.Id);

                //if (rfcommService != null)
                {

                    // 创建使用蓝牙连接的飞行器对象。
                    var x = rfcommService.ConnectionHostName.ToString();
                    var copter1 = CreateBluetoothCopter(x, "EHANG GHOST");
                    return copter1;
                }
            }
            catch (Exception ex)
            {
                string retMsg = "连接飞行器失败，返回信息为：" + ex.ToString();
                MessageDialog diag = new MessageDialog(retMsg);
                await diag.ShowAsync();
                return null;
            }
        }

        /// <summary>
        /// 获取已配对的 VR 眼镜/G-BOX 列表。注意：由于 UWP 的限制，需要先在系统设置中配对，密码是 1234。
        /// </summary>
        /// <returns>已配对的 VR 眼镜/G-BOX 列表。</returns>
        private static async Task<IEnumerable<PeerInformation>> FindBluetoothPeersAsync()
        {


            try
            {
                PeerFinder.Start();
                PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = string.Empty;
                var peers = (await PeerFinder.FindAllPeersAsync());//.Where(p => p.DisplayName.StartsWith("EHANG"));
                return peers;
            }
            catch (Exception ex)
            {
                throw;  // 无蓝牙设备或者未开启蓝牙。
            }


        }

        private static ICopter CreateBluetoothCopter(string hostName, string copterName)
        {
            var connection = new BluetoothConnection(hostName);
            var copter = new EHCopter(connection, SynchronizationContext.Current)
            {
                Id = "Bluetooth_" + hostName,
                Name = copterName
            };

            return copter;
        }
        #endregion

        /// <summary>
        /// Load the saved location data from roaming storage. 
        /// </summary>
        public static async Task<List<CopterData>> GetCopterDataAsync()
        {
            List<CopterData> data = null;
            try
            {
                StorageFile dataFile = await ApplicationData.Current.RoamingFolder.GetFileAsync(dataFileName);
                string text = await FileIO.ReadTextAsync(dataFile);
                byte[] bytes = Encoding.Unicode.GetBytes(text);
                var serializer = new DataContractJsonSerializer(typeof(List<CopterData>));
                using (var stream = new MemoryStream(bytes))
                {
                    data = serializer.ReadObject(stream) as List<CopterData>;
                }
            }
            catch (FileNotFoundException)
            {
                // Do nothing.
            }
            return data ?? new List<CopterData>();
        }

        /// <summary>
        /// Save the location data to roaming storage. 
        /// </summary>
        /// <param name="locations">The locations to save.</param>
        public static async Task SaveCopterDataAsync(IEnumerable<CopterData> locations)
        {
            StorageFile sampleFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync(
                dataFileName, CreationCollisionOption.ReplaceExisting);
            using (MemoryStream stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(List<CopterData>));
                serializer.WriteObject(stream, locations.ToList());
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string locationString = reader.ReadToEnd();
                    await FileIO.WriteTextAsync(sampleFile, locationString);
                }
            }
        }

    }
}
