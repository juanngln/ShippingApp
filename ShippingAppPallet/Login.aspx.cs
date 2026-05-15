using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Web.Security;

namespace ShippingAppPallet
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RememberMe.InputAttributes.Add("class", "form-check-input");

            if (Request.QueryString["logout"] == "true")
            {
                Session.Clear();
                Session.Abandon();

                if (Request.Cookies["username"] != null)
                    Response.Cookies["username"].Expires = DateTime.Now.AddDays(-1);

                if (Request.Cookies["password"] != null)
                    Response.Cookies["password"].Expires = DateTime.Now.AddDays(-1);

                Response.Redirect(ResolveUrl("~/Login.aspx"));
            }

            if (!IsPostBack)
            {
                LoadRememberedUser();
            }
        }

        private void LoadRememberedUser()
        {
            if (Request.Cookies["username"] != null && Request.Cookies["password"] != null)
            {
                txtUsername.Text = Request.Cookies["username"].Value;

                try
                {
                    var bytes = Convert.FromBase64String(Request.Cookies["password"].Value);
                    var output = MachineKey.Unprotect(bytes, "ProtectCookie");
                    string result = Encoding.UTF8.GetString(output);
                    txtPassword.Attributes["value"] = result;
                    RememberMe.Checked = true;
                }
                catch
                {
                    txtPassword.Attributes["value"] = "";
                }
            }
        }

        private bool LoginDB(string username, string password, out string role)
        {
            role = "";
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT Role FROM udt_CDUsersShipping WHERE Username = @Username AND Password = @Password AND Enable = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);

                        conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            role = result.ToString();

                            string updateQuery = "UPDATE udt_CDUsersShipping SET LastLogin = GETDATE() WHERE Username = @Username";
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@Username", username);
                                updateCmd.ExecuteNonQuery();
                            }

                            Response.Cookies["Name"].Value = username;
                            Response.Cookies["Name"].Expires = DateTime.Now.AddDays(30);

                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogDebug("LoginDB Exception: " + ex.Message);
                return false;
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            lblMessage.Visible = false;

            try
            {
                LogDebug($"[STEP 1] Login attempt: {username} at {DateTime.Now}");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowAlert("Username and password cannot be empty.");
                    LogDebug("[FAILED] Empty username or password.");
                    return;
                }

                string role = "";

                if (!LoginDB(username, password, out role))
                {
                    ShowAlert("Login failed. Invalid credentials or account disabled.");
                    LogDebug("[FAILED] Database login failed or unauthorized.");
                    return;
                }

                LogDebug($"[STEP 2 & 3] DB login successful. Role: {role}");

                Session["UserID"] = username;
                Session["Username"] = username;
                Session["FullName"] = username;
                Session["Role"] = role;

                if (RememberMe.Checked)
                {
                    Response.Cookies["username"].Value = username;
                    Response.Cookies["username"].Expires = DateTime.Now.AddDays(1);

                    var cookieText = Encoding.UTF8.GetBytes(password);
                    var encryptedValue = Convert.ToBase64String(MachineKey.Protect(cookieText, "ProtectCookie"));
                    Response.Cookies["password"].Value = encryptedValue;
                    Response.Cookies["password"].Expires = DateTime.Now.AddDays(1);
                }
                else
                {
                    Response.Cookies["username"].Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies["password"].Expires = DateTime.Now.AddDays(-1);
                }

                LogDebug("[STEP 5] Redirecting to Dashboard...");

                string userRole = Session["Role"] != null ? Session["Role"].ToString() : "";
                if (userRole.Equals("Logistic", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("ListPallet.aspx", false);
                }
                else if (userRole.Equals("PA", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("ListShipmentPlan.aspx", false);
                }
                else if (userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("ListShipmentPlan.aspx", false);
                }
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error: " + ex.Message;
                lblMessage.Visible = true;
                LogDebug("[EXCEPTION] " + ex.ToString());
            }
        }

        private void ShowAlert(string message)
        {
            string script = $"<script>alert('{message.Replace("'", "\\'")}');</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "alert", script);
        }

        private void LogDebug(string message)
        {
            try
            {
                string logPath = Server.MapPath("App_Data/LoginLog.txt");
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logPath, true))
                {
                    sw.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch
            {
                // ignore error logging
            }
        }
    }
}
