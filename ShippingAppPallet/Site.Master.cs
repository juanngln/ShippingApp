using System;
using System.Web.UI;

namespace ShippingAppPallet
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Username"] == null)
            {
                phLoggedOut.Visible = true;
                phLoggedIn.Visible = false;

                Response.Redirect("~/Login.aspx");
                return;
            }
            else
            {
                phLoggedIn.Visible = true;
                phLoggedOut.Visible = false;

                litFullName.Text = Session["FullName"] != null ? Session["FullName"].ToString() : Session["Username"].ToString();
            }

            string userRole = Session["Role"] != null ? Session["Role"].ToString() : "";

            string currentPath = Request.AppRelativeCurrentExecutionFilePath.ToLower();

            bool isAuthorized = true;

            if (userRole.Equals("Logistic", StringComparison.OrdinalIgnoreCase))
            {
                if (currentPath.Contains("shiptolocation"))
                {
                    isAuthorized = false;
                }
            }
            else if (userRole.Equals("PA", StringComparison.OrdinalIgnoreCase))
            {
                if (currentPath.Contains("listpallet"))
                {
                    isAuthorized = false;
                }
            }
            else if (userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                isAuthorized = true;
            }

            if (!isAuthorized)
            {
                string script = "<script>alert('Akses Ditolak! Anda tidak memiliki otorisasi untuk mengakses menu ini.'); window.location.href='" + ResolveUrl("~/") + "';</script>";
                Response.Write(script);
                Response.End();
            }
        }
    }
}
