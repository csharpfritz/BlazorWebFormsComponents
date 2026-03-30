using System;
using System.Collections.Generic;

namespace MyApp
{
    public partial class TC22_DataBindGridView : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                gvProducts.DataSource = GetProducts();
                gvProducts.DataBind();
            }
        }

        private List<object> GetProducts()
        {
            return new List<object>();
        }
    }
}
