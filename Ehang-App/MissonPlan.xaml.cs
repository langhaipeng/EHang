using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CopterHelper;
using System.Collections.ObjectModel;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI;
using EHangApp.Common;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.Storage.Streams;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Popups;
using MetroLog;
using MetroLog.Targets;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using EHang.Copters;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EHangApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MissonPlan : Page, INotifyPropertyChanged
    {

        ILogger log = LogManagerFactory.DefaultLogManager.GetLogger<MissonPlan>();

        #region navigation button
        private async void MainPageButton_Click(object sender, RoutedEventArgs e)
        {

            Frame.Navigate(typeof(MainPage));

        }

        private void DroneSetButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DronesSet));
        }

        private void FlyPlanButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MissonPlan));
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AppSet));
        }
        #endregion

        public MissonPlan()
        {
            this.InitializeComponent();
            MissionCollection = new ObservableCollection<MissionData>();
            this.InputMap.Style = MapStyle.Aerial3DWithRoads;
            MissionTypeModel model = new MissionTypeModel();
            model.SelectedTypeID = 0;

            this.combox_MissionType.ItemsSource = MissionTypeModel.typeCollections;
            this.combox_MissionType.SelectedIndex = model.SelectedTypeID;
            // this.MissionPanel.Visibility = Visibility.Collapsed;
        }

        public ObservableCollection<MissionData> MissionCollection { get; set; }

        private object selectedMission;
        /// <summary>
        /// Gets or sets the LocationData object corresponding to the current selection in the locations list. 
        /// </summary>
        public object SelectedMission
        {
            get { return this.selectedMission; }
            set
            {
                if (this.selectedMission != value)
                {
                    var oldValue = this.selectedMission as MissionData;
                    var newValue = value as MissionData;
                    if (oldValue != null)
                    {
                        oldValue.IsSelected = false;
                        oldValue.Typeid = this.combox_MissionType.SelectedIndex;
                    }
                    if (newValue != null)
                    {
                        newValue.IsSelected = true;
                    }
                    this.selectedMission = value;
                }
                // this.MissionPanel.Visibility = Visibility.Visible;

                setMissionType(value as MissionData);
                this.MissionInfo.DataContext = value;

            }
        }


        private void setMissionType(MissionData missionData)
        {
            if (missionData != null)
            {
                try
                {
                    MissionTypeModel model = new MissionTypeModel();
                    if (missionData.Typeid != -1)
                    {
                        model.SelectedTypeID = missionData.Typeid;
                    }

                    // this.combox_MissionType.ItemsSource = MissionTypeModel.typeCollections;
                    this.combox_MissionType.SelectedIndex = model.SelectedTypeID;
                }
                catch (Exception e)
                {
                    log.Trace("ReadMissionData  function:", e);
                    MessageDialog diag = new MessageDialog("设置航点类型失败。");
                    diag.ShowAsync();

                }
            }
        }


        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }


        private void setMapIconAndLine(Geopoint pos)
        {

            //插入图片，并画线
            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = pos;

            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon1.Title = "航点" + this.MissionCollection.Count;
            mapIcon1.ZIndex = 0;
            MissionData data = new MissionData();
            data.Name = mapIcon1.Title;
            data.Altitude = 10;
            data.Longitude = pos.Position.Longitude;
            data.Latitude = pos.Position.Latitude;
            this.InputMap.MapElements.Add(mapIcon1);


            if (this.MissionCollection.Count > 0) {
                MissionData lastData = this.MissionCollection[MissionCollection.Count - 1];
                MapPolyline mapPolyline = new MapPolyline();
                mapPolyline.Path = new Geopath(new List<BasicGeoposition>() {
         new BasicGeoposition() {Latitude=lastData.Latitude, Longitude=lastData.Longitude },
         new BasicGeoposition() {Latitude=pos.Position.Latitude, Longitude=pos.Position.Longitude },

          });

                mapPolyline.ZIndex = 1;
                mapPolyline.StrokeColor = Colors.Blue;
                mapPolyline.StrokeThickness = 3;
                mapPolyline.StrokeDashed = false;

                this.InputMap.MapElements.Add(mapPolyline);
            }

            this.MissionCollection.Add(data);
        }

        private void InputMap_MapHolding(MapControl sender, MapInputEventArgs args)
        {
            var pos = args.Location;
            this.setMapIconAndLine(pos);
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region map initial

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                try
                {
                    
                    CopterHelper.CopterHelper.Geolocator.StatusChanged += Geolocator_StatusChanged;
                    combox_MissionType.SelectionChanged += ComboBox_SelectionChanged;
                    
                    if (MainViewModel.currentCopterManager != null)
                    {
                        this.currentdroneid.Text = MainViewModel.currentCopterManager.Copter.Id;
                        if (MainViewModel.currentCopterManager.Copter.IsConnected)
                        {
                            this.currentdronestatus.Text = "已连接";
                        }
                        else
                        {
                            this.currentdronestatus.Text = "未连接";
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageDialog diag = new MessageDialog("定位飞行计划页面出错。");
                    await diag.ShowAsync();

                }
            }

            // this.Locations[0].Copter = await Foo();
        }

        /// <summary>
        /// Cancels any in-flight request to the Geolocator, and
        /// disconnects the Geolocator.StatusChanged event handler. 
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            CopterHelper.CopterHelper.CancelGetCurrentLocation();
            CopterHelper.CopterHelper.Geolocator.StatusChanged -= Geolocator_StatusChanged;
            combox_MissionType.SelectionChanged -= ComboBox_SelectionChanged;
        }

        /// <summary>
        /// Handles the Geolocator.StatusChanged event to refresh the map and locations list 
        /// if the Geolocator is available, and to display an error message otherwise.
        /// </summary>
        private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            try
            {
                await Helpers.CallOnUiThreadAsync(async () =>
                {
                    switch (args.Status)
                    {
                        case PositionStatus.Ready:
                            await this.ResetViewAsync();
                            break;
                        case PositionStatus.Initializing:
                            break;
                        case PositionStatus.NoData:
                        case PositionStatus.Disabled:
                        case PositionStatus.NotInitialized:
                        case PositionStatus.NotAvailable:
                        default:
                            await this.ResetViewAsync(false);
                            break;
                    }
                });
            }
            catch (Exception e)
            {
                string ex = e.StackTrace;
            }
        }

        private async Task ResetViewAsync(bool isGeolocatorReady = true)
        {
            CopterData currentLocation = null;
            if (isGeolocatorReady) currentLocation = await this.GetCurrentLocationAsync();

            List<BasicGeoposition> positions = new List<BasicGeoposition>();
            positions.Add(currentLocation.Geopoint.Position);
            var bounds = GeoboundingBox.TryCompute(positions);
            double viewWidth = ApplicationView.GetForCurrentView().VisibleBounds.Width;
            var margin = new Thickness((viewWidth >= 500 ? 300 : 10), 10, 10, 10);
            bool isSuccessful = false;

            try
            {
                isSuccessful = await this.InputMap.TrySetViewBoundsAsync(bounds, margin, MapAnimationKind.Default);
            }
            catch (Exception e)
            {
                var x = e.Message;
            }

            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = currentLocation.Geopoint;
            mapIcon1.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/mappin-yellow.png"));
            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
            this.InputMap.MapElements.Add(mapIcon1);

            /*

          if (isSuccessful && positions.Count < 2) this.InputMap.ZoomLevel = 12;
          else if (!isSuccessful && positions.Count > 0)
          {
              this.InputMap.Center = new Geopoint(positions[0]);
              this.InputMap.ZoomLevel = 12;
          }
          else
          {
              var sss = "";
          }
          */
            //this.InputMap.Center = currentLocation.Geopoint;
            //this.InputMap.ZoomLevel = 12;
        }









        private async Task<CopterData> GetCurrentLocationAsync()
        {
            var currentLocation = await CopterHelper.CopterHelper.GetCurrentLocationAsync();
            return currentLocation;
        }

        #endregion

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // string oldType = (e.RemovedItems[0] as MissionType).Name;
            string newType = (e.AddedItems[0] as MissionType).Name;
            if ((newType == "LAND") || (newType == "LAUNCH"))
            {
                LangtitudePanel.Visibility = Visibility.Collapsed;
                LongtitudePanel.Visibility = Visibility.Collapsed;
                AltitudePanel.Visibility = Visibility.Collapsed;
                RadiusPanel.Visibility = Visibility.Collapsed;
                SuspendPanel.Visibility = Visibility.Collapsed;
                CirclePanel.Visibility = Visibility.Collapsed;
                DirectPanel.Visibility = Visibility.Collapsed;
                HoverRadiusPanel.Visibility = Visibility.Collapsed;
            }
            else if (newType == "CIRCLETURNS")
            {
                LangtitudePanel.Visibility = Visibility.Visible;
                LongtitudePanel.Visibility = Visibility.Visible;
                AltitudePanel.Visibility = Visibility.Visible;
                RadiusPanel.Visibility = Visibility.Collapsed;
                SuspendPanel.Visibility = Visibility.Collapsed;
                CirclePanel.Visibility = Visibility.Visible;
                DirectPanel.Visibility = Visibility.Visible;
                HoverRadiusPanel.Visibility = Visibility.Visible;

            }
            else if (newType == "CIRCLETIME")
            {
                LangtitudePanel.Visibility = Visibility.Visible;
                LongtitudePanel.Visibility = Visibility.Visible;
                AltitudePanel.Visibility = Visibility.Visible;
                RadiusPanel.Visibility = Visibility.Collapsed;
                SuspendPanel.Visibility = Visibility.Visible;
                CirclePanel.Visibility = Visibility.Collapsed;
                DirectPanel.Visibility = Visibility.Visible;
                HoverRadiusPanel.Visibility = Visibility.Visible;

            }
            else if (newType == "FLYTO")
            {
                LangtitudePanel.Visibility = Visibility.Visible;
                LongtitudePanel.Visibility = Visibility.Visible;
                AltitudePanel.Visibility = Visibility.Visible;
                RadiusPanel.Visibility = Visibility.Visible;
                SuspendPanel.Visibility = Visibility.Visible;
                CirclePanel.Visibility = Visibility.Collapsed;
                DirectPanel.Visibility = Visibility.Visible;
                HoverRadiusPanel.Visibility = Visibility.Collapsed;

            }

        }

        private async void SaveToDroneButton_Click(object sender, RoutedEventArgs e)
        {
            
            if ((MainViewModel.currentCopterManager != null) && (MainViewModel.currentCopterManager.Copter.IsConnected))
            {
                if (!MainViewModel.currentCopterManager.Copter.State.Equals(CopterState.Locked))
                {
                    MessageDialog diag = new MessageDialog("必须在锁定状态下读写航线。");
                    await diag.ShowAsync();

                }
                else
                {
                    List<IMission> missions = new List<IMission>();
                    foreach (MissionData data in MissionCollection)
                    {
                        
                        if (data.TypeName == "FLYTO")
                        {
                            IMission mission = Mission.CreateWaypointMission(data.Latitude, data.Longitude, data.Altitude);
                            missions.Add(mission);

                        }
                        else if(data.TypeName=="LANDING")
                        {
                            IMission mission = Mission.CreateLandMission();
                            missions.Add(mission);
                        }
                    }
                    bool isSuccessful = false;

                    int i = 0;
                    while ((!isSuccessful) && (i < 10))
                    {
                        isSuccessful = await MainViewModel.currentCopterManager.Copter.WriteMissionListAsync(missions, 1000);
                        i++;
                    }
                    await Task.Delay(10000);

                    if (!isSuccessful)
                    {
                        MessageDialog diag1 = new MessageDialog("写入航线数据失败。");
                        await diag1.ShowAsync();

                    }
                    else
                    {
                        string retMsg = "写入航线成功。";
                        MessageDialog diag1 = new MessageDialog(retMsg);
                        await diag1.ShowAsync();
                    }
                }

            }
            else
            {
                MessageDialog diag = new MessageDialog("请先连接一架飞行器。");
                await diag.ShowAsync();
            }
            
        } 
         private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.MissionCollection=new ObservableCollection<MissionData>();
            MissionsView.ItemsSource = this.MissionCollection;

        }

        private async void ReadFromDroneButton_Click(object sender, RoutedEventArgs e)
        {
            
            if ((MainViewModel.currentCopterManager != null) && (MainViewModel.currentCopterManager.Copter.IsConnected))
            {
                if (!MainViewModel.currentCopterManager.Copter.State.Equals(CopterState.Locked))
                {
                    MessageDialog diag = new MessageDialog("必须在锁定状态下读写航线。");
                    await diag.ShowAsync();

                }
                else
                {
                    var missions = await MainViewModel.currentCopterManager.Copter.RequestMissionListAsync(5000);
                    await Task.Delay(5000);
                    string ret = string.Empty;
                    foreach (IMission mission in missions)
                    {
                        if (mission.Command.ToString().ToUpper().Equals("WAYPOINT"))
                        {
                            ret += mission.Sequence + "-" + mission.Command.ToString() + "-" + mission.Latitude + "-" + mission.Altitude + "-" + mission.Longitude + "\n";
                        }
                        else
                        {
                            ret += mission.Sequence + "-" + mission.Command.ToString() + "\n";
                        }
                    }
                    MessageDialog diag = new MessageDialog(ret);
                    await diag.ShowAsync();
                }
            }
            else
            {
                MessageDialog diag = new MessageDialog("请先连接一架飞行器。");
                await diag.ShowAsync();
            }
    
        }

        private async void SaveToFileButton_Click(object sender, RoutedEventArgs e)
        {
            
            ContentDialog content_dialog = new ContentDialog()
            {
                Title = "航线定义",
                Content = new ContentShow(),
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消",
                FullSizeDesired = false,
            };

            content_dialog.PrimaryButtonClick += (_s, _e) => { };

            ContentDialogResult ret = await content_dialog.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
                WriteMissionData(MissionViewModel.templateName);
            }
            
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".xml");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ReadMissionData(file);
            }
            
        }

        private async void ReadMissionData(Windows.Storage.StorageFile file)
        {
            try
            {
                MissionCollection.Clear();
                Windows.Data.Xml.Dom.XmlLoadSettings loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
                loadSettings.ProhibitDtd = false;
                loadSettings.ResolveExternals = false;
                // loadSettings.ElementContentWhiteSpace = true;
                Windows.Data.Xml.Dom.XmlDocument doc= await Windows.Data.Xml.Dom.XmlDocument.LoadFromFileAsync(file, loadSettings);
                var nodelist = doc.DocumentElement.SelectNodes("/missiondata/mission");
                foreach (var node in nodelist)
                {
                    MissionData mission = new MissionData();
                    mission.Typeid=Int32.Parse(node.Attributes.GetNamedItem("typeid").NodeValue.ToString());
                    mission.Name = node.Attributes.GetNamedItem("name").NodeValue.ToString();
                    mission.TypeName = MissionTypeModel.typeCollections[mission.Typeid].Name;
                    mission.Altitude = float.Parse(node.Attributes.GetNamedItem("altitude").NodeValue.ToString());
                    mission.Latitude = Double.Parse(node.Attributes.GetNamedItem("latitude").NodeValue.ToString());
                    mission.Longitude = Double.Parse(node.Attributes.GetNamedItem("longitude").NodeValue.ToString());
                    mission.Param1 = Int32.Parse(node.Attributes.GetNamedItem("param1").NodeValue.ToString());
                    mission.Param2 = Int32.Parse(node.Attributes.GetNamedItem("param2").NodeValue.ToString());
                    mission.Param3 = Int32.Parse(node.Attributes.GetNamedItem("param3").NodeValue.ToString());
                    mission.Param4 = Int32.Parse(node.Attributes.GetNamedItem("param4").NodeValue.ToString());
                    MissionCollection.Add(mission);
                }
                
            }
            catch (Exception e)
            {
                log.Trace("ReadMissionData  function:", e);
                MessageDialog diag = new MessageDialog("打开航线模板失败。");
                await diag.ShowAsync();

            }

        }

        private async void WriteMissionData(string fileName)
        {
            try
            {
                Windows.Data.Xml.Dom.XmlLoadSettings loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
                loadSettings.ProhibitDtd = false;
                loadSettings.ResolveExternals = false;
                Windows.Data.Xml.Dom.XmlDocument missionDoc = new Windows.Data.Xml.Dom.XmlDocument();
                var root = missionDoc.CreateElement("missiondata");
                missionDoc.AppendChild(root);
                foreach (MissionData data in MissionCollection)
                {
                    Windows.Data.Xml.Dom.XmlElement subElement = missionDoc.CreateElement("mission");
                    subElement.SetAttribute("name", data.Name);
                    subElement.SetAttribute("typeid", data.Typeid.ToString());
                    subElement.SetAttribute("typename", MissionTypeModel.typeCollections[data.Typeid].Name.ToString());
                    subElement.SetAttribute("altitude", data.Altitude.ToString());
                    subElement.SetAttribute("longitude", data.Longitude.ToString());
                    subElement.SetAttribute("latitude", data.Latitude.ToString());
                    subElement.SetAttribute("param1", data.Param1.ToString());
                    subElement.SetAttribute("param2", data.Param2.ToString());
                    subElement.SetAttribute("param3", data.Param3.ToString());
                    subElement.SetAttribute("param4", data.Param4.ToString());

                    root.AppendChild(subElement);
                }
                Windows.Storage.StorageFolder storageFolder =
                   KnownFolders.PicturesLibrary;
                Windows.Storage.StorageFile sampleFile =
                    await storageFolder.CreateFileAsync(fileName+".xml",
                        Windows.Storage.CreationCollisionOption.ReplaceExisting);
                ;
                await missionDoc.SaveToFileAsync(sampleFile);
            }
            catch (Exception e)
            {
                log.Trace("WriteMissionData  function:", e);
                MessageDialog diag = new MessageDialog("保存航线模板失败。");
                await diag.ShowAsync();

            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (!Regex.IsMatch(textbox.Text, "^\\d*\\.?\\d*$") && textbox.Text != "")
            {
                int pos = textbox.SelectionStart - 1;
                textbox.Text = textbox.Text.Remove(pos, 1);
                textbox.SelectionStart = pos;
            }
        }
    }
}
