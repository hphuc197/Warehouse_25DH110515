using System.Web;
using System.Web.Mvc;

namespace Warehouse_25DH110515
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
