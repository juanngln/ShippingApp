using System;
using System.Text;
using System.Web;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Data;
using System.Collections.Generic;

namespace ShippingAppPallet
{
    public partial class ShipmentPlanDetail : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string ConnectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["ShippingConnection"].ConnectionString;

            using (SqlConnection oConn = new SqlConnection(ConnectionString))
            {
                oConn.Open();

                using (SqlCommand SqlCmdDetail = new SqlCommand("udp_GetPlanDetailById", oConn))
                {
                    SqlCmdDetail.CommandType = CommandType.StoredProcedure;
                    SqlCmdDetail.Parameters.AddWithValue("@PlanId", Request.QueryString["ID"]);

                    using (SqlDataAdapter da = new SqlDataAdapter(SqlCmdDetail))
                    using (DataSet dt = new DataSet())
                    {
                        da.Fill(dt);

                        var sb = new StringBuilder();

                        string planNumber = "";
                        if (dt.Tables[1].Rows.Count > 0 && dt.Tables[1].Columns.Contains("ShipmentPlanID"))
                        {
                            planNumber = dt.Tables[1].Rows[0]["ShipmentPlanID"].ToString();
                        }

                        sb.Append("<div class='d-flex flex-column my-3'>");
                        sb.Append("<span class='text-muted'>Shipment Plan ID: " + planNumber)
                          .Append(HttpUtility.HtmlEncode(Request.QueryString["ShipmentPlanID"]))
                          .Append("</span>");
                        sb.Append("</div>");

                        sb.Append("<div class='row'>");
                        sb.Append("<div class='col-md-12'>");

                        var excludedColumns = new HashSet<string> { "ID", "ShipmentPlanID", "CreatedDate", "CreatedBy" };

                        sb.Append("<table id='detail_table' class='table table-bordered table-striped'>");
                        sb.Append("<thead><tr>");
                        foreach (DataColumn dc in dt.Tables[0].Columns)
                        {
                            if (!excludedColumns.Contains(dc.ColumnName))
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
                                if (!excludedColumns.Contains(dc.ColumnName))
                                {
                                    sb.Append("<td>");
                                    sb.Append(dr[dc.ColumnName]);
                                    sb.Append("</td>");
                                }
                            }
                            sb.Append("</tr>");
                        }
                        sb.Append("</tbody>");
                        sb.Append("</table");

                        sb.Append("</div>");
                        sb.Append("</div>");

                        Response.Write(sb.ToString());
                    }
                }
            }
        }
    }
}
