using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace MyApp
{
    public partial class TC25_DataBindAndEvents : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            gvProducts.DataSource = GetProducts();
            gvProducts.DataBind();
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Handle row command
        }

        protected void Refresh_Click(object sender, EventArgs e)
        {
            gvProducts.DataSource = GetProducts();
            gvProducts.DataBind();
        }

        private List<object> GetProducts() { return new List<object>(); }
    }
}
