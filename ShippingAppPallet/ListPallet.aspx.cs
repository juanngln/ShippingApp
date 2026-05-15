using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace ShippingAppPallet
{
    public partial class ListPallet : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtFilterDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                GenerateTable();
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            GenerateTable();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtFilterDate.Text = string.Empty;
            ddlFilterTrip.SelectedIndex = 0;
            GenerateTable();
        }

        private IEnumerable<DataRow> GetFilteredData()
        {
            DataSet dt = this.GetData();
            DataTable dataTable = dt.Tables[0];

            string selectedDate = txtFilterDate.Text;
            string selectedTrip = ddlFilterTrip.SelectedValue;

            IEnumerable<DataRow> query = dataTable.AsEnumerable();

            if (!string.IsNullOrEmpty(selectedDate) && DateTime.TryParse(selectedDate, out DateTime filterDate))
            {
                query = query.Where(row =>
                {
                    var cellValue = row["CreatedDate"];
                    if (cellValue != DBNull.Value && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
                        return dateValue.Date == filterDate.Date;
                    return false;
                });
            }

            if (selectedTrip == "1") // Pagi
            {
                query = query.Where(row =>
                {
                    var cellValue = row["CreatedDate"];
                    if (cellValue != DBNull.Value && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
                        return dateValue.Hour < 12;
                    return false;
                });
            }
            else if (selectedTrip == "2") // Siang
            {
                query = query.Where(row =>
                {
                    var cellValue = row["CreatedDate"];
                    if (cellValue != DBNull.Value && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
                        return dateValue.Hour >= 12;
                    return false;
                });
            }

            return query;
        }

        private void GenerateTable()
        {
            DataTable dataTable = this.GetData().Tables[0];
            var query = GetFilteredData();

            DataTable filteredTable = query.Any() ? query.CopyToDataTable() : dataTable.Clone();

            PlaceHolder1.Controls.Clear();
            string iid = string.Empty;
            StringBuilder html = new StringBuilder();

            html.Append("<table id='datatable-buttons' class='table table-hover table-bordered'>");
            html.Append("<thead><tr>");
            filteredTable.Columns.Add("Action");

            foreach (DataColumn column in filteredTable.Columns)
            {
                if (column.ColumnName == "PalletNumber") html.Append("<th>PALLET NUMBER</th>");
                else if (column.ColumnName == "CreatedDate") html.Append("<th>CREATED DATE</th>");
                else if (column.ColumnName == "CreatedBy") html.Append("<th>CREATED BY</th>");
                else if (column.ColumnName != "ID") html.Append("<th>" + column.ColumnName + "</th>");
            }
            html.Append("</tr></thead><tbody>");
            foreach (DataRow row in filteredTable.Rows)
            {
                iid = row["ID"].ToString();
                html.Append("<tr>");
                foreach (DataColumn column in filteredTable.Columns)
                {
                    if (column.ColumnName != "ID")
                    {
                        if (column.ColumnName == "Action")
                        {
                            html.Append("<td>");
                            html.Append("<a href='#detail-modal' onclick=\"DetailPallet('" + iid + "');\" type='button' class='btn btn-light btn-sm waves-effect waves-light' data-animation='fadein' data-bs-toggle='modal' data-bs-target='#detail-modal' data-overlayspeed='200' data-overlaycolor='#36404a' Title='View Detail'><i class='fa fa-list-alt'></i></a>");
                            html.Append("<a href='AddPallet.aspx?id=" + iid + "' class='btn btn-warning btn-sm waves-effect waves-light ms-2' title='Edit Pallet'><i class='fa fa-edit'></i></a>");
                            html.Append("<a href='GenerateQrCode.aspx?Id=" + iid + "' class='btn btn-primary btn-sm waves-effect waves-light ms-2' title='Generate QR Code'><i class='fa fa-qrcode'></i></a>");
                            html.Append("</td>");
                        }
                        else
                        {
                            html.Append("<td>" + row[column.ColumnName] + "</td>");
                        }
                    }
                }
                html.Append("</tr>");
            }
            html.Append("</tbody></table>");
            PlaceHolder1.Controls.Add(new Literal { Text = html.ToString() });
        }

        // --- EVENT HANDLER EXPORT EXCEL (.XLSX) ---
        protected void btnExport_Click(object sender, EventArgs e)
        {
            var filteredRows = GetFilteredData().ToList();
            if (filteredRows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('No data to export.');", true);
                return;
            }

            List<string> palletIds = new List<string>();
            foreach (var row in filteredRows)
            {
                palletIds.Add(row["ID"].ToString());
            }

            DataTable detailTable = new DataTable();
            string idsStr = string.Join(",", palletIds);

            string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                // UPDATE: Menambahkan LEFT JOIN ke udt_OpenSO untuk mengambil Customer order dan Reference A
                string sql = $@"
                    SELECT 
                        p.PalletNumber,
                        p.CreatedDate,
                        pd.CartonBoxId,
                        pd.Qty AS BoxQty,
                        a.Item,
                        a.[Sales Order],
                        d.Position,
                        d.Sequence,
                        oso.[Customer order] AS CustomerPO,
                        oso.[Reference A] AS RefA
                    FROM udt_shPallet p
                    JOIN udt_shPalletDetail pd ON p.ID = pd.PalletID
                    LEFT JOIN udt_CartonBoxAllocation a ON pd.CartonBoxId = a.CartonBoxId
                    LEFT JOIN udt_shShipmentPlan sp ON sp.ShipmentPlanID = a.ShipmentPlanId
                    LEFT JOIN udt_shShipmentPlanDetail d ON d.ShipmentPlanID = sp.ID AND d.Item = a.Item AND d.SO = a.[Sales Order]
                    LEFT JOIN udt_OpenSO oso ON oso.[Sales Order] = a.[Sales Order] AND oso.Item = a.Item
                    WHERE p.ID IN ({idsStr})
                    ORDER BY p.PalletNumber, a.Item, a.[Sales Order]";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    sda.Fill(detailTable);
                }
            }

            DateTime deliveryDateDt = detailTable.Rows.Count > 0 && detailTable.Rows[0]["CreatedDate"] != DBNull.Value
                                    ? Convert.ToDateTime(detailTable.Rows[0]["CreatedDate"])
                                    : DateTime.Now;

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Style.Font.FontSize = 9;

                // Mencegah error jika worksheet "LOD Export" tidak diset ke objek yang benar
                var ws = wb.Worksheets.Add("LOD Export");
                // Catatan: Baris "var ws = wb.Worksheets.Add("LOD Export");" sebelumnya salah posisi di kode Anda.
                // ws di-declare dua kali, pastikan hanya satu kali.

                // (Tidak perlu mendeklarasikan ulang var ws di sini, gunakan yang sudah di-assign)

                ws.Cell(1, 1).Value = "List Material to be Deliver To SEMB";
                ws.Range("A1:E1").Merge();

                ws.Cell(3, 1).Value = "Delivery Date";
                ws.Range("A3:B3").Merge();
                ws.Cell(3, 3).Value = deliveryDateDt;
                ws.Cell(3, 3).Style.DateFormat.Format = "d-mmmm-yy";
                ws.Cell(3, 4).Value = "Delivered To";

                ws.Cell(4, 1).Value = "Truck / Trailer No.";
                ws.Range("A4:B4").Merge();
                ws.Cell(4, 4).Value = "Forwarder";

                ws.Cell(5, 1).Value = "Pre-alert Number";
                ws.Range("A5:B5").Merge();

                ws.Range("A1:E1").Style.Font.Bold = true;
                ws.Range("A3:E5").Style.Font.Bold = true; // Update: Sampai E5

                ws.Range("A1:E1").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                var infoRange = ws.Range("A3:E5");
                infoRange.Style.Border.OutsideBorder = XLBorderStyleValues.Dashed; // Update: Menggunakan Dashed sesuai konvensi Excel putus-putus standar
                infoRange.Style.Border.InsideBorder = XLBorderStyleValues.Dashed;

                string[] headers = {
                    "No", "Shipment ID", "Customer Part Number", "Customer PO", "Line",
                    "Unit", "Qty @ Box", "No. of Box", "Total Qty", "Palette ID",
                    "Total Pallet", "Item", "Remark", "LOD", "Sales Order",
                    "Position", "Seq", "Ref A/Hub Po", "Invoice"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(7, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    //cell.Style.Fill.BackgroundColor = XLColor.LightGray; // Menghapus warna header jika ingin polos
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int currentRow = 8;

                var groupedByPallet = detailTable.AsEnumerable().GroupBy(r => r["PalletNumber"].ToString());

                foreach (var pGrp in groupedByPallet)
                {
                    string palletNumber = pGrp.Key;
                    var groupedByItem = pGrp.GroupBy(r => r["Item"] != DBNull.Value ? r["Item"].ToString() : "");

                    foreach (var iGrp in groupedByItem)
                    {
                        bool isFirstItemRow = true;
                        int startItemRow = currentRow;
                        string itemCode = iGrp.Key;

                        string custPartNum = itemCode;
                        int dashIndex = itemCode.IndexOf('-');
                        if (dashIndex >= 0 && itemCode.StartsWith("SEA", StringComparison.OrdinalIgnoreCase))
                            custPartNum = itemCode.Substring(dashIndex + 1);

                        int totalNoOfBoxForItem = iGrp.Count();
                        string qtyAtBoxForItem = iGrp.First()["BoxQty"] != DBNull.Value ? iGrp.First()["BoxQty"].ToString() : "0";

                        var groupedBySO = iGrp.GroupBy(r => r["Sales Order"] != DBNull.Value ? r["Sales Order"].ToString() : "");
                        foreach (var soGrp in groupedBySO)
                        {
                            string so = soGrp.Key;
                            int noOfBoxForSO = soGrp.Count();
                            int qtyAtBoxForSO = soGrp.First()["BoxQty"] != DBNull.Value ? Convert.ToInt32(soGrp.First()["BoxQty"]) : 0;

                            int totalQtyForSO = noOfBoxForSO * qtyAtBoxForSO;

                            string pos = soGrp.First()["Position"] != DBNull.Value ? soGrp.First()["Position"].ToString() : "";
                            string seq = soGrp.First()["Sequence"] != DBNull.Value ? soGrp.First()["Sequence"].ToString() : "";

                            // UPDATE: Menarik data Customer PO dan Ref A dari tabel
                            string customerPO = soGrp.First()["CustomerPO"] != DBNull.Value ? soGrp.First()["CustomerPO"].ToString() : "";
                            string refA = soGrp.First()["RefA"] != DBNull.Value ? soGrp.First()["RefA"].ToString() : "";

                            ws.Cell(currentRow, 1).Value = "";
                            ws.Cell(currentRow, 2).Value = "";
                            ws.Cell(currentRow, 3).Value = custPartNum;
                            ws.Cell(currentRow, 4).Value = customerPO; // UPDATE: Assign Customer PO
                            ws.Cell(currentRow, 5).Value = "";
                            ws.Cell(currentRow, 6).Value = "PC";

                            if (isFirstItemRow && int.TryParse(qtyAtBoxForItem, out int qtyBox))
                                ws.Cell(currentRow, 7).Value = qtyBox;
                            else
                                ws.Cell(currentRow, 7).Value = "";

                            if (isFirstItemRow)
                                ws.Cell(currentRow, 8).Value = totalNoOfBoxForItem;
                            else
                                ws.Cell(currentRow, 8).Value = "";

                            ws.Cell(currentRow, 9).Value = totalQtyForSO;
                            ws.Cell(currentRow, 10).Value = palletNumber;

                            if (isFirstItemRow)
                                ws.Cell(currentRow, 11).Value = 1;
                            else
                                ws.Cell(currentRow, 11).Value = "";

                            ws.Cell(currentRow, 12).Value = itemCode;
                            ws.Cell(currentRow, 13).Value = "";
                            ws.Cell(currentRow, 14).Value = "";
                            ws.Cell(currentRow, 15).Value = so;

                            if (int.TryParse(pos, out int posNum))
                                ws.Cell(currentRow, 16).Value = posNum;
                            else
                                ws.Cell(currentRow, 16).Value = pos;

                            if (int.TryParse(seq, out int seqNum))
                                ws.Cell(currentRow, 17).Value = seqNum;
                            else
                                ws.Cell(currentRow, 17).Value = seq;

                            ws.Cell(currentRow, 18).Value = refA; // UPDATE: Assign Ref A
                            ws.Cell(currentRow, 19).Value = "";

                            currentRow++;
                            isFirstItemRow = false;
                        }

                        if (currentRow - 1 > startItemRow)
                        {
                            var mergeRange = ws.Range(startItemRow, 11, currentRow - 1, 11);
                            mergeRange.Merge();
                            mergeRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                    }
                }

                if (currentRow > 8)
                {
                    var tableRange = ws.Range(7, 1, currentRow - 1, 19);
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    ws.Range(8, 5, currentRow - 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range(8, 11, currentRow - 1, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range(8, 16, currentRow - 1, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                ws.Columns().AdjustToContents();

                ws.Column(1).Width = 4.5;
                ws.Column(5).Width = 12;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment;filename=PalletExport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
        }

        private DataSet GetData()
        {
            String connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            string email = User.Identity.Name;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("udp_GetPalletList"))
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
    }
}
