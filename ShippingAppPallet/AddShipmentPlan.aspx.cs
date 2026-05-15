using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;

namespace ShippingAppPallet
{
    [ScriptService]
    public partial class AddShipmentPlan : Page
    {
        private static readonly string connStr = ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                dateShip.Value = DateTime.Now.ToShortDateString();
            }
        }

        // --- DTO CLASSES ---
        public class InventorySummaryDto
        {
            public string bp { get; set; }
            public string item { get; set; }
            public string family { get; set; }
            public int stockQty { get; set; }
            public string demandQty { get; set; }
        }

        public class CartonBoxDto
        {
            public string box { get; set; }
            public int qty { get; set; }
            public string display { get; set; }
        }

        public class SalesOrderDto
        {
            public string so { get; set; }
            public string position { get; set; }
            public string sequence { get; set; }
            public int qty { get; set; }
            public string salesPrice { get; set; }
            public string status { get; set; }
            public string display { get; set; }
            public bool selectable { get; set; }
        }

        public class AllocationResult
        {
            public bool ok { get; set; }
            public string message { get; set; }
        }

        // --- EXISTING WEB METHODS ---
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GenerateShipmentPlanId()
        {
            string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(@"
                    SELECT
                    CAST(ISNULL(IDENT_CURRENT('udt_shShipmentPlan'), 0) AS INT) +
                    CAST(ISNULL(IDENT_INCR('udt_shShipmentPlan'), 1) AS INT) AS NextId;", conn))
            {
                conn.Open();
                var obj = cmd.ExecuteScalar();
                int nextId = Convert.ToInt32(obj);
                return "TEST-SHP" + DateTime.Now.ToString("yyMMdd") + $"{nextId:D3}";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<string> GetProductFamilies()
        {
            var list = new List<string>();
            string query = "SELECT DISTINCT ProductFamily FROM udt_shProjectSite WHERE ProductFamily IS NOT NULL ORDER BY ProductFamily";
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader()) { while (dr.Read()) list.Add(dr["ProductFamily"].ToString()); }
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<string> GetBPCode(List<string> families)
        {
            var list = new List<string>();
            string query = @"
                SELECT DISTINCT so.BP
                FROM udt_OpenSO so
                INNER JOIN udt_shProjectSite ps ON so.Item = ps.FlexProject
                WHERE so.BP IS NOT NULL ";
            if (families != null && families.Count > 0)
            {
                var paramsList = new List<string>();
                for (int i = 0; i < families.Count; i++) paramsList.Add($"@f{i}");
                query += $" AND ps.ProductFamily IN ({string.Join(",", paramsList)})";
            }
            query += " ORDER BY so.BP";

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                if (families != null)
                    for (int i = 0; i < families.Count; i++) cmd.Parameters.AddWithValue($"@f{i}", families[i]);
                conn.Open();
                using (var dr = cmd.ExecuteReader()) { while (dr.Read()) list.Add(dr["BP"].ToString()); }
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<InventorySummaryDto> GetInventorySummary(List<string> families, List<string> bpCodes)
        {
            var list = new List<InventorySummaryDto>();
            string query = @"
    SELECT
        ps.ProductFamily, d.BP, s.Item, s.StockQty, d.DemandQty
    FROM
        -- UPDATE: Menghitung stok aktif tanpa yang sudah teralokasi (filter via udt_CartonBoxAllocation)
        (SELECT p.Item, ISNULL(SUM(p.QTY), 0) as StockQty 
         FROM udt_shPicklistDetail p
         LEFT JOIN udt_CartonBoxAllocation a ON p.PartNumber = a.CartonBoxId AND p.Item = a.Item
         WHERE p.Status = 'On Rack' AND a.ID IS NULL
         GROUP BY p.Item) s
    INNER JOIN
        -- UPDATE: Ambil real Balance dari SoBalance jika sudah dialokasikan sebagian, atau Qty Ordered jika belum
        (SELECT o.BP, o.Item, SUM(CONVERT(FLOAT, ISNULL(b.Balance, o.[Qty Ordered]))) as DemandQty 
         FROM udt_OpenSO o
         LEFT JOIN udt_SoBalance b 
            ON o.[Sales Order] = b.[Sales Order] 
            AND o.Position = b.Position 
            AND o.Sequence = b.Sequence
            AND o.Item = b.Item
         GROUP BY o.BP, o.Item) d ON s.Item = d.Item
    INNER JOIN udt_shProjectSite ps ON s.Item = ps.FlexProject
    WHERE 1=1 ";

            if (families != null && families.Count > 0)
            {
                var fParams = new List<string>();
                for (int i = 0; i < families.Count; i++) fParams.Add($"@fam{i}");
                query += $" AND ps.ProductFamily IN ({string.Join(",", fParams)})";
            }
            if (bpCodes != null && bpCodes.Count > 0)
            {
                var bParams = new List<string>();
                for (int i = 0; i < bpCodes.Count; i++) bParams.Add($"@bp{i}");
                query += $" AND d.BP IN ({string.Join(",", bParams)})";
            }

            query += " ORDER BY d.BP, s.Item";
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                if (families != null)
                    for (int i = 0; i < families.Count; i++) cmd.Parameters.AddWithValue($"@fam{i}", families[i]);
                if (bpCodes != null)
                    for (int i = 0; i < bpCodes.Count; i++) cmd.Parameters.AddWithValue($"@bp{i}", bpCodes[i]);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new InventorySummaryDto
                        {
                            bp = dr["BP"].ToString(),
                            item = dr["Item"].ToString(),
                            family = dr["ProductFamily"].ToString(),
                            stockQty = Convert.ToInt32(dr["StockQty"]),
                            demandQty = dr["DemandQty"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CartonBoxDto> GetCartonList(string item)
        {
            var list = new List<CartonBoxDto>();
            string query = @"
                SELECT p.PartNumber, p.QTY
                FROM udt_shPicklistDetail p
                LEFT JOIN udt_CartonBoxAllocation a ON p.PartNumber = a.CartonBoxId AND p.Item = a.Item
                WHERE p.Item = @item 
                  AND p.Status = 'On Rack'
                  AND a.ID IS NULL -- Filter out allocated boxes
                ORDER BY p.ShippingReceivedDt ASC, p.PartNumber";

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@item", item);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new CartonBoxDto
                        {
                            box = dr["PartNumber"].ToString(),
                            qty = Convert.ToInt32(dr["QTY"]),
                            display = dr["PartNumber"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<SalesOrderDto> GetSoList(string item, int totalQty)
        {
            var list = new List<SalesOrderDto>();
            string query = @"
                SELECT 
                    o.[Sales Order], o.Position, o.Sequence, o.[Sales Price], o.Status,
                    CASE 
                        WHEN b.ID IS NOT NULL THEN b.Balance
                        ELSE o.[Qty Ordered]
                    END as EffectiveQty
                FROM udt_OpenSO o
                LEFT JOIN udt_SoBalance b 
                    ON o.[Sales Order] = b.[Sales Order] 
                    AND o.Position = b.Position 
                    AND o.Sequence = b.Sequence
                    AND o.Item = b.Item
                WHERE o.Item = @item
                ORDER BY o.[Rate Date] ASC, o.Status DESC";

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@item", item);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        double effQty = Convert.ToDouble(dr["EffectiveQty"]);
                        if (effQty <= 0) continue; // Skip fully allocated SOs

                        var statusVal = dr["Status"].ToString();
                        list.Add(new SalesOrderDto
                        {
                            so = dr["Sales Order"].ToString(),
                            position = dr["Position"].ToString(),
                            sequence = dr["Sequence"].ToString(),
                            qty = (int)effQty,
                            salesPrice = dr["Sales Price"] != DBNull.Value ? dr["Sales Price"].ToString() : "0",
                            status = statusVal,
                            display = dr["Sales Order"].ToString(),
                            selectable = IsSelectableStatus(statusVal)
                        });
                    }
                }
            }

            var enabled = new List<SalesOrderDto>();
            var disabled = new List<SalesOrderDto>();
            foreach (var so in list)
            {
                if (so.selectable) enabled.Add(so);
                else disabled.Add(so);
            }
            enabled.AddRange(disabled);
            return enabled;
        }

        private static readonly HashSet<string> AllowedStatuses = new HashSet<string>(
            new[] { "Generate Logical Trucks", "Awaiting delivery." },
            StringComparer.OrdinalIgnoreCase
        );

        private static bool IsSelectableStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return AllowedStatuses.Contains(status.Trim());
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static AllocationResult BatchAllocate(string item, string planId, List<CartonBoxDto> boxes, List<SalesOrderDto> sos)
        {
            if (boxes == null || boxes.Count == 0 || sos == null || sos.Count == 0)
            {
                return new AllocationResult { ok = false, message = "Data tidak lengkap." };
            }

            sos = sos.OrderBy(x => x.position).ThenBy(x => x.sequence).ToList();

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var boxQueue = new Queue<CartonBoxDto>(boxes);

                        foreach (var so in sos)
                        {
                            if (!IsSelectableStatus(so.status)) continue;

                            int qtyNeeded = so.qty;
                            int qtyAllocatedToThisSo = 0;

                            while (qtyAllocatedToThisSo < qtyNeeded && boxQueue.Count > 0)
                            {
                                var nextBox = boxQueue.Peek();
                                int spaceRemaining = qtyNeeded - qtyAllocatedToThisSo;

                                if (nextBox.qty <= spaceRemaining)
                                {
                                    var boxToUse = boxQueue.Dequeue();
                                    qtyAllocatedToThisSo += boxToUse.qty;

                                    string sqlAlloc = @"
                                        INSERT INTO udt_CartonBoxAllocation (Item, CartonBoxId, QTY, Status, [Sales Order], ShipmentPlanId)
                                        VALUES (@item, @boxId, @boxQty, 'Allocated', @soNum, @planId);";

                                    using (var cmd = new SqlCommand(sqlAlloc, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@item", item);
                                        cmd.Parameters.AddWithValue("@boxId", boxToUse.box);
                                        cmd.Parameters.AddWithValue("@boxQty", boxToUse.qty);
                                        cmd.Parameters.AddWithValue("@soNum", so.so);
                                        cmd.Parameters.AddWithValue("@planId", planId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            int newBalance = qtyNeeded - qtyAllocatedToThisSo;

                            string sqlBalance = @"
                                MERGE udt_SoBalance AS target
                                USING (SELECT @so as so, @pos as pos, @seq as seq, @item as item) AS source
                                ON (target.[Sales Order] = source.so AND target.Position = source.pos AND target.Sequence = source.seq AND target.Item = source.item)
                                WHEN MATCHED THEN
                                    UPDATE SET Balance = @bal, ShipmentPlanId = @planId, CreatedAt = GETDATE()
                                WHEN NOT MATCHED THEN
                                    INSERT ([Sales Order], Position, Sequence, [Qty Ordered], Item, [Sales Price], Balance, ShipmentPlanId, Status, [Rate Date])
                                    VALUES (@so, @pos, @seq, @origQty, @item, @price, @bal, @planId, @status, GETDATE());";

                            using (var cmd = new SqlCommand(sqlBalance, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@so", so.so);
                                cmd.Parameters.AddWithValue("@pos", so.position);
                                cmd.Parameters.AddWithValue("@seq", so.sequence);
                                cmd.Parameters.AddWithValue("@item", item);

                                cmd.Parameters.AddWithValue("@origQty", so.qty + qtyAllocatedToThisSo);
                                cmd.Parameters.AddWithValue("@price", Convert.ToDecimal(so.salesPrice));
                                cmd.Parameters.AddWithValue("@bal", newBalance);
                                cmd.Parameters.AddWithValue("@planId", planId);
                                cmd.Parameters.AddWithValue("@status", so.status);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                        return new AllocationResult { ok = true, message = "Alokasi Berhasil Disimpan!" };
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return new AllocationResult { ok = false, message = "Error Database: " + ex.Message };
                    }
                }
            }
        }

        protected void Upload_Click(object sender, EventArgs e)
        {
            try
            {
                string planId = hfShipmentPlanId.Value;
                string shipDate = dateShip.Value;
                string customer = selectCustomer.SelectedItem != null ? selectCustomer.SelectedItem.Text : "";
                string status = selectStatus.SelectedItem != null ? selectStatus.SelectedItem.Text : "";
                string pic = Session["Username"] != null ? Session["Username"].ToString() : "System";

                if (string.IsNullOrEmpty(planId))
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Shipment Plan ID is missing!');", true);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int newHeaderId = 0;
                            string sqlHeader = @"
                                INSERT INTO udt_shShipmentPlan 
                                    (ShipmentPlanID, Customer, Status, ShipmentDate, PIC, CreatedBy, CreatedDate)
                                OUTPUT INSERTED.ID
                                VALUES 
                                    (@PlanId, @Customer, @Status, @ShipDate, @PIC, 'System', GETDATE())";

                            using (SqlCommand cmd = new SqlCommand(sqlHeader, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@PlanId", planId);
                                cmd.Parameters.AddWithValue("@Customer", customer);
                                cmd.Parameters.AddWithValue("@Status", status);

                                DateTime parsedShipDate;
                                if (!DateTime.TryParse(shipDate, out parsedShipDate))
                                    parsedShipDate = DateTime.Now;

                                cmd.Parameters.AddWithValue("@ShipDate", parsedShipDate);
                                cmd.Parameters.AddWithValue("@PIC", pic);

                                newHeaderId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            string sqlDetail = @"
                                INSERT INTO udt_shShipmentPlanDetail 
                                    (ShipmentPlanID, BP, Item, Qty, SO, Position, Sequence, [Sales Price], [Total Box], CreatedDate, CreatedBy)
                                SELECT 
                                    @HeaderId,
                                    iso.BP,
                                    alloc.Item,
                                    alloc.TotalQty,
                                    alloc.SO,
                                    iso.Position,
                                    iso.Sequence,
                                    iso.[Sales Price],
                                    alloc.TotalBox,
                                    GETDATE(),
                                    'System'
                                FROM (
                                    SELECT 
                                        Item, 
                                        [Sales Order] as SO, 
                                        SUM(QTY) as TotalQty, 
                                        COUNT(CartonBoxId) as TotalBox
                                    FROM udt_CartonBoxAllocation
                                    WHERE ShipmentPlanId = @PlanIdStr
                                    GROUP BY Item, [Sales Order]
                                ) alloc
                                OUTER APPLY (
                                    SELECT TOP 1 Position, Sequence, [Sales Price], BP
                                    FROM udt_OpenSO 
                                    WHERE [Sales Order] = alloc.SO AND Item = alloc.Item
                                ) iso";

                            using (SqlCommand cmd = new SqlCommand(sqlDetail, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@HeaderId", newHeaderId);
                                cmd.Parameters.AddWithValue("@PlanIdStr", planId);
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                            ScriptManager.RegisterStartupScript(this, GetType(), "success",
                                $"alert('Shipment Plan {planId} has been successfully saved!'); window.location='/ListShipmentPlan';", true);
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            string safeErrorMsg = ex.Message.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
                            ScriptManager.RegisterStartupScript(this, GetType(), "error",
                                $"alert('Database Error: {safeErrorMsg}');", true);
                        }
                    }
                }
            }
            catch (Exception mainEx)
            {
                string safeErrorMsg = mainEx.Message.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
                ScriptManager.RegisterStartupScript(this, GetType(), "error",
                    $"alert('System Error: {safeErrorMsg}');", true);
            }
        }
    }
}
