using EHang.CopterManagement;
using Microsoft.Practices.ServiceLocation;

namespace EHangApp
{
    public class MainViewModel
    {
        public ICopterManager CopterManager { get; } = ServiceLocator.Current.GetInstance<ICopterManager>();
    }
}
