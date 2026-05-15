using DocumentFormat.OpenXml.Math;
using ShippingAppPallet.Class;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ShippingAppPallet
{
    public partial class ListShipmentPlan : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string userRole = Session["Role"] != null ? Session["Role"].ToString() : "";

                if (userRole.Equals("Logistic", StringComparison.OrdinalIgnoreCase))
                {
                    cardHeader.Visible = false;
                }
                else if (userRole.Equals("PA", StringComparison.OrdinalIgnoreCase))
                {
                    cardHeader.Visible = true;
                }
            }

            DataSet dt = this.GetData();
            //DataSet dt = null;
            string iid = string.Empty;
            //Building an HTML string.
            StringBuilder html = new StringBuilder();

            //Table start.
            html.Append("<table id='datatable-buttons' class='table table-hover table-bordered'>");

            //Building the Header row.
            html.Append("<thead>");
            html.Append("<tr>");
            dt.Tables[0].Columns.Add("ACTION");
            var excludedColumns = new HashSet<string> { "ID", "CreatedDate", "CreatedBy" };

            foreach (DataColumn column in dt.Tables[0].Columns)
            {
                if (!excludedColumns.Contains(column.ColumnName))
                {
                    if (column.ColumnName == "ShipmentPlanID")
                    {
                        html.Append("<th>");
                        html.Append("PLAN ID");
                        html.Append("</th>");
                    }
                    else if (column.ColumnName == "Customer")
                    {
                        html.Append("<th>");
                        html.Append("CUSTOMER");
                        html.Append("</th>");
                    }
                    else if (column.ColumnName == "Status")
                    {
                        html.Append("<th>");
                        html.Append("STATUS");
                        html.Append("</th>");
                    }
                    else if (column.ColumnName == "ShipmentDate")
                    {
                        html.Append("<th>");
                        html.Append("SHIPMENT DATE");
                        html.Append("</th>");
                    }
                    else if (column.ColumnName == "PIC")
                    {
                        html.Append("<th>");
                        html.Append("PIC");
                        html.Append("</th>");
                    }
                    else
                    {
                        html.Append("<th>");
                        html.Append(column.ColumnName);
                        html.Append("</th>");
                    }
                }
            }
            html.Append("</tr>");
            html.Append("</thead>");

            //Building the Data rows.
            html.Append("<tbody>");
            foreach (DataRow row in dt.Tables[0].Rows)
            {
                html.Append("<tr>");
                foreach (DataColumn column in dt.Tables[0].Columns)
                {
                    if (!excludedColumns.Contains(column.ColumnName))
                    {
                        if (column.ColumnName == "ACTION")
                        {
                            iid = row["ID"].ToString();
                            html.Append("<td>");
                            html.Append($"<a href=\"#\" onclick=\"DetailPlan('{iid}')\" class=\"btn btn-sm btn-light\"><i class=\"fa-solid fa-list\"></i></a>");
                            html.Append("</td>");
                        }
                        else if (column.ColumnName == "ShipmentDate")
                        {
                            html.Append("<td>");
                            if (row[column.ColumnName] != DBNull.Value)
                            {
                                html.Append(Convert.ToDateTime(row[column.ColumnName]).ToString("dd-MMM-yyyy"));
                            }
                            html.Append("</td>");
                        }
                        else
                        {
                            html.Append("<td>");
                            html.Append(row[column.ColumnName]);
                            html.Append("</td>");
                        }
                    }
                }
                html.Append("</tr>");
            }
            html.Append("</tbody>");

            //Table end.
            html.Append("</table>");

            //Append the HTML string to Placeholder.
            PlaceHolder1.Controls.Add(new System.Web.UI.WebControls.Literal { Text = html.ToString() });
        }

        private DataSet GetData()
        {
            string email = User.Identity.Name;
            String connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("udp_GetShipmentPlanList"))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter sda = new SqlDataAdapter())
                    {
                        cmd.Connection = con;

                        sda.SelectCommand = cmd;
                        using (DataSet dt = new DataSet())
                        {
                            sda.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
        }

        // --- EXPORT LOD TEMPLATE ---
        protected void btnExportLOD_Click(object sender, EventArgs e)
        {
            // Query untuk mendapatkan data Shipment Plan hari ini 
            // digabung dengan urutan pallet (Load ID) dari tabel udt_shPallet
            string query = @"
                WITH PalletToday AS (
                    -- Mencari urutan pallet hari ini berdasarkan waktu pembuatan (CreatedDate)
                    SELECT 
                        ID, 
                        DENSE_RANK() OVER (ORDER BY CreatedDate, ID) as SeqNo
                    FROM udt_shPallet
                    WHERE CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE)
                )
                SELECT 
                    d.BP, 
                    'HUB ' + CAST(pt.SeqNo AS NVARCHAR) as GeneratedLoadID,
                    d.Sequence, 
                    d.SO, 
                    d.Item, 
                    d.Qty, 
                    o.Position AS POLine,
                    o.Status AS RSD 
                FROM udt_shShipmentPlanDetail d
                LEFT JOIN udt_OpenSO o ON d.SO = o.[Sales Order]
                -- Menghubungkan ke PalletDetail untuk mencari kecocokan item yang sudah dipaketkan ke pallet
                LEFT JOIN udt_shPalletDetail pd ON d.Item = pd.PartNumber AND d.Qty = pd.Qty
                LEFT JOIN PalletToday pt ON pd.PalletID = pt.ID
                WHERE CAST(d.CreatedDate AS DATE) = CAST(GETDATE() AS DATE)";

            DataTable dtLOD = new DataTable();

            String connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dtLOD);
                    }
                }
            }

            if (dtLOD.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "showalert", "alert('Tidak ada data Shipment Plan untuk hari ini.');", true);
                return;
            }

            StringBuilder sb = new StringBuilder();

            // 1. Generate Header (33 Kolom sesuai Template LOD Jan'26)
            string header = "Customer|Load ID|Carrier|BL/AWB|BL/AWB Date 1|Mode|Service Type|Shipping Instructions|Container Type|Routing|Container|Seal#|Est.Departure Date|BL Origin|POL|POD|BL Dest|FinalDestination|Vessel|Voyage|LoadSeq|PO|PO Line|Item#|Qty|Cartons|Volume|Weight|RSD|ROD|CM|Docs|P/S";
            sb.AppendLine(header);

            // 2. Generate Data Rows
            foreach (DataRow row in dtLOD.Rows)
            {
                string[] fields = new string[33];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = string.Empty;
                }

                // 3. Mapping Data sesuai Ketentuan Baru
                fields[0] = row["BP"].ToString();               // Customer (BP Code)
                fields[1] = row["GeneratedLoadID"].ToString();  // Load ID: "HUB (Urutan Pallet)"field
                fields[2] = "TRU";
                fields[5] = "1";
                fields[20] = "1";
                fields[21] = row["SO"].ToString();              // PO (SO)
                fields[22] = row["POLine"].ToString();
                fields[23] = row["Item"].ToString();            // Item# (Item)
                fields[24] = row["Qty"].ToString();             // Qty
                fields[25] = "1";                               // Cartons (Default 1)
                fields[26] = "0";                               // Cartons (Default 1)
                fields[28] = row["RSD"].ToString();             // RSD (Status dari udt_OpenSO)
                fields[32] = "S";

                // Gabungkan baris dengan delimiter pipa (|)
                sb.AppendLine(string.Join("|", fields));
            }

            // 4. Proses Download File CSV (Pipe-Delimited)
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", $"attachment;filename=Template LOD {DateTime.Now:yyyyMMdd}.csv");
            Response.Charset = "";
            Response.ContentType = "text/csv";
            Response.Output.Write(sb.ToString());
            Response.Flush();
            Response.End();
        }
    }
}
