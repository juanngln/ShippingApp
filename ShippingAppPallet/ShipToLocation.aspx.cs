using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using ExcelDataReader;

namespace ShippingAppPallet
{
    public partial class ShipToLocation : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RenderTableFromDb();
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (!uploadExcel.HasFile)
                {
                    ShowAlert("Select your file", "danger");
                    return;
                }

                var ext = System.IO.Path.GetExtension(uploadExcel.FileName).ToLowerInvariant();
                if (ext != ".xls" && ext != ".xlsx")
                {
                    ShowAlert("The format must be .xls or .xlsx.", "danger");
                    return;
                }

                var dtExcel = ReadExcelToDataTable(uploadExcel.FileContent);
                if (dtExcel.Rows.Count == 0)
                {
                    ShowAlert("Invalid excel file (no valid data rows).", "warning");
                    return;
                }

                int inserted = BulkInsertToDb(dtExcel);

                RenderTableFromDb();

                ShowAlert($"Upload success {inserted} row(s) saved", "success");
            }
            catch (Exception ex)
            {
                ShowAlert($"An error occured {ex.Message}", "danger");
            }
        }

        private DataTable ReadExcelToDataTable(System.IO.Stream stream)
        {
            var dt = new DataTable("ProjectSiteImport");
            dt.Columns.Add("FlexProject", typeof(string));
            dt.Columns.Add("PartNumber", typeof(string));
            dt.Columns.Add("ProductFamily", typeof(string));
            dt.Columns.Add("SiteName", typeof(string));
            dt.Columns.Add("BPCode", typeof(string));
            dt.Columns.Add("Address", typeof(string));

            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                if (ds.Tables.Count == 0)
                    throw new InvalidOperationException("Worksheet tidak ditemukan di file Excel.");

                var sheet = ds.Tables[0];
                if (sheet.Rows.Count == 0)
                    throw new InvalidOperationException("Sheet kosong.");

                var mustHaveHeaders = new[]
                {
                    "BP Code",
                    "Ship to location /Site Name",
                    "Address details"
                };

                int headerRow = FindHeaderRowIndex(sheet, mustHaveHeaders);
                if (headerRow < 0)
                {
                    throw new InvalidOperationException(
                        "Header Excel tidak ditemukan. Pastikan file memiliki kolom: " +
                        "'BP Code', 'Ship to location /Site Name', dan 'Address details'."
                    );
                }

                var targetToHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "FlexProject",   "Flex Project" },
                    { "PartNumber",    "Part Number" },
                    { "ProductFamily", "Product Family" },
                    { "SiteName",      "Ship to location /Site Name" },
                    { "BPCode",        "BP Code" },
                    { "Address",       "Address details" }
                };

                var colMap = BuildColumnMap(sheet, headerRow, targetToHeader);

                for (int i = headerRow + 1; i < sheet.Rows.Count; i++)
                {
                    var row = sheet.Rows[i];

                    var bp = GetCellString(row, colMap, "BPCode");
                    if (string.IsNullOrWhiteSpace(bp))
                        continue;

                    var dr = dt.NewRow();
                    dr["FlexProject"] = GetCellString(row, colMap, "FlexProject");
                    dr["PartNumber"] = GetCellString(row, colMap, "PartNumber");
                    dr["ProductFamily"] = GetCellString(row, colMap, "ProductFamily");
                    dr["SiteName"] = GetCellString(row, colMap, "SiteName");
                    dr["BPCode"] = bp;
                    dr["Address"] = GetCellString(row, colMap, "Address");

                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        // ---- Helper ----
        private int FindHeaderRowIndex(DataTable sheet, string[] mustHaveHeaders)
        {
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                var rowCells = sheet.Rows[r].ItemArray
                    .Select(v => NormalizeHeader(v))
                    .ToList();

                bool allFound = mustHaveHeaders.All(h =>
                    rowCells.Any(c => string.Equals(c, NormalizeHeader(h), StringComparison.OrdinalIgnoreCase)));

                if (allFound)
                    return r;
            }
            return -1;
        }

        private Dictionary<string, int> BuildColumnMap(
            DataTable sheet,
            int headerRow,
            Dictionary<string, string> targetToHeader)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var headers = sheet.Rows[headerRow].ItemArray
                .Select((v, idx) => new { idx, name = NormalizeHeader(v) })
                .Where(x => !string.IsNullOrWhiteSpace(x.name))
                .ToList();

            foreach (var kvp in targetToHeader)
            {
                var targetName = kvp.Key;
                var headerName = NormalizeHeader(kvp.Value);

                var found = headers.FirstOrDefault(h =>
                    string.Equals(h.name, headerName, StringComparison.OrdinalIgnoreCase));

                if (found != null)
                {
                    map[targetName] = found.idx;
                }
                else
                {
                    bool isRequired =
                        targetName.Equals("BPCode", StringComparison.OrdinalIgnoreCase) ||
                        targetName.Equals("SiteName", StringComparison.OrdinalIgnoreCase) ||
                        targetName.Equals("Address", StringComparison.OrdinalIgnoreCase);

                    if (isRequired)
                        throw new InvalidOperationException($"Kolom header '{kvp.Value}' tidak ditemukan pada baris header.");

                    map[targetName] = -1;
                }
            }

            return map;
        }

        private string NormalizeHeader(object cell)
        {
            if (cell == null || cell == DBNull.Value) return null;
            var s = Convert.ToString(cell, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private string GetCellString(DataRow row, Dictionary<string, int> colMap, string targetKey)
        {
            int idx;
            if (!colMap.TryGetValue(targetKey, out idx) || idx < 0 || idx >= row.ItemArray.Length)
                return null;

            var v = row[idx];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim();
        }

        private static int ResolveColumn(DataTable table, params string[] candidates)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var name = table.Columns[i].ColumnName?.Trim();
                foreach (var c in candidates)
                {
                    if (name.Equals(c, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1;
        }

        private static string GetCell(DataRow row, int idx)
        {
            if (idx < 0) return null;
            var v = row[idx];
            return v == DBNull.Value ? null : Convert.ToString(v);
        }

        private int BulkInsertToDb(DataTable dt)
        {
            string connStr = ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;

            using (var con = new SqlConnection(connStr))
            {
                con.Open();
                using (var bulk = new SqlBulkCopy(con))
                {
                    bulk.DestinationTableName = "dbo.udt_shProjectSite";
                    bulk.ColumnMappings.Add("FlexProject", "FlexProject");
                    bulk.ColumnMappings.Add("PartNumber", "PartNumber");
                    bulk.ColumnMappings.Add("ProductFamily", "ProductFamily");
                    bulk.ColumnMappings.Add("SiteName", "SiteName");
                    bulk.ColumnMappings.Add("BPCode", "BPCode");
                    bulk.ColumnMappings.Add("Address", "Address");

                    bulk.WriteToServer(dt);
                }
            }
            return dt.Rows.Count;
        }

        private void RenderTableFromDb()
        {
            DataSet dt = this.GetData();

            StringBuilder html = new StringBuilder();
            html.Append("<table id='datatable-buttons' class='table table-hover table-bordered'>");
            html.Append("<thead><tr>");

            foreach (DataColumn column in dt.Tables[0].Columns)
            {
                if (column.ColumnName == "ProductFamily")
                    AppendTh(html, "PRODUCT FAMILY");
                else if (column.ColumnName == "SiteName")
                    AppendTh(html, "SITE NAME");
                else if (column.ColumnName == "FlexProject")
                    AppendTh(html, "PROJECT");
                else if (column.ColumnName == "PartNumber")
                    AppendTh(html, "PART NUMBER");
                else if (column.ColumnName == "BPCode")
                    AppendTh(html, "BP CODE");
                else if (column.ColumnName == "Address")
                    AppendTh(html, "ADDRESS DETAILS");
                else if (column.ColumnName != "ID")
                    AppendTh(html, column.ColumnName);
            }
            html.Append("</tr></thead><tbody>");

            foreach (DataRow row in dt.Tables[0].Rows)
            {
                html.Append("<tr>");
                foreach (DataColumn column in dt.Tables[0].Columns)
                {
                    if (column.ColumnName != "ID")
                    {
                        html.Append("<td>");
                        html.Append(row[column.ColumnName]);
                        html.Append("</td>");
                    }
                }
                html.Append("</tr>");
            }

            html.Append("</tbody></table>");

            PlaceHolder1.Controls.Clear();
            PlaceHolder1.Controls.Add(new Literal { Text = html.ToString() });
        }

        private void AppendTh(StringBuilder sb, string text)
        {
            sb.Append("<th>");
            sb.Append(text);
            sb.Append("</th>");
        }

        private DataSet GetData()
        {
            string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("GetProjectSiteList", con))
            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (DataSet dt = new DataSet())
                {
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        private void ShowAlert(string message, string type)
        {
            phAlert.Controls.Clear();
            var div = new System.Web.UI.HtmlControls.HtmlGenericControl("div");
            div.Attributes["class"] = $"alert alert-{type}";
            div.InnerText = message;
            phAlert.Controls.Add(div);
        }
    }
}
