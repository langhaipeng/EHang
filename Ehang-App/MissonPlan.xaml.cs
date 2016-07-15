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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EHangApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MissonPlan : Page, INotifyPropertyChanged
    {
        

 
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
                setMissionType(value as MissionData);
                this.MissionInfo.DataContext = value;
            }
        }


        private void setMissionType(MissionData missionData)
        {
            MissionTypeModel model = new MissionTypeModel();
            if (missionData.Typeid != -1)
            {
                model.SelectedTypeID = missionData.Typeid;
            }
            
            this.combox_MissionType.ItemsSource = MissionTypeModel.typeCollections;
            this.combox_MissionType.SelectedIndex = model.SelectedTypeID;
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
            data.Altitude = pos.Position.Altitude;
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
                CopterHelper.CopterHelper.Geolocator.StatusChanged += Geolocator_StatusChanged;
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
            catch(Exception e)
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
            mapIcon1.Image= RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/mappin-yellow.png"));
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
            string x = (e.RemovedItems[0] as MissionType).Description;
            string y= (e.AddedItems[0] as MissionType).Description;
        }
    }
}
