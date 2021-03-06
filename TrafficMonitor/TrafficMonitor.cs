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

using CopterHelper;
using Windows.ApplicationModel.Background;

namespace TrafficMonitor
{
    /// <summary>
    /// Represents a background task that monitors selected locations for an 
    /// increase in travel time due to traffic. 
    /// </summary>
    public sealed class TrafficMonitor : IBackgroundTask
    {
        /// <summary>
        /// Uses the CopterHelper class to check monitored locations
        /// and raise a notification if the travel time has increased. 
        /// </summary>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            bool isCanceled = false;
            taskInstance.Canceled += (s, e) => isCanceled = true;
            try
            {
                if (isCanceled)
                {
                    deferral.Complete();
                    return;
                }
                await CopterHelper.CopterHelper.CheckTravelInfoForMonitoredLocationsAsync();
            }
            finally
            {
                deferral.Complete();
            }
        }
    }

}
