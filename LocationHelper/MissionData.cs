

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
    public class MissionData : BindableBase
    {

        

        private int index;


        private string name;
        /// <summary>
        /// Gets or sets the name of the Mission.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.SetProperty(ref this.name, value); }
        }
        private int typeid=-1;
        public int Typeid
        {
            get
            {
                return typeid;
            }

            set
            {
                typeid = value;
            }
        }

        private string typeName;

        private double longitude=0;

        public double Longitude
        {
            get
            {
                return longitude;
            }

            set
            {
                longitude = value;
            }
        }

        private float altitude=0;

        public float Altitude
        {
            get
            {
                return altitude;
            }

            set
            {
                altitude = value;
            }
        }

        private double latitude=0;

        public double Latitude
        {
            get
            {
                return latitude;
            }

            set
            {
                latitude = value;
            }
        }

        public string Channelid
        {
            get
            {
                return channelid;
            }

            set
            {
                channelid = value;
            }
        }

        public string Channelcommand
        {
            get
            {
                return channelcommand;
            }

            set
            {
                channelcommand = value;
            }
        }

        public int Param1
        {
            get
            {
                return param1;
            }

            set
            {
                param1 = value;
            }
        }

        public int Param2
        {
            get
            {
                return param2;
            }

            set
            {
                param2 = value;
            }
        }

        public int Param3
        {
            get
            {
                return param3;
            }

            set
            {
                param3 = value;
            }
        }

        public int Param4
        {
            get
            {
                return param4;
            }

            set
            {
                param4 = value;
            }
        }

        public int Index
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }

        private string channelid;

        private string channelcommand;

        private int param1=0;

        private int param2=0;

        private int param3=0;

        private int param4=0;

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

        public string TypeName
        {
            get
            {
                return typeName;
            }

            set
            {
                typeName = value;
            }
        }
    }

    public class MissionType
    {
        string name;

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        string description;
    }


    public class MissionTypeModel
    {
        public static ObservableCollection<MissionType> typeCollections { get; set; }

         public MissionTypeModel() {
            typeCollections = new ObservableCollection<MissionType>
        {
            new MissionType() { Name = "FLYTO",Description = "飞往目标位置"},
            new MissionType() { Name = "LAND",Description = "降落"},
            new MissionType() { Name = "LAUNCH",Description = "返航"},
            new MissionType() { Name = "CIRCLETURNS",Description = "盘旋指定圈数"},
            new MissionType() { Name = "CIRCLETIME",Description = "盘旋指定时长"},

        };
        }
        public int SelectedTypeID { get; set; }

    }
}
