using System;
using System.Linq;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace ShippingAppPallet
{
    public partial class PalletDetail : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection oConn = new SqlConnection(ConnectionString))
            {
                oConn.Open();
                using (SqlCommand SqlCmdDetail = new SqlCommand("udp_GetPalletDetailById", oConn))
                {
                    SqlCmdDetail.CommandType = CommandType.StoredProcedure;
                    SqlCmdDetail.Parameters.AddWithValue("@PalletId", Request.QueryString["ID"]);
                    using (SqlDataAdapter da = new SqlDataAdapter(SqlCmdDetail))
                    {
                        using (DataSet dt = new DataSet())
                        {
                            da.Fill(dt);
                            StringBuilder sb = new StringBuilder();

                            string iid = "";
                            if (dt.Tables[0].Rows.Count > 0)
                            {
                                iid = dt.Tables[0].Rows[0]["PalletID"].ToString();
                            }

                            string palletNumber = "";
                            if (dt.Tables[1].Rows.Count > 0 && dt.Tables[1].Columns.Contains("ID"))
                            {
                                palletNumber = dt.Tables[1].Rows[0]["ID"].ToString();
                            }

                            int totalBox = dt.Tables[0].AsEnumerable()
                                            .Select(r => r["CartonBoxId"].ToString())
                                            .Distinct()
                                            .Count();

                            int totalPartNumber = dt.Tables[0].AsEnumerable()
                                                   .Select(r => r["PartNumber"].ToString())
                                                   .Distinct()
                                                   .Count();

                            int totalQty = dt.Tables[0].AsEnumerable()
                                             .Sum(r => Convert.ToInt32(r["Qty"]));

                            var summaryData = dt.Tables[0].AsEnumerable()
                                                .GroupBy(r => r["PartNumber"].ToString())
                                                .Select(g => new
                                                {
                                                    PartNumber = g.Key,
                                                    BoxCount = g.Select(r => r["CartonBoxId"].ToString()).Distinct().Count(),
                                                    TotalQty = g.Sum(r => Convert.ToInt32(r["Qty"]))
                                                }).ToList();

                            sb.Append("<div class='d-flex mb-4'>");
                            //sb.Append("<a href='' type='button' class='btn btn-primary btn-sm waves-effect waves-light me-2'><i class='fa fa-envelope'></i> Send Email Request LOD</a>");
                            sb.Append("<a href='GenerateQrCode.aspx?Id=" + iid + "' type='button' class='btn btn-primary btn-sm waves-effect waves-light me-2'><i class='fa fa-qrcode'></i> Print Pallet Number for Customer</a>");
                            //sb.Append("<a href='' type='button' class='btn btn-warning btn-sm waves-effect waves-light'><i class='fa fa-qrcode'></i> Re-Print Pallet Production</a>");
                            sb.Append("</div>");

                            sb.Append("<div class='row'>");

                            // ================= Column 1 =================
                            sb.Append("<div class='col-md-6'>");

                            sb.Append("<div class='d-flex flex-column mb-3'>");
                            sb.Append("<span class='my-auto fw-bold text-uppercase'>Pallet Information</span>");
                            sb.Append("<span class='my-auto text-muted'>Detail Pallet and Carton Box</span>");
                            sb.Append("</div>");

                            sb.AppendLine("<div class='container mb-4 px-0'>");
                            sb.AppendLine("<div class='row mb-2'>");
                            sb.AppendLine("<div class='col-4 fw-semibold'>Pallet Number</div><div class='col-1 text-center'>:</div><div class='col-7'>" + palletNumber + "</div>");
                            sb.AppendLine("</div>");
                            sb.AppendLine("<div class='row mb-2'>");
                            sb.AppendLine("<div class='col-4 fw-semibold'>Total Box</div><div class='col-1 text-center'>:</div><div class='col-7'>" + totalBox + "</div>");
                            sb.AppendLine("</div>");
                            sb.AppendLine("<div class='row mb-2'>");
                            sb.AppendLine("<div class='col-4 fw-semibold'>Total Part Number</div><div class='col-1 text-center'>:</div><div class='col-7'>" + totalPartNumber + "</div>");
                            sb.AppendLine("</div>");
                            sb.AppendLine("<div class='row mb-2'>");
                            sb.AppendLine("<div class='col-4 fw-semibold'>Total Qty@Box</div><div class='col-1 text-center'>:</div><div class='col-7'>" + totalQty + "</div>");
                            sb.AppendLine("</div>");
                            sb.AppendLine("</div>");

                            sb.Append("<table id='summary_table' class='table table-bordered table-striped'>");
                            sb.Append("<thead><tr>");
                            sb.Append("<th>Part Number</th>");
                            sb.Append("<th>Box Qty</th>");
                            sb.Append("<th>Qty@Box</th>");
                            sb.Append("</tr></thead><tbody>");

                            foreach (var item in summaryData)
                            {
                                sb.Append("<tr>");
                                sb.Append("<td>" + item.PartNumber + "</td>");
                                sb.Append("<td>" + item.BoxCount + "</td>");
                                sb.Append("<td>" + item.TotalQty + "</td>");
                                sb.Append("</tr>");
                            }

                            sb.Append("</tbody></table>");
                            sb.Append("</div>");


                            // ================= Column 2 =================
                            sb.Append("<div class='col-md-6'>");

                            sb.Append("<div class='d-flex flex-column mb-3'>");
                            sb.Append("<span class='my-auto fw-bold text-uppercase'>Carton Box</span>");
                            sb.Append("<span class='my-auto text-muted'>Scan Result</span>");
                            sb.Append("</div>");

                            sb.Append("<table id='detail_table' class='table table-bordered table-striped'>");
                            sb.Append("<thead><tr>");

                            foreach (DataColumn dc in dt.Tables[0].Columns)
                            {
                                if (dc.ColumnName.ToUpper() != "BOXID" && dc.ColumnName.ToUpper() != "PALLETID")
                                {
                                    sb.Append("<th>");
                                    sb.Append(dc.ColumnName);
                                    sb.Append("</th>");
                                }
                            }
                            sb.Append("</tr></thead><tbody>");

                            foreach (DataRow dr in dt.Tables[0].Rows)
                            {
                                sb.Append("<tr>");
                                foreach (DataColumn dc in dt.Tables[0].Columns)
                                {
                                    if (dc.ColumnName == "BoxNumber" || dc.ColumnName == "PartNumber")
                                    {
                                        sb.Append("<td>");
                                        sb.Append(dr[dc.ColumnName]);
                                        sb.Append("</td>");
                                    }
                                    else if (dc.ColumnName.ToUpper() != "BOXID" && dc.ColumnName.ToUpper() != "PALLETID")
                                    {
                                        sb.Append("<td>");
                                        sb.Append(dr[dc.ColumnName]);
                                        sb.Append("</td>");
                                    }
                                }
                                sb.Append("</tr>");
                            }

                            sb.Append("</tbody>");
                            sb.Append("</table>");

                            sb.Append("</div>");
                            sb.Append("</div>");

                            Response.Write(sb.ToString());
                        }
                    }
                }
            }
        }
    }
}
