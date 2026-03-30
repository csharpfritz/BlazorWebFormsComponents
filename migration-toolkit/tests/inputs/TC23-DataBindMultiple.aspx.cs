using System;
using System.Collections.Generic;

namespace MyApp
{
    public partial class TC23_DataBindMultiple : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            gvOrders.DataSource = GetOrders();
            gvOrders.DataBind();
            rptCategories.DataSource = GetCategories();
            rptCategories.DataBind();
        }

        private List<object> GetOrders() { return new List<object>(); }
        private List<object> GetCategories() { return new List<object>(); }
    }
}
