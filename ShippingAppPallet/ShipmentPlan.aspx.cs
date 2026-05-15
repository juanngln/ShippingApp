using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Web.Script.Serialization;

namespace ShippingAppPallet
{
    public class ShipmentPlanLineDto
    {
        public string shipmentPlanNumber { get; set; }
        public string baanPN { get; set; }
        public string cpn { get; set; }
        public string po { get; set; }
        public string so { get; set; }
        public int qty { get; set; }
        public int currentShipmentDetailQuantity { get; set; }
        public int currentPallet { get; set; }
    }

    public partial class ShipmentPlan : System.Web.UI.Page
    {
        private const string ShipmentPlanApiUrl = "http://btmnt012/ShipmentPlan/api/DataShipmentPlan";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadShipmentPlanTableAsync();
            }
        }

        private async void LoadShipmentPlanTableAsync()
        {
            try
            {
                var lines = await FetchShipmentPlanLinesAsync();
                var ds = BuildGroupedShipmentPlanDataSet(lines);
                var htmlTable = BuildShipmentPlanGroupTableHtml(ds);
                listShipmentPlanTable.Text = htmlTable;
            }
            catch (Exception ex)
            {
                listShipmentPlanTable.Text = "Failed to load Shipment Plan: " + HttpUtility.HtmlEncode(ex.Message);
            }
        }

        private async Task<List<ShipmentPlanLineDto>> FetchShipmentPlanLinesAsync()
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = true,
                PreAuthenticate = true
            };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.GetAsync(ShipmentPlanApiUrl);
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"401 Unauthorized (Windows Auth). Check IIS configuration. Response: {body}");
                }

                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<List<ShipmentPlanLineDto>>(json);
                return data ?? new List<ShipmentPlanLineDto>();
            }
        }

        private DataSet BuildGroupedShipmentPlanDataSet(List<ShipmentPlanLineDto> lines)
        {
            var table = new DataTable("ShipmentPlanGroups");
            table.Columns.Add("ShipmentPlanNumber", typeof(string));
            table.Columns.Add("TotalQty", typeof(int));
            table.Columns.Add("TotalSO", typeof(int));
            table.Columns.Add("DetailCount", typeof(int));
            table.Columns.Add("Action", typeof(string));

            var groups = lines.GroupBy(x => x.shipmentPlanNumber);
            foreach (var g in groups)
            {
                int totalQty = g.Sum(x => x.qty);
                int totalSO = g.Select(x => x.so).Distinct().Count();
                int detailCount = g.Count();
                table.Rows.Add(g.Key, totalQty, totalSO, detailCount, "");
            }

            var ds = new DataSet();
            ds.Tables.Add(table);
            return ds;
        }

        private string BuildShipmentPlanGroupTableHtml(DataSet ds)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return "<p>No shipment plan data available.</p>";

            var dt = ds.Tables[0];
            var html = new StringBuilder();

            html.Append("<table id='datatable-buttons' class='table table-hover table-bordered'>");
            html.Append("<thead><tr>");
            html.Append("<th>SHIP ID</th>");
            html.Append("<th>TOTAL QTY</th>");
            html.Append("<th>TOTAL SO</th>");
            html.Append("<th>LINES</th>");
            html.Append("<th>ACTION</th>");
            html.Append("</tr></thead>");
            html.Append("<tbody>");

            foreach (DataRow row in dt.Rows)
            {
                string shipId = row["ShipmentPlanNumber"]?.ToString() ?? "";
                int totalQty = SafeInt(row["TotalQty"]);
                int totalSO = SafeInt(row["TotalSO"]);
                int detailCount = SafeInt(row["DetailCount"]);

                html.Append("<tr>");
                // SHIP ID
                html.Append("<td>").Append(HttpUtility.HtmlEncode(shipId)).Append("</td>");
                // TOTAL QTY
                html.Append("<td class='text-left'>").Append(totalQty.ToString("N0")).Append("</td>");
                // TOTAL SO
                html.Append("<td class='text-left'>").Append(totalSO.ToString("N0")).Append("</td>");
                // LINES
                html.Append("<td class='text-left'>").Append(detailCount.ToString("N0")).Append("</td>");
                // ACTION
                html.Append("<td>");
                html.Append("<a href='#detail-modal' onclick=\"DetailPlan('" + shipId + "');\" type='button' class='btn btn-light btn-sm waves-effect waves-light' data-animation='fadein' data-bs-toggle='modal' data-bs-target='#detail-modal' data-overlayspeed='200' data-overlaycolor='#36404a' Title='View Detail'>");
                html.Append("<i class='fa fa-list-alt'></i>");
                html.Append("</a>");
                html.Append("</td>");
                html.Append("</tr>");
            }

            html.Append("</tbody></table>");
            return html.ToString();
        }

        private int SafeInt(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            int n; return int.TryParse(o.ToString(), out n) ? n : 0;
        }
    }
}
