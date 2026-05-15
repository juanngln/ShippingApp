using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ShippingAppPallet
{
    public partial class ScanPallet : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void txtScanBox_TextChanged(object sender, EventArgs e)
        {
            string boxNumber = txtScanBox.Text.Trim();
            if (string.IsNullOrEmpty(boxNumber) || string.IsNullOrEmpty(selectPlan.SelectedValue))
                return;

            String connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT PalletID FROM Box WHERE BoxNumber=@BoxNumber", conn);
                cmd.Parameters.AddWithValue("@BoxNumber", boxNumber);
                int palletId = (int)cmd.ExecuteScalar();

                SqlCommand palletCmd = new SqlCommand("SELECT BoxNumber, PartNumber, Qty FROM Box WHERE PalletID=@PalletID", conn);
                palletCmd.Parameters.AddWithValue("@PalletID", palletId);
                SqlDataReader reader = palletCmd.ExecuteReader();
                gvPalletDetails.DataSource = reader;
                gvPalletDetails.DataBind();
                reader.Close();

                SqlCommand validateCmd = new SqlCommand(@"
                    SELECT b.PartNumber, SUM(b.Qty) AS PalletQty, sp.Qty AS PlanQty
                    FROM Box b
                    JOIN ShipmentPlanDetail sp ON b.PartNumber = sp.PartNumber
                    WHERE b.PalletID=@PalletID AND sp.ShipmentPlanID=@ShipmentPlanID
                    GROUP BY b.PartNumber, sp.Qty", conn);
                validateCmd.Parameters.AddWithValue("@PalletID", palletId);
                validateCmd.Parameters.AddWithValue("@ShipmentPlanID", selectPlan.SelectedValue);

                SqlDataReader valReader = validateCmd.ExecuteReader();
                bool isValid = true;
                while (valReader.Read())
                {
                    if ((int)valReader["PalletQty"] > (int)valReader["PlanQty"])
                    {
                        isValid = false;
                        break;
                    }
                }
                lblStatus.Text = isValid ? "✅ Data sesuai dengan Shipment Plan" : "❌ Qty mismatch!";
            }

            txtScanBox.Text = "";
        }
    }
}
