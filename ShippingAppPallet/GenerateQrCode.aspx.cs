using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using QRCoder;

namespace ShippingAppPallet
{
    public partial class GenerateQrCode : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string id = Request.QueryString["Id"];
                if (!string.IsNullOrEmpty(id))
                {
                    string qrData = LoadPalletDetail(id);

                    imgQrCode.ImageUrl = GenQrCode(qrData);
                }
            }
        }

        private string GenQrCode(string data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                using (Bitmap bitmap = qrCode.GetGraphic(6))
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string LoadPalletDetail(string palletId)
        {
            DataTable dtSummary = new DataTable();
            dtSummary.Columns.Add("Item");
            dtSummary.Columns.Add("Total Box");
            dtSummary.Columns.Add("Total Qty");
            dtSummary.Columns.Add("Production Date");

            StringBuilder qrText = new StringBuilder();
            qrText.AppendLine($"ID:{palletId}");

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            string productionDate = "";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand cmdDate = new SqlCommand("SELECT CreatedDate FROM udt_shPallet WHERE ID = @ID", conn))
                {
                    cmdDate.Parameters.AddWithValue("@ID", palletId);
                    object result = cmdDate.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        productionDate = Convert.ToDateTime(result).ToString("yyyy-MM-dd");
                    }
                }

                // 2. Ambil detail pallet menggunakan SP yang sama dengan PalletDetail.aspx
                using (SqlCommand cmd = new SqlCommand("udp_GetPalletDetailById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PalletId", palletId);

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        sda.Fill(ds);

                        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            var summaryData = ds.Tables[0].AsEnumerable()
                                .GroupBy(r => r["PartNumber"].ToString())
                                .Select(g => new
                                {
                                    Item = g.Key,
                                    TotalBox = g.Select(r => r["CartonBoxId"].ToString()).Distinct().Count(),
                                    TotalQty = g.Sum(r => Convert.ToInt32(r["Qty"])),
                                    ProductionDate = productionDate
                                }).ToList();

                            foreach (var item in summaryData)
                            {
                                dtSummary.Rows.Add(item.Item, item.TotalBox, item.TotalQty, item.ProductionDate);

                                qrText.AppendLine($"{item.Item}|Box:{item.TotalBox}|Qty:{item.TotalQty}");
                            }
                        }
                        else
                        {
                            qrText.AppendLine("Status:No Details");
                        }
                    }
                }
            }

            gvPalletDetail.DataSource = dtSummary;
            gvPalletDetail.DataBind();

            return qrText.ToString();
        }
    }
}
