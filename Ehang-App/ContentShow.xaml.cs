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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace EHangApp
{
    public sealed partial class ContentShow : UserControl
    {
        public ContentShow()
        {
            this.InitializeComponent();
        }

        private void MissionTemplateName_KeyUp(object sender, KeyRoutedEventArgs e)
        {
          // MissionViewModel.templateName=  this.MissionTemplateName.Text.Trim();
        }

        private void MissionTemplateName_LostFocus(object sender, RoutedEventArgs e)
        {
           MissionViewModel.templateName = this.MissionTemplateName.Text.Trim();

        }

        private void MissionTemplateName_Unloaded(object sender, RoutedEventArgs e)
        {
            MissionViewModel.templateName = this.MissionTemplateName.Text.Trim();

        }
    }
}
