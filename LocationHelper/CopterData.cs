﻿//  ---------------------------------------------------------------------------------
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
using System.Runtime.Serialization;
using Windows.Devices.Geolocation;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Services.Maps;
using EHang.Copters;
using EHang.Communication;
using EHang.CopterManagement;
using CopterHelper;
using EHang.Messaging;
using Windows.UI.Xaml.Controls.Maps;

namespace CopterHelper
{
    /// <summary>
    /// 一个飞机的. 
    /// </summary>
    public class CopterData : BindableBase
    {
        private string name;
        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.SetProperty(ref this.name, value); }
        }
        private string type;
        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        private string hostname;

        public string Hostname
        {
            get
            {
                return hostname;
            }

            set
            {
                hostname = value;
            }
        }
        private BasicGeoposition position;
        /// <summary>
        /// Gets the geographic position of the location.
        /// </summary>
        public BasicGeoposition Position
        {
            get { return this.position; }
            set
            {
                this.SetProperty(ref this.position, value);
                this.OnPropertyChanged(nameof(Geopoint));
            }
        }

        private MapIcon destmapIcon;
        public MapIcon DestmapIcon
        {
            get
            {
                return destmapIcon;
            }

            set
            {
                destmapIcon = value;
            }
        }

        private MapPolyline destmapLine;
        public MapPolyline DestmapLine
        {
            get
            {
                return destmapLine;
            }

            set
            {
                destmapLine = value;
            }
        }
        /// <summary>
        /// Gets a Geopoint representation of the current location for use with the map service APIs.
        /// </summary>
        public Geopoint Geopoint => new Geopoint(this.Position);

        private bool isCurrentLocation;
        /// <summary>
        /// Gets or sets a value that indicates whether the location represents the user's current location.
        /// </summary>
        public bool IsCurrentLocation
        {
            get { return this.isCurrentLocation; }
            set
            {
                this.SetProperty(ref this.isCurrentLocation, value);
                this.OnPropertyChanged(nameof(NormalizedAnchorPoint));
            }
        }

        private bool isSelected;
        /// <summary>
        /// Gets or sets a value that indicates whether the location is 
        /// the currently selected one in the list of saved locations.
        /// </summary>
        [IgnoreDataMember]
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.SetProperty(ref this.isSelected, value);
                this.OnPropertyChanged(nameof(ImageSource));
            }
        }

        /// <summary>
        /// Gets a path to an image to use as a map pin, reflecting the IsSelected property value. 
        /// </summary>
        public string ImageSource => IsSelected ? "Assets/7.png" : "Assets/toy02.png"; 

        private Point centerpoint = new Point(0.5, 0.5);
        private Point pinpoint = new Point(0.5, 0.9778);
        /// <summary>
        /// Gets a value for the MapControl.NormalizedAnchorPoint attached property
        /// to reflect the different map icon used for the user's current location. 
        /// </summary>
        public Point NormalizedAnchorPoint => IsCurrentLocation ? centerpoint : pinpoint;

        
     
      


        private bool isMonitored;
        /// <summary>
        /// Gets or sets a value that indicates whether this location is 
        /// being monitored for an increase in travel time due to traffic. 
        /// </summary>
        public bool IsMonitored
        {
            get { return this.isMonitored; }
            set { this.SetProperty(ref this.isMonitored, value); }
        }

        public ICopter Copter
        {
            get
            {
                return copter;
            }

            set
            {
                copter = value;
            }
        }

        public List<IMission> Missions
        {
            get
            {
                return missions;
            }

            set
            {
                missions = value;
            }
        }


        private ICopter copter;

        private List<IMission> missions=new List<IMission>();


       // private ICopterManager copterManager;

      //  private IEHMessenger messager;

        /// <summary>
        /// Returns the name of the location, or the geocoordinates if there is no name. 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => String.IsNullOrEmpty(this.Name) ? 
            $"{this.Position.Latitude}, {this.Position.Longitude}" : this.Name;

        /// <summary>
        /// Return a new CopterData with the same property values as the current one.
        /// </summary>
        /// <returns>The new CopterData instance.</returns>
        public CopterData Clone()
        {
            var location = new CopterData();
            location.Copy(this);
            return location;
        }

        /// <summary>
        /// Copies the property values of the specified location into the current location.
        /// </summary>
        /// <param name="location">The location to copy the values from.</param>
        public void Copy(CopterData location)
        {
            this.Name = location.Name;  
            this.DestmapIcon = location.DestmapIcon;
            this.Position = location.Position;
            this.DestmapLine = location.DestmapLine;
            this.copter = location.copter;
            this.IsMonitored = location.IsMonitored;
            
        }

    }
}
