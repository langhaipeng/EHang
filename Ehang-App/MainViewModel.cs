using EHang.CopterManagement;
using Microsoft.Practices.ServiceLocation;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
namespace EHangApp
{
    public class MainViewModel
    {
        public ICopterManager CopterManager { get; } = ServiceLocator.Current.GetInstance<ICopterManager>();

        public static Dictionary<string,ICopterManager> copManagers=new Dictionary<string,ICopterManager>();

        public static ICopterManager currentCopterManager { get; set; }

    }
}
