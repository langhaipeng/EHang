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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EHangApp.Common;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using EHang.Copters;
using EHang.Communication;
using EHang.CopterManagement;
using Microsoft.Practices.ServiceLocation;
using CopterHelper;
using EHang.Messaging;
using GalaSoft.MvvmLight.Messaging;
using EHang.CopterControllers;
using Windows.Networking.Proximity;
using System.Threading;
using System.IO;


using Windows.Devices.Bluetooth.Rfcomm;
using Windows.System.Threading;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using EHang.Geography;
using Windows.UI;

namespace EHangApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>finput
    public sealed partial class MainPage : Page
    {
        private CopterData locationInEdit;
        private BasicGeoposition locationInEditOriginalPosition;
        private bool isNewLocationInEdit;
        private bool isMapSelectionEnabled;
        private bool isExistingLocationBeingRepositioned;
        public ICopter _copter;
        private ICopterManager _copterManager = ServiceLocator.Current.GetInstance<ICopterManager>();
        private IMessenger _messenger = ServiceLocator.Current.GetInstance<IMessenger>();
        #region Location data

        /// <summary>
        /// Gets or sets the saved locations. 
        /// </summary>
        public ObservableCollection<CopterData> Locations { get; private set; }

        /// <summary>
        /// Gets or sets the locations represented on the map; this is a superset of Locations, and 
        /// includes the current location and any locations being added but not yet saved. 
        /// </summary>
        public ObservableCollection<CopterData> MappedLocations { get; set; }

        private object selectedLocation;
        /// <summary>
        /// Gets or sets the CopterData object corresponding to the current selection in the locations list. 
        /// </summary>
        public object SelectedLocation
        {
            get { return this.selectedLocation; }
            set
            {
                if (this.selectedLocation != value)
                {
                    var oldValue = this.selectedLocation as CopterData;
                    var newValue = value as CopterData;
                    if (oldValue != null)
                    {
                        oldValue.IsSelected = false;
                       // this.InputMap.Routes.Clear();
                    }
                    if (newValue != null)
                    {
                        newValue.IsSelected = true;
                       // if (newValue.FastestRoute != null) this.InputMap.Routes.Add(new MapRouteView(newValue.FastestRoute));
                    }
                    this.selectedLocation = newValue;

                }
                 //this.flyinfo.DataContext=(value as CopterData).Copter;
               // this.EditLocation(value as CopterData);
            }
        }

        #endregion Location data

        /// <summary>
        /// Initializes a new instance of the class and sets up the association
        /// between the Locations and MappedLocations collections. 
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            this.Locations = new ObservableCollection<CopterData>();
            this.MappedLocations = new ObservableCollection<CopterData>(this.Locations);

            // MappedLocations is a superset of Locations, so any changes in Locations
            // need to be reflected in MappedLocations. 
            this.Locations.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null) foreach (CopterData item in e.NewItems) this.MappedLocations.Add(item);
                if (e.OldItems != null) foreach (CopterData item in e.OldItems) this.MappedLocations.Remove(item);
            };

            // Update the travel times every 5 minutes.
            Helpers.StartTimer(5, async () => await this.UpdateLocationsTravelInfoAsync());

            // Update the freshness timestamp every minute;
            Helpers.StartTimer(1, () => { foreach (var location in this.Locations) location.RefreshFormattedTimestamp(); });

           // _messenger.Register<CopterConnectedMessage>(this, m =>
          //  {
          //      AddOrMoveCopter(m.Copter);
          //  });
            _messenger.Register<CopterLocationChangedMessage>(this, m =>
            {
                AddOrMoveCopter(m.Copter);
            });
            DataContext = new MainViewModel();

            CopterHelper.CopterHelper.RegisterTrafficMonitor();

           




        }

    

        private void AddOrMoveCopter(ICopter copter)
        {
            if((copter.State!=CopterState.Initialized)&& (copter.State != CopterState.Locked))
                {
                (this.LocationsView.SelectedItem as CopterData).Position = new BasicGeoposition { Latitude = copter.Latitude, Longitude = copter.Longitude };
            }
               
        }

        #region todo

        /// <summary>
        /// Loads the saved location data on first navigation, and 
        /// attaches a Geolocator.StatusChanged event handler. 
        /// </summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                // Load sample location data.
                foreach (var location in await CopterDataStore.GetSampleCopterDataAsync()) this.Locations.Add(location);

                // Alternative: Load location data from storage.
                //foreach (var item in await CopterDataStore.GetCopterDataAsync()) this.Locations.Add(item);

                // Start handling Geolocator and network status changes after loading the data 
                // so that the view doesn't get refreshed before there is something to show.
                CopterHelper.CopterHelper.Geolocator.StatusChanged += Geolocator_StatusChanged;
                NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            }
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
            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
        }

      
        #region Geolocator and network status and map refresh code

        /// <summary>
        /// Handles the Geolocator.StatusChanged event to refresh the map and locations list 
        /// if the Geolocator is available, and to display an error message otherwise.
        /// </summary>
        private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            await Helpers.CallOnUiThreadAsync(async () =>
            {
                switch (args.Status)
                {
                    case PositionStatus.Ready:
                        this.UpdateLocationStatus(true);
                        await this.ResetViewAsync();
                        break;
                    case PositionStatus.Initializing:
                        break;
                    case PositionStatus.NoData:
                    case PositionStatus.Disabled:
                    case PositionStatus.NotInitialized:
                    case PositionStatus.NotAvailable:
                    default:
                        this.UpdateLocationStatus(false);
                        await this.ResetViewAsync(false);
                        break;
                }
            });
        }

        /// <summary>
        /// Handles the NetworkInformation.NetworkStatusChanged event to refresh the locations 
        /// list if the internet is available, and to display an error message otherwise.
        /// </summary>
        /// <param name="sender"></param>
        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await Helpers.CallOnUiThreadAsync(async () =>
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                bool isNetworkAvailable = profile != null;
                this.UpdateNetworkStatus(isNetworkAvailable);
                if (isNetworkAvailable) await this.ResetViewAsync();
            });
        }

        /// <summary>
        /// Updates the UI to account for the user's current position, if available, 
        /// resetting the MapControl bounds and refreshing the travel info. 
        /// </summary>
        /// <param name="isGeolocatorReady">false if the Geolocator is known to be unavailable; otherwise, true.</param>
        /// <returns></returns>
        private async Task ResetViewAsync(bool isGeolocatorReady = true)
        {
            CopterData currentLocation = null;
            if (isGeolocatorReady) currentLocation = await this.GetCurrentLocationAsync();

            if (currentLocation != null)
            {
                if (this.MappedLocations.Count > 0 && this.MappedLocations[0].IsCurrentLocation)
                {
                    this.MappedLocations.RemoveAt(0);
                }
                this.MappedLocations.Insert(0, new CopterData { Position = currentLocation.Position, IsCurrentLocation = true });
            }

            // Set the current view of the map control. 
            var positions = this.Locations.Select(loc => loc.Position).ToList();
            if (currentLocation != null) positions.Insert(0, currentLocation.Position);
            var bounds = GeoboundingBox.TryCompute(positions);
            double viewWidth = ApplicationView.GetForCurrentView().VisibleBounds.Width;
            var margin = new Thickness((viewWidth >= 500 ? 300 : 10), 10, 10, 10);
            bool isSuccessful=false;

            try
            {
                 isSuccessful = await this.InputMap.TrySetViewBoundsAsync(bounds, margin, MapAnimationKind.Default);
            }
            catch (Exception e)
            {
                var x = e.Message;
            }
            if (isSuccessful && positions.Count < 2) this.InputMap.ZoomLevel = 12;
            else if (!isSuccessful && positions.Count > 0)
            {
                this.InputMap.Center = new Geopoint(positions[0]);
                this.InputMap.ZoomLevel = 12;
            } else
            {
                var sss = "";
            }
            if (currentLocation != null) await this.TryUpdateLocationsTravelInfoAsync(this.Locations, currentLocation);
        }

        /// <summary>
        /// Updates the travel time and distance info for all locations in the Locations collection,
        /// based on the user's current position (if available). 
        /// </summary>
        private async Task UpdateLocationsTravelInfoAsync()
        {
            var currentLocation = await this.GetCurrentLocationAsync();
            if (currentLocation != null) await this.TryUpdateLocationsTravelInfoAsync(this.Locations, currentLocation);
        }

        /// <summary>
        /// Shows or hides the error message relating to network status, depending on the specified value.
        /// </summary>
        /// <param name="isNetworkAvailable">true if network resources are available; otherwise, false.</param>
        private void UpdateNetworkStatus(bool isNetworkAvailable)
        {
            this.MapServicesDisabledMessage.Visibility =
                isNetworkAvailable ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Shows or hides the error message relating to the Geolocator status, depending on the specified value.
        /// </summary>
        /// <param name="isLocationAvailable">true if the Geolocator is available; otherwise, false.</param>
        private void UpdateLocationStatus(bool isLocationAvailable)
        {
            this.LocationDisabledMessage.Visibility =
                isLocationAvailable ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Attempts to update the travel distance and time info for the specified locations, 
        /// relative to the current location, and raises an alert for each flagged location 
        /// if traffic is currently increasing the travel time by 10 minutes or more; also 
        /// updates the network status message depending on the results.
        /// </summary>
        private async Task<bool> TryUpdateLocationsTravelInfoAsync(IEnumerable<CopterData> locations, CopterData currentLocation)
        {
            bool isNetworkAvailable = await CopterHelper.CopterHelper.TryUpdateLocationsTravelInfoAsync(this.Locations, currentLocation);
            this.UpdateNetworkStatus(isNetworkAvailable);
            return isNetworkAvailable;
        }

        /// <summary>
        /// Gets the current location if the geolocator is available, 
        /// and updates the Geolocator status message depending on the results.
        /// </summary>
        /// <returns>The current location.</returns>
        private async Task<CopterData> GetCurrentLocationAsync()
        {
            var currentLocation = await CopterHelper.CopterHelper.GetCurrentLocationAsync();
            this.UpdateLocationStatus(currentLocation != null);
            return currentLocation;
        }

        #endregion Geolocator status and map refresh code

        #region Primary commands: app-bar buttons, map holding gesture

        /// <summary>
        /// Handles the button click event to hide or show the locations list, enabling 
        /// greater access to the map control with small windows. 
        /// </summary>
        private void ListDisplayModeButton_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleLocationsPaneVisibility();
        }

        /// <summary>
        /// Changes the visibility of the locations list, and updates
        /// the app bar button to reflect the new state. 
        /// </summary>
        private void ToggleLocationsPaneVisibility()
        {
            if (this.LocationsView.Visibility == Visibility.Visible)
            {
                this.LocationsView.Visibility = Visibility.Collapsed;
                this.ListDisplayModeButton.Icon = new SymbolIcon { Symbol = Symbol.OpenPane };
                ToolTipService.SetToolTip(this.ListDisplayModeButton, "Show locations list");
            }
            else
            {
                this.LocationsView.Visibility = Visibility.Visible;
                this.ListDisplayModeButton.Icon = new SymbolIcon { Symbol = Symbol.ClosePane };
                ToolTipService.SetToolTip(this.ListDisplayModeButton, "Hide locations list");
            }
        }

        /// <summary>
        /// Handles clicks to the Add Current button by adding a new location 
        /// to the Locations list with the user's current position.
        /// </summary>
        private async void AddCurrentLocation_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var currentLocation = await this.GetCurrentLocationAsync();
            if (currentLocation != null)
            {
                // Resolve the address given the geocoordinates.
                await CopterHelper.CopterHelper.TryUpdateMissingLocationInfoAsync(currentLocation, currentLocation);

                this.EditNewLocation(currentLocation);
            }

            (sender as Button).IsEnabled = true;
        }

        /// <summary>
        /// Handles clicks to the Add New button by creating a new, empty location
        /// in the Locations list and displaying the editor flyout. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNewLocation_Click(object sender, RoutedEventArgs e)
        {
            /*
            var newposition = new BasicGeoposition
            {
                Latitude = this.Locations[0].Position.Latitude + 0.01,
                Longitude = this.Locations[0].Position.Longitude + 0.01
            };
            this.Locations[0].Position = newposition;
            */
           //this.EditNewLocation(new CopterData());
        }

        /// <summary>
        /// Handles the Holding event of the MapControl to add a new location
        /// to the Locations list, using the position indicated by the gesture.
        /// </summary>
        private async void InputMap_MapHolding(MapControl sender, MapInputEventArgs args)
        {
            var location = new CopterData { Position = args.Location.Position };
            
            var pos = args.Location;
             //InputMap.GetLocationFromOffset(args.Location.Position, out pos);
            // if ((_copterManager.Copter.IsConnected) )//&& (_copterManager.Copter.State.Equals(CopterState.CommandMode)))
            {

                // await _copterManager.UnlockAsync();
                string msg = "当前飞机位置：高度-" + _copterManager.Copter.Altitude.ToString() + ";\n";
                msg += "经度-" + _copterManager.Copter.Longitude.ToString() + ";纬度-" + _copterManager.Copter.Latitude.ToString() + ";\n";
                msg += "要飞往位置：距离-" + this.CalcDistance(_copterManager.Copter, pos) + "米;\n";
                msg += "经度-" + pos.Position.Longitude.ToString() + ";纬度-" + pos.Position.Latitude.ToString() + ";\n";
                var dialog = new ContentDialog()
                {
                    Title = "消息提示",
                    Content = msg,
                    PrimaryButtonText = "确定",
                    SecondaryButtonText = "取消",
                    FullSizeDesired = false,
                };

                dialog.PrimaryButtonClick += (_s, _e) => { };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await _copterManager.FlyToAsync(pos.Position.Latitude, pos.Position.Longitude);
                    /*
                    bool isFly = await _copterManager.CheckStatusAndFlyToAsync(pos.Position.Latitude, pos.Position.Longitude);
                    if (!isFly)
                    {
                        string retMsg = _copterManager.Copter.StatusText;
                        if(retMsg==null)
                        {
                            retMsg = "飞行器状态不容许飞行。";
                        }
                        MessageDialog diag = new MessageDialog(retMsg);
                        await diag.ShowAsync();
                    }
                    */

                    this.setMapIconAndLine(pos);



                }

            }
        }

        private void setMapIconAndLine(Geopoint pos)
        {

            var copterdata = (this.SelectedLocation as CopterData);
            //插入图片，并画线
            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = pos;
            
            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon1.Title = "目的地";
            mapIcon1.ZIndex = 0;
            if(copterdata.DestmapIcon!=null)
            {
                this.InputMap.MapElements.Remove(copterdata.DestmapIcon);
            }
            this.InputMap.MapElements.Add(mapIcon1);
            copterdata.DestmapIcon = mapIcon1;
                    MapPolyline mapPolyline = new MapPolyline();
            mapPolyline.Path = new Geopath(new List<BasicGeoposition>() {
         new BasicGeoposition() {Latitude=_copterManager.Copter.Latitude, Longitude=_copterManager.Copter.Longitude },
         new BasicGeoposition() {Latitude=pos.Position.Latitude, Longitude=pos.Position.Longitude },

   });

            mapPolyline.ZIndex = 1;
            mapPolyline.StrokeColor = Colors.Blue;
            mapPolyline.StrokeThickness = 3;
            mapPolyline.StrokeDashed = false;
            if (copterdata.DestmapLine != null)
            {
                this.InputMap.MapElements.Remove(copterdata.DestmapLine);
            }
            this.InputMap.MapElements.Add(mapPolyline);
            copterdata.DestmapLine = mapPolyline;
        }


        #endregion Primary commands: app-bar buttons, map holding gesture

        #region Location commands: per-location buttons

        /// <summary>
        /// Handles edit button clicks to enter edit mode for the selected location.
        /// </summary>
        private void EditLocation_Click(object sender, RoutedEventArgs e)
        {
            this.EditLocation(this.GetLocation(sender as Button));
        }

        /// <summary>
        /// Handles delete button clicks to remove the selected
        /// location from the Locations collection. 
        /// </summary>
        private async void DeleteLocation_Click(object sender, RoutedEventArgs e)
        {
            var location = this.GetLocation(sender as Button);
            int index = this.Locations.IndexOf(location);
            this.Locations.Remove(location);
            await CopterDataStore.SaveCopterDataAsync(this.Locations);
        }

        /// <summary>
        /// Handles clicks to the Show Route button to launch the Maps app and display 
        /// the route to the selected location from the user's current position. 
        /// </summary>
        private async void ShowRouteButton_Click(object sender, RoutedEventArgs e)
        {
            var currentLocation = await this.GetCurrentLocationAsync();
            if (currentLocation != null)
            { 
                var location = this.GetLocation(sender as Button);
                await CopterHelper.CopterHelper.ShowRouteToLocationInMapsAppAsync(location, currentLocation);
            }
        }

        /// <summary>
        /// Handles clicks to the Track button to flag the selected location for monitoring 
        /// by a background task that periodically checks traffic and sends a notification 
        /// whenever traffic adds 10 minutes or more to the travel time.
        /// </summary>
        private async void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            var location = this.GetLocation(button);
            location.IsMonitored = button.IsChecked.Value;
            this.UpdateTrafficMonitor(button.IsChecked.Value);
            await CopterDataStore.SaveCopterDataAsync(this.Locations);
        }

        /// <summary>
        /// Gets the data context of the specified element as a CopterData instance.
        /// </summary>
        /// <param name="element">The element bound to the location.</param>
        /// <returns>The location bound to the element.</returns>
        private CopterData GetLocation(FrameworkElement element) => 
            (element.FindName("Presenter") as FrameworkElement).DataContext as CopterData;

        /// <summary>
        /// Registers or unregisters the traffic monitoring background task depending 
        /// on whether the number of tracked locations changes from 1 to 0 or from 0 to 1.
        /// </summary>
        /// <param name="isIncrement">true if a location was just flagged; 
        /// false if a location was just unflagged.</param>
        private void UpdateTrafficMonitor(bool isIncrement)
        {
            var monitoredLocationCount = this.Locations.Count(location => location.IsMonitored);
            if (isIncrement && monitoredLocationCount == 1) CopterHelper.CopterHelper.RegisterTrafficMonitor();
            else if (monitoredLocationCount == 0) CopterHelper.CopterHelper.UnregisterTrafficMonitor();
        }

        #endregion Location commands: per-location buttons

        #region Editor functionality

        /// <summary>
        /// Handles clicks to the Save button by saving location edits. 
        /// </summary>
        private async void FlyoutSave_Click(object sender, RoutedEventArgs e)
        {
            await this.SaveAsync((sender as FrameworkElement).DataContext as CopterData);
        }

     
        /// <summary>
        /// Adds the specified location to the Locations list and shows the editor flyout.
        /// </summary>
        public void EditNewLocation(CopterData location)
        {
            if (this.LocationsView.Visibility == Visibility.Collapsed) this.ToggleLocationsPaneVisibility();
            this.Locations.Add(location);
            this.LocationsView.UpdateLayout();
            this.isNewLocationInEdit = true;
            this.EditLocation(location);
        }

        /// <summary>
        /// Opens the editor, binding it to a temporary copy of the current location 
        /// that can be saved, or discarded if the user dismisses the editor. 
        /// </summary>
        /// <param name="location"></param>
        private void EditLocation(CopterData location)
        {
            this.locationInEdit = location;
            //var element = this.GetTemplateRootForLocation(location);
            var element = this.LocationsView as FrameworkElement;
            var flyout = Flyout.GetAttachedFlyout(element) as Flyout;
            //(flyout.Content as FrameworkElement).DataContext = location.Clone();
            //flyout.ShowAt(element);
        }

        /// <summary>
        /// Applies the changes represented by the specified CopterData to 
        /// the cached location in edit, saves the changes to the file system,
        /// and updates the user interface to account for the changes. 
        /// </summary>
        private async Task SaveAsync(CopterData workingCopy)
        {
            Flyout.GetAttachedFlyout(this.GetTemplateRootForLocation(this.locationInEdit)).Hide();

            this.isNewLocationInEdit = false;
            this.isExistingLocationBeingRepositioned = false;
            bool isAddressNew = workingCopy.Address != this.locationInEdit.Address;
            bool areCoordinatesNew = !workingCopy.Position.Equals(this.locationInEdit.Position);

            // If just the address OR just the coordinates are new, 
            // clear the other value so that it can be updated.
            if (isAddressNew ^ areCoordinatesNew)
            {
                if (isAddressNew) workingCopy.Position = new BasicGeoposition(); 
                if (areCoordinatesNew) workingCopy.Address = string.Empty;
            }

            // If the address, the coordinates, or both have changed, clear the travel 
            // info and the route so that it doesn't reflect the old position.
            if (isAddressNew || areCoordinatesNew)
            {
                //workingCopy.ClearTravelInfo();
                this.InputMap.Routes.Clear();
            }

            this.locationInEdit.Copy(workingCopy);

            var currentLocation = await this.GetCurrentLocationAsync();
            if (currentLocation != null)
            {
                if (isAddressNew ^ areCoordinatesNew)
                {
                    await CopterHelper.CopterHelper.TryUpdateMissingLocationInfoAsync(this.locationInEdit, currentLocation);
                }
            }

            await CopterDataStore.SaveCopterDataAsync(this.Locations);

            if (currentLocation != null)
            {
                bool isNetworkAvailable = await this.TryUpdateLocationsTravelInfoAsync(this.Locations, currentLocation);
               // if (isNetworkAvailable) this.InputMap.Routes.Add(new MapRouteView(this.locationInEdit.FastestRoute));
            }
        }

        /// <summary>
        /// Handles the light-dismiss of the editor flyout to cancel edit mode.
        /// </summary>
        private void Flyout_Closed(object sender, object e)
        {
            // Do nothing if the flyout is closing in order to enter map selection mode. 
            if (this.isMapSelectionEnabled) return;

            // If a new location is still in edit, then the user has light-dismissed 
            // the editor without saving. In this case, delete the new location.
            else if (this.isNewLocationInEdit)
            {
                this.isNewLocationInEdit = false;
                this.Locations.RemoveAt(this.Locations.Count - 1);
            }

            // If the user has repositioned an existing location but has not yet 
            // saved the changes, revert the position to the original one. 
            else if (this.isExistingLocationBeingRepositioned)
            {
                this.isExistingLocationBeingRepositioned = false;
                this.locationInEdit.Position = this.locationInEditOriginalPosition;
            }
        }

        /// <summary>
        /// Gets the UI element that represents the specified location; 
        /// used to access the attached editor flyout. 
        /// <param name="location">The location to edit.</param>
        /// <returns>The element that represents the location.</returns>
        private FrameworkElement GetTemplateRootForLocation(CopterData location)
        {
            var item = this.LocationsView.ContainerFromItem(location) as ListViewItem;
            return item.ContentTemplateRoot as FrameworkElement;
        }

        #endregion Editor functionality

        #region Map selection mode for repositioning a location

       

        /// <summary>
        /// Handles the Tapped event of the map control to reposition a location 
        /// when map selection mode is enabled.
        /// </summary>
        private async void InputMap_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            this.InputMap.Routes.Clear();
            this.isExistingLocationBeingRepositioned = true;
            this.locationInEditOriginalPosition = this.locationInEdit.Position;
            this.locationInEdit.Position = args.Location.Position;

            var element = this.GetTemplateRootForLocation(this.locationInEdit);
            var flyout = Flyout.GetAttachedFlyout(element) as Flyout;
            var location = (flyout.Content as FrameworkElement).DataContext as CopterData;

            location.Position = args.Location.Position;
            location.Address = String.Empty;
            await CopterHelper.CopterHelper.TryUpdateMissingLocationInfoAsync(location, null);

            this.DisableMapSelection();
            flyout.ShowAt(element);
        }

        /// <summary>
        /// Enters map selection mode, where the user can reposition the selected 
        /// location by tapping a new location on the map control. 
        /// </summary>
        private void EnterMapSelectionMode(object sender, RoutedEventArgs e)
        {
            this.isMapSelectionEnabled = true;
            this.InputMap.MapTapped += InputMap_MapTapped;
            this.InputMap.MapHolding -= InputMap_MapHolding;
            this.LocationsView.Visibility = Visibility.Collapsed;
            this.MapSelectionModeMessage.Visibility = Visibility.Visible;
            this.ListDisplayModeButton.IsEnabled = false;
            this.AddCurrentLocationButton.IsEnabled = false;
            this.AddNewLocationButton.IsEnabled = false;
            Flyout.GetAttachedFlyout(this.GetTemplateRootForLocation(this.locationInEdit)).Hide();
        }

        /// <summary>
        /// Leaves map selection mode. 
        /// </summary>
        private void DisableMapSelection()
        {
            //this.isMapSelectionEnabled = false;
            //this.InputMap.MapTapped -= InputMap_MapTapped;
            this.InputMap.MapHolding += InputMap_MapHolding;
            this.LocationsView.Visibility = Visibility.Visible;
            this.MapSelectionModeMessage.Visibility = Visibility.Collapsed;
            this.ListDisplayModeButton.IsEnabled = true;
            this.AddCurrentLocationButton.IsEnabled = true;
            this.AddNewLocationButton.IsEnabled = true;
        }

        #endregion Map selection mode for repositioning a location

        private async void MainPageButton_Click(object sender, RoutedEventArgs e)
        {

           

        }

        async void query_ContentsChanged(Windows.Storage.Search.IStorageQueryResultBase sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("file add in picture folder");
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderByDate, null)
            {
                IndexerOption = IndexerOption.DoNotUseIndexer,
                FolderDepth = FolderDepth.Deep,
            };
            StorageFileQueryResult queryResult = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOptions);

            IReadOnlyCollection<StorageFile> files = await queryResult.GetFilesAsync(0, 1);
            /*
            await Dispatcher.RunAsync(
            CoreDispatcherPriority.High,
            () =>
            {

                var imgpath = files.FirstOrDefault<StorageFile>().Path;
                var bitmapImage = new BitmapImage(new Uri(imgpath, UriKind.Relative));
                this.CapturedImage.Source = bitmapImage;
            }
            );
            */
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Image_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Create a menu and add commands specifying a callback delegate for each.
            // Since command delegates are unique, no need to specify command Ids.
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand("Open with", (command) =>
            {
            }));
            menu.Commands.Add(new UICommand("Save attachment", (command) =>
            {
            }));

            // We don't want to obscure content, so pass in a rectangle representing the sender of the context menu event.
            // We registered command callbacks; no need to handle the menu completion event
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private void LocationsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //var flyout = Flyout.GetAttachedFlyout(sender as FrameworkElement) as Flyout;
            // flyout.ShowAt(sender as FrameworkElement);

        }

        #endregion

        /// <summary>
        /// 创建虚拟飞行器代理对象。
        /// </summary>
        /// <returns><see cref="ICopter"/> 实例。</returns>
        private static ICopter CreateFakeCopter()
        {
            var copter = new FakeCopter();
            copter.SetProperties(id: "FakeCopter", latitude: 23.14183333, longitude: 113.40184166);
            return copter;
        }

        public async Task<ICopter> Foo()
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
            }catch(Exception ex)
            {
                string retMsg = "连接飞行器失败，返回信息为：" + ex.ToString();
            MessageDialog diag = new MessageDialog(retMsg);
            await diag.ShowAsync();
                return null;
            }
    }

        private async Task<IReadOnlyList<PeerInformation>> getBTs()
        {
            IReadOnlyList<PeerInformation> peers = await PeerFinder.FindAllPeersAsync();
            if (peers.Count > 0)
            {
            }
            return peers;
        }

        /// <summary>
        /// 获取已配对的 VR 眼镜/G-BOX 列表。注意：由于 UWP 的限制，需要先在系统设置中配对，密码是 1234。
        /// </summary>
        /// <returns>已配对的 VR 眼镜/G-BOX 列表。</returns>
        private async Task<IEnumerable<PeerInformation>> FindBluetoothPeersAsync()
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

        private ICopter CreateBluetoothCopter(string hostName, string copterName)
        {
            var connection = new BluetoothConnection(hostName);
            var copter = new EHCopter(connection, SynchronizationContext.Current)
            {
                Id = "Bluetooth_" + hostName,
                Name = copterName
            };
            return copter;
        }
        private async void MenuFlyoutItem_Connect_Click(object sender, RoutedEventArgs e)
        {
            var copter = (this.SelectedLocation as CopterData).Copter;
           // var copter = (LocationsView.SelectedItem as CopterData).Copter;
            //double y1 = copter.Longitude;
            //double y2 = copter.Latitude;

           // await _copterManager.ConnectAsync(copter);

            //double x1=copter.Longitude;
            //double x2 = copter.Latitude;

           // ICopter copter = await Foo();
            (this.SelectedLocation as CopterData).Copter = copter;
           await _copterManager.ConnectAsync(copter);
            //MessageDialog diag = new MessageDialog(_copterManager.Copter.StatusText);
            //await diag.ShowAsync();
        }


        private async void InputMap_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

        

        }

        private async void MenuFlyoutItem_Return_Click(object sender, RoutedEventArgs e)
        {
            await _copterManager.ReturnToLaunchAsync();
        }

        private async void MenuFlyoutItem_TakeOff_Click(object sender, RoutedEventArgs e)
        {
           // await _copterManager.UnlockAsync();
            await _copterManager.TakeOffAsync();
        }

        private async void MenuFlyoutItem_UnLock_Click(object sender, RoutedEventArgs e)
        {
            
            await _copterManager.UnlockAsync();
        }

        private async void MenuFlyoutItem_Lock_Click(object sender, RoutedEventArgs e)
        {
               MessageDialog diag = new MessageDialog("真的要锁定吗？螺旋桨将立即停转。");
            diag.Commands.Add(new UICommand("确定", cmd => { }, commandId: 0));
            diag.Commands.Add(new UICommand("取消", cmd => { }, commandId: 1));


           IUICommand cmd1= await diag.ShowAsync();
            if (cmd1.Label == "确定")
            {
                await _copterManager.LockAsync();
            }
        }

        private async void MenuFlyoutItem_Hover_Click(object sender, RoutedEventArgs e)
        {
          await  _copterManager.HoverAsync();
        }

        private async void MenuFlyoutItem_Landing_Click(object sender, RoutedEventArgs e)
        {
            await _copterManager.LandAsync();
        }
        private async void MenuFlyoutItem_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            await _copterManager.DisconnectAsync();
        }


        public  double CalcDistance(ICopter copter, Geopoint loc2)
        {
            if (copter == null)
            {
                throw new ArgumentNullException(nameof(copter));
            }
            if (loc2 == null)
            {
                throw new ArgumentNullException(nameof(loc2));
            }
            return GeographyUtils.CalcDistance(copter.Latitude, copter.Longitude, copter.Altitude, loc2.Position.Latitude, loc2.Position.Longitude, copter.Altitude);
        }
    }
}
