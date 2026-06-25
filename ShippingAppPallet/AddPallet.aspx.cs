using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace ShippingAppPallet
{
    #region Data Models
    public class BoxDetail
    {
        public Guid RowId { get; set; }
        public string ShipmentPlan { get; set; }
        public string BoxNumber { get; set; }
        public string PartNumber { get; set; }
        public int Qty { get; set; }
        public string PC { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

    public class PlanMaster
    {
        public string ShipmentPlanID { get; set; }
        public string Customer { get; set; }
        public string Status { get; set; }
    }

    public class PlanDetailItem
    {
        public string Item { get; set; }
        public double TotalQty { get; set; }
        public int TotalBox { get; set; }
        public string OverallStatus { get; set; }
        public List<PlanBox> Boxes { get; set; } = new List<PlanBox>();
    }

    public class PlanBox
    {
        public string CartonBoxId { get; set; }
        public double Qty { get; set; }
        public string SO { get; set; }
        public string Position { get; set; }
        public string Sequence { get; set; }
        public string ValidStatus { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
    #endregion

    public partial class AddPallet : System.Web.UI.Page
    {
        #region UI Controls Declaration
        protected HtmlGenericControl lblPalletNumber;
        protected HtmlGenericControl partDetail_TotalPartNumber;
        protected HtmlGenericControl partDetail_TotalBOX;
        protected HtmlGenericControl partDetail_TotalQTY;
        protected HtmlGenericControl lblMessage;

        protected DropDownList ddlCustomer;
        protected DropDownList ddlShipmentPlan;
        protected TextBox txtBoxScan;
        protected GridView gvBoxes;
        protected GridView gvSummary;
        protected Repeater rptShipmentPlan;
        protected RadioButton palletType1;
        protected RadioButton palletType2;
        #endregion

        #region Constants & Helpers
        private static object DbValueOrNull(string s) => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s;
        private string DbConnectionString => System.Configuration.ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;
        private string ToUpperRaw(string pn) => string.IsNullOrWhiteSpace(pn) ? null : pn.Trim().ToUpperInvariant();
        private string ToUpperNormalized(string pn) => string.IsNullOrWhiteSpace(pn) ? null : Regex.Replace(pn.Trim().ToUpperInvariant(), @"[^A-Z0-9]", "");
        #endregion

        #region Page Lifecycle & Initial Setup
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindShipmentPlanDropdown();
                if (Request.QueryString["id"] != null)
                {
                    LoadExistingPallet(Request.QueryString["id"]);
                }
                else
                {
                    Session["ScannedBoxes"] = new List<BoxDetail>();
                    btnComplete.Text = "Complete";

                    ShowPalletNumberPreview();
                    BindEmptyShipmentPlanGrid();
                }

                var boxes = GetSessionBoxes();
                foreach (var b in boxes) if (b.RowId == Guid.Empty) b.RowId = Guid.NewGuid();
                SetSessionBoxes(boxes);

                RebindBoxesAndSummary();
                BindShipmentPlanAutoSelectAndBind();
            }
        }

        private void LoadExistingPallet(string palletIdStr)
        {
            if (!int.TryParse(palletIdStr, out int palletId)) return;
            using (var conn = new SqlConnection(DbConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT PalletNumber, Customer FROM udt_shPallet WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", palletId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            lblPalletNumber.InnerText = r["PalletNumber"].ToString();
                            string customer = r["Customer"].ToString();

                            if (ddlCustomer.Items.FindByValue(customer) == null && !string.IsNullOrEmpty(customer))
                                ddlCustomer.Items.Add(new ListItem(customer, customer));
                            ddlCustomer.SelectedValue = customer;
                            btnComplete.Text = "Update Pallet";
                        }
                    }
                }

                var loadedBoxes = new List<BoxDetail>();
                using (var cmd = new SqlCommand(@"
            SELECT pd.CartonBoxId, pd.PartNumber, pd.Qty, pd.PC, 
                   ISNULL(a.ShipmentPlanId, '') AS ShipmentPlan
            FROM udt_shPalletDetail pd
            LEFT JOIN udt_CartonBoxAllocation a ON pd.CartonBoxId = a.CartonBoxId
            WHERE pd.PalletID = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", palletId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            loadedBoxes.Add(new BoxDetail
                            {
                                RowId = Guid.NewGuid(),
                                BoxNumber = r["CartonBoxId"].ToString(),
                                PartNumber = r["PartNumber"].ToString(),
                                Qty = Convert.ToInt32(r["Qty"]),
                                PC = r["PC"].ToString(),
                                ShipmentPlan = r["ShipmentPlan"].ToString(),
                                IsValid = true,
                                ValidationMessage = "Loaded from existing pallet"
                            });
                        }
                    }
                }
                SetSessionBoxes(loadedBoxes);
                var distinctPlans = loadedBoxes.Select(b => b.ShipmentPlan).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
                if (distinctPlans.Count == 1)
                {
                    if (ddlShipmentPlan.Items.FindByValue(distinctPlans[0]) != null)
                        ddlShipmentPlan.SelectedValue = distinctPlans[0];
                    BindShipmentPlanForPlan(distinctPlans[0]);
                }
            }
        }

        private void ShowPalletNumberPreview(SqlConnection existingConn = null)
        {
            try
            {
                string nextIdStr = GetNextPalletIdPreview(existingConn);
                lblPalletNumber.InnerText = nextIdStr;
            }
            catch
            {
                lblPalletNumber.InnerText = $"{DateTime.Now:yyMMdd}-0001 (auto generate)";
            }
        }

        private string GetNextPalletIdPreview(SqlConnection existingConn)
        {
            string query = @"
        DECLARE @Prefix VARCHAR(7) = FORMAT(GETDATE(), 'yyMMdd') + '-';
        DECLARE @NextSeq INT = 1;
        
        -- Ambil 4 karakter terakhir sebagai nomor urut
        SELECT TOP 1 @NextSeq = CAST(RIGHT(PalletNumber, 4) AS INT) + 1
        FROM udt_shPallet
        WHERE PalletNumber LIKE @Prefix + '%' AND ISNUMERIC(RIGHT(PalletNumber, 4)) = 1
        ORDER BY PalletNumber DESC;
        
        -- Gabungkan Prefix dengan 4 digit Sequence (0001, 0002, dst)
        SELECT @Prefix + RIGHT('0000' + CAST(@NextSeq AS VARCHAR), 4);";
            if (existingConn != null && existingConn.State == ConnectionState.Open)
            {
                using (var cmd = new SqlCommand(query, existingConn))
                    return cmd.ExecuteScalar()?.ToString();
            }

            using (var conn = new SqlConnection(DbConnectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        private void ResetPalletizingState(SqlConnection existingConn = null)
        {
            SetSessionBoxes(new List<BoxDetail>());
            gvBoxes.EditIndex = -1;
            RebindBoxesAndSummary();

            try { ddlCustomer.ClearSelection(); } catch { }
            ClearFooterInputs();
            ShowPalletNumberPreview(existingConn);
            BindEmptyShipmentPlanGrid();
        }

        protected void btnClear_Click(object sender, EventArgs e) => ResetPalletizingState();
        #endregion

        #region Scanning & Database Validation
        protected void txtBoxId_TextChanged(object sender, EventArgs e)
        {
            var scannedBoxes = GetSessionBoxes();
            string qrData = txtBoxScan.Text.Trim();

            if (!string.IsNullOrEmpty(qrData))
            {
                var parts = qrData.Split(';');
                if (parts.Length >= 4)
                {
                    string boxNumber = parts[0], partNumber = parts[1], pc = parts[3];
                    int qty = Convert.ToInt32(parts[2]);

                    string shipmentPlan = ddlShipmentPlan.SelectedValue;

                    var validation = ValidateScanWithPlanAllocation(shipmentPlan, boxNumber, partNumber, qty, scannedBoxes);
                    var existing = scannedBoxes.FirstOrDefault(b => b.BoxNumber == boxNumber);

                    if (existing == null)
                    {
                        scannedBoxes.Add(new BoxDetail
                        {
                            RowId = Guid.NewGuid(),
                            ShipmentPlan = shipmentPlan,
                            BoxNumber = boxNumber,
                            PartNumber = partNumber,
                            Qty = qty,
                            PC = pc,
                            IsValid = validation.IsValid,
                            ValidationMessage = validation.Message
                        });
                    }
                    else
                    {
                        existing.PartNumber = partNumber;
                        existing.Qty = qty;
                        existing.PC = pc;
                        existing.ShipmentPlan = shipmentPlan;
                        existing.IsValid = validation.IsValid;
                        existing.ValidationMessage = validation.Message;
                    }

                    ShowInlineStatus(validation.Message, !validation.IsValid);
                }
                else
                {
                    ShowInlineStatus("QR Code invalid", true);
                }
            }

            SetSessionBoxes(scannedBoxes);
            txtBoxScan.Text = string.Empty;
            RebindBoxesAndSummary();

            // Auto refresh UI master-detail
            if (!string.IsNullOrEmpty(ddlShipmentPlan.SelectedValue))
                BindShipmentPlanForPlan(ddlShipmentPlan.SelectedValue);
        }

        private ValidationResult ValidateScanWithPlanAllocation(string planId, string boxNumber, string partNumber, int qty, List<BoxDetail> currentSessionBoxes)
        {
            if (string.IsNullOrWhiteSpace(planId))
                return new ValidationResult { IsValid = false, Message = "Select Shipment Plan" };
            if (string.IsNullOrWhiteSpace(boxNumber))
                return new ValidationResult { IsValid = false, Message = "Box Number invalid" };
            using (var conn = new SqlConnection(DbConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT Item, QTY 
                    FROM udt_CartonBoxAllocation 
                    WHERE ShipmentPlanId = @planId AND CartonBoxId = @boxId", conn))
                {
                    cmd.Parameters.AddWithValue("@planId", planId);
                    cmd.Parameters.AddWithValue("@boxId", boxNumber);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            return new ValidationResult { IsValid = false, Message = $"Box {boxNumber} not registered in Shipment Plan {planId}." };

                        string allocatedItem = r["Item"].ToString();
                        int allocatedQty = Convert.ToInt32(r["QTY"]);

                        if (!string.Equals(allocatedItem.Trim(), partNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                            return new ValidationResult { IsValid = false, Message = $"PN scan ({partNumber}) not matched with ({allocatedItem})." };

                        if (qty != allocatedQty)
                            return new ValidationResult { IsValid = false, Message = $"Qty scan ({qty}) not matched with ({allocatedQty})." };
                    }
                }
            }

            if (currentSessionBoxes.Any(b => string.Equals(b.BoxNumber, boxNumber, StringComparison.OrdinalIgnoreCase)))
                return new ValidationResult { IsValid = true, Message = "Box Updated" };
            return new ValidationResult { IsValid = true, Message = "Box Scan Valid" };
        }
        #endregion

        #region Shipment Plan Data (SQL)
        private List<PlanMaster> GetAvailableShipmentPlans()
        {
            var plans = new List<PlanMaster>();
            using (var conn = new SqlConnection(DbConnectionString))
            using (var cmd = new SqlCommand("SELECT ShipmentPlanID, Customer, Status FROM udt_shShipmentPlan ORDER BY CreatedDate DESC", conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        plans.Add(new PlanMaster
                        {
                            ShipmentPlanID = r["ShipmentPlanID"].ToString(),
                            Customer = r["Customer"].ToString(),
                            Status = r["Status"].ToString()
                        });
                    }
                }
            }
            return plans;
        }

        private void BindShipmentPlanDropdown()
        {
            var plans = GetAvailableShipmentPlans();
            string prevSelected = ddlShipmentPlan.SelectedValue;

            ddlShipmentPlan.Items.Clear();
            ddlShipmentPlan.Items.Add(new ListItem("-- select shipment plan --", ""));
            foreach (var p in plans) ddlShipmentPlan.Items.Add(new ListItem(p.ShipmentPlanID, p.ShipmentPlanID));
            if (!string.IsNullOrEmpty(prevSelected) && ddlShipmentPlan.Items.FindByValue(prevSelected) != null)
                ddlShipmentPlan.SelectedValue = prevSelected;
        }

        protected void ddlShipmentPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            string planId = ddlShipmentPlan.SelectedValue;
            if (string.IsNullOrEmpty(planId))
            {
                BindEmptyShipmentPlanGrid();
                return;
            }

            var selectedPlan = GetAvailableShipmentPlans().FirstOrDefault(p => p.ShipmentPlanID == planId);
            if (selectedPlan != null)
            {
                if (ddlCustomer.Items.FindByValue(selectedPlan.Customer) == null)
                    ddlCustomer.Items.Add(new ListItem(selectedPlan.Customer, selectedPlan.Customer));
                ddlCustomer.SelectedValue = selectedPlan.Customer;

                bool isNonVHub = !string.IsNullOrEmpty(selectedPlan.Status) && selectedPlan.Status.IndexOf("Non-V-Hub", StringComparison.OrdinalIgnoreCase) >= 0;
                if (palletType1 != null) palletType1.Checked = !isNonVHub;
                if (palletType2 != null) palletType2.Checked = isNonVHub;
            }

            BindShipmentPlanForPlan(planId);
            // Re-validate existing session boxes
            var boxes = GetSessionBoxes();
            foreach (var b in boxes)
            {
                var v = ValidateScanWithPlanAllocation(ddlShipmentPlan.SelectedValue, b.BoxNumber, b.PartNumber, b.Qty, boxes);
                b.IsValid = v.IsValid;
                b.ValidationMessage = v.Message;
            }
            SetSessionBoxes(boxes);
            RebindBoxesAndSummary();
        }

        private void BindShipmentPlanForPlan(string planId)
        {
            if (string.IsNullOrEmpty(planId)) { BindEmptyShipmentPlanGrid(); return; }

            var scannedBoxes = GetSessionBoxes();
            var detailItems = new List<PlanDetailItem>();

            using (var conn = new SqlConnection(DbConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT d.Item, SUM(d.Qty) as TotalQty, SUM(d.[Total Box]) as TotalBox
                    FROM udt_shShipmentPlan p JOIN udt_shShipmentPlanDetail d ON p.ID = d.ShipmentPlanID
                    WHERE p.ShipmentPlanID = @planId GROUP BY d.Item", conn))
                {
                    cmd.Parameters.AddWithValue("@planId", planId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            detailItems.Add(new PlanDetailItem
                            {
                                Item = r["Item"].ToString(),
                                TotalQty = Convert.ToDouble(r["TotalQty"]),
                                TotalBox = r["TotalBox"] != DBNull.Value ? Convert.ToInt32(r["TotalBox"]) : 0
                            });
                        }
                    }
                }

                using (var cmd = new SqlCommand(@"
                    SELECT a.Item, a.CartonBoxId, a.QTY, a.[Sales Order], d.Position, d.Sequence
                    FROM udt_CartonBoxAllocation a
                    LEFT JOIN udt_shShipmentPlan p ON p.ShipmentPlanID = a.ShipmentPlanId
                    LEFT JOIN udt_shShipmentPlanDetail d ON d.ShipmentPlanID = p.ID AND d.Item = a.Item AND d.SO = a.[Sales Order]
                    WHERE a.ShipmentPlanId = @planId", conn))
                {
                    cmd.Parameters.AddWithValue("@planId", planId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var detail = detailItems.FirstOrDefault(x => x.Item == r["Item"].ToString());
                            if (detail != null)
                            {
                                string boxId = r["CartonBoxId"].ToString();
                                var hit = scannedBoxes.FirstOrDefault(b => string.Equals(b.BoxNumber, boxId, StringComparison.OrdinalIgnoreCase));

                                string validStatusHtml = "";
                                if (hit == null)
                                    validStatusHtml = "<span style=\"color: #6c757d; font-weight: bold;\">Waiting</span>";
                                else if (hit.IsValid)
                                    validStatusHtml = "<span style=\"color: #28a745; font-weight: bold;\">Matched</span>";
                                else
                                    validStatusHtml = "<span style=\"color: #dc3545; font-weight: bold;\">Not Matched</span>";

                                detail.Boxes.Add(new PlanBox
                                {
                                    CartonBoxId = boxId,
                                    Qty = Convert.ToDouble(r["QTY"]),
                                    SO = r["Sales Order"].ToString(),
                                    Position = r["Position"] != DBNull.Value ? r["Position"].ToString() : "-",
                                    Sequence = r["Sequence"] != DBNull.Value ? r["Sequence"].ToString() : "-",
                                    ValidStatus = validStatusHtml
                                });
                            }
                        }
                    }
                }
            }

            foreach (var detail in detailItems)
            {
                if (detail.Boxes.Count == 0)
                {
                    detail.OverallStatus = "-";
                }
                else
                {
                    int total = detail.Boxes.Count;
                    int valid = detail.Boxes.Count(b => b.ValidStatus.Contains("#28a745"));
                    int invalid = detail.Boxes.Count(b => b.ValidStatus.Contains("#dc3545"));
                    if (valid == total)
                    {
                        detail.OverallStatus = "<span style=\"color: #28a745; font-weight: bold;\">Matched</span>";
                    }
                    else if (valid > 0)
                    {
                        detail.OverallStatus = "<span style=\"color: #ff9800; font-weight: bold;\">Partial Matched</span>";
                    }
                    else if (invalid == total || invalid > 0)
                    {
                        detail.OverallStatus = "<span style=\"color: #dc3545; font-weight: bold;\">Not Matched</span>";
                    }
                    else
                    {
                        detail.OverallStatus = "<span style=\"color: #6c757d; font-weight: bold;\">Waiting</span>";
                    }
                }
            }

            if (rptShipmentPlan != null)
            {
                rptShipmentPlan.DataSource = detailItems;
                rptShipmentPlan.DataBind();
            }

            if (detailItems.Count == 0) lblMessage.InnerHtml = $"ℹ️ No detail for Shipment Plan: {Server.HtmlEncode(planId)}";
        }

        // Auto Select Plan directly using SQL instead of API
        private void BindShipmentPlanAutoSelectAndBind()
        {
            var boxes = GetSessionBoxes();
            if (boxes.Count == 0) { BindEmptyShipmentPlanGrid(); return; }

            var pns = boxes.Select(b => ToUpperRaw(b.PartNumber)).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
            if (pns.Count == 0) return;

            string bestPlan = null;
            string inClause = string.Join(",", pns.Select((s, i) => $"@pn{i}"));
            string query = $@"
                SELECT TOP 1 p.ShipmentPlanID 
                FROM udt_shShipmentPlanDetail d
                JOIN udt_shShipmentPlan p ON p.ID = d.ShipmentPlanID
                WHERE UPPER(d.Item) IN ({inClause}) 
                GROUP BY p.ShipmentPlanID 
                ORDER BY COUNT(d.Item) DESC";
            using (var conn = new SqlConnection(DbConnectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                for (int i = 0; i < pns.Count; i++)
                    cmd.Parameters.AddWithValue($"@pn{i}", pns[i]);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null) bestPlan = result.ToString();
            }

            if (string.IsNullOrEmpty(bestPlan)) { BindEmptyShipmentPlanGrid(); return; }

            if (ddlShipmentPlan.Items.FindByValue(bestPlan) != null)
                ddlShipmentPlan.SelectedValue = bestPlan;
            BindShipmentPlanForPlan(bestPlan);
        }

        private void BindEmptyShipmentPlanGrid()
        {
            if (rptShipmentPlan != null)
            {
                rptShipmentPlan.DataSource = null;
                rptShipmentPlan.DataBind();
            }
        }
        #endregion

        #region Complete Pallet
        protected void btnComplete_Click(object sender, EventArgs e)
        {
            var scannedBoxes = GetSessionBoxes();
            string editIdStr = Request.QueryString["id"];
            int editId = 0;
            bool isEditMode = !string.IsNullOrEmpty(editIdStr) && int.TryParse(editIdStr, out editId);
            using (SqlConnection conn = new SqlConnection(DbConnectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string palletCustomer = ddlCustomer.SelectedValue;
                        int palletId;
                        string palletNumber;

                        if (isEditMode)
                        {
                            palletId = editId;
                            palletNumber = lblPalletNumber.InnerText;

                            using (SqlCommand cmd = new SqlCommand("UPDATE udt_shPallet SET Customer=@Customer WHERE ID=@ID", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Customer", DbValueOrNull(palletCustomer));
                                cmd.Parameters.AddWithValue("@ID", palletId);
                                cmd.ExecuteNonQuery();
                            }

                            using (SqlCommand cmd = new SqlCommand("DELETE FROM udt_shPalletDetail WHERE PalletID=@ID", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ID", palletId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // INSERT BARU yang aman dari bentrok dengan format PLTyyMMddxxx
                            string insertSql = @"
        DECLARE @Prefix VARCHAR(9) = 'PLT' + FORMAT(GETDATE(), 'yyMMdd');
        DECLARE @NextSeq INT = 1;

        -- Mengunci sementara row agar tidak ada nomor kembar jika diakses serentak
        SELECT TOP 1 @NextSeq = CAST(RIGHT(PalletNumber, 3) AS INT) + 1
        FROM udt_shPallet WITH (UPDLOCK, HOLDLOCK)
        WHERE PalletNumber LIKE @Prefix + '%' AND ISNUMERIC(RIGHT(PalletNumber, 3)) = 1
        ORDER BY PalletNumber DESC;

        DECLARE @NewPalletNumber VARCHAR(20) = @Prefix + RIGHT('000' + CAST(@NextSeq AS VARCHAR), 3);

        INSERT INTO udt_shPallet (CreatedBy, Customer, PalletNumber) 
        OUTPUT INSERTED.ID, INSERTED.PalletNumber
        VALUES (@CreatedBy, @Customer, @NewPalletNumber);
    ";

                            using (SqlCommand cmd = new SqlCommand(insertSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@CreatedBy", User.Identity.Name);
                                cmd.Parameters.AddWithValue("@Customer", DbValueOrNull(palletCustomer));

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        palletId = Convert.ToInt32(reader["ID"]);
                                        palletNumber = reader["PalletNumber"].ToString();
                                    }
                                    else
                                    {
                                        throw new Exception("Failed to get Shipment Plan");
                                    }
                                }
                            }
                        }

                        StringBuilder duplicateBoxes = new StringBuilder();
                        foreach (var box in scannedBoxes)
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM udt_shPalletDetail WHERE CartonBoxId=@CartonBoxId", conn, tx))
                            {
                                checkCmd.Parameters.AddWithValue("@CartonBoxId", box.BoxNumber);
                                if ((int)checkCmd.ExecuteScalar() > 0)
                                {
                                    duplicateBoxes.AppendLine(box.BoxNumber);
                                    continue;
                                }
                            }

                            using (SqlCommand insertCmd = new SqlCommand("INSERT INTO udt_shPalletDetail (CartonBoxId, PartNumber, Qty, PC, PalletID) VALUES (@CartonBoxId, @PartNumber, @Qty, @PC, @PalletID)", conn, tx))
                            {
                                insertCmd.Parameters.AddWithValue("@CartonBoxId", box.BoxNumber);
                                insertCmd.Parameters.AddWithValue("@PartNumber", box.PartNumber);
                                insertCmd.Parameters.AddWithValue("@Qty", box.Qty);
                                insertCmd.Parameters.AddWithValue("@PC", box.PC);
                                insertCmd.Parameters.AddWithValue("@PalletID", palletId);
                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        lblMessage.InnerHtml = duplicateBoxes.Length > 0
                            ? $"✅ Pallet {(isEditMode ? "updated" : "created")}: {palletNumber}<br/>⚠️ Some boxes already scanned in other pallets:<br/>{duplicateBoxes.ToString().Replace(Environment.NewLine, "<br/>")}"
                            : $"✅ Pallet {(isEditMode ? "updated" : "created")} with number: {palletNumber}";
                        if (!isEditMode) ResetPalletizingState(conn);
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        lblMessage.InnerHtml = $"❌ Error completing pallet: {Server.HtmlEncode(ex.Message)}";
                        if (!isEditMode) ShowPalletNumberPreview();
                    }
                }
            }
        }
        #endregion

        #region Session Management & Table UI Binding
        private List<BoxDetail> GetSessionBoxes() => (Session["ScannedBoxes"] as List<BoxDetail>) ?? new List<BoxDetail>();
        private void SetSessionBoxes(List<BoxDetail> boxes) => Session["ScannedBoxes"] = boxes ?? new List<BoxDetail>();
        private string FormatScanSummary(int scanned, int target)
        {
            string color = "#ff9800";
            if (scanned == target) color = "#28a745";
            else if (scanned > target) color = "#dc3545";

            return $"<span style=\"color: {color}; font-weight: bold;\">{scanned}</span> of {target}";
        }

        private void RebindBoxesAndSummary()
        {
            var boxes = GetSessionBoxes();
            gvBoxes.DataKeyNames = new[] { "RowId" };
            gvBoxes.DataSource = boxes;
            gvBoxes.DataBind();

            var dtSummary = new DataTable();
            dtSummary.Columns.Add("ShipmentPlan", typeof(string));
            dtSummary.Columns.Add("PartNumber", typeof(string));
            dtSummary.Columns.Add("TotalBox", typeof(int));
            dtSummary.Columns.Add("TotalQty", typeof(int));

            if (boxes.Count > 0)
            {
                var grouped = boxes.GroupBy(b => b.PartNumber).Select(g => new { PartNumber = g.Key, TotalBox = g.Count(), TotalQty = g.Sum(x => x.Qty) }).OrderBy(x => x.PartNumber);
                foreach (var item in grouped)
                {
                    var row = dtSummary.NewRow();
                    row["ShipmentPlan"] = ddlShipmentPlan.SelectedValue;
                    row["PartNumber"] = item.PartNumber;
                    row["TotalBox"] = item.TotalBox;
                    row["TotalQty"] = item.TotalQty;
                    dtSummary.Rows.Add(row);
                }
            }
            if (gvSummary != null) { gvSummary.DataSource = dtSummary; gvSummary.DataBind(); }

            int scannedPN = boxes.Select(b => b.PartNumber).Distinct().Count();
            int scannedBox = boxes.Count;
            int scannedQty = boxes.Sum(b => b.Qty);

            int targetPN = 0, targetBox = 0, targetQty = 0;
            string planId = ddlShipmentPlan.SelectedValue;

            if (!string.IsNullOrEmpty(planId))
            {
                using (var conn = new SqlConnection(DbConnectionString))
                {
                    conn.Open();

                    // PERBAIKAN: Fokus menghitung baris alokasi secara spesifik agar 
                    // label partDetail di atas tabel tidak menggelembung ke jumlah total plan
                    using (var cmd = new SqlCommand(@"
                SELECT COUNT(DISTINCT Item) as TargetPN,
                       COUNT(CartonBoxId) as TargetBox,
                       ISNULL(SUM(QTY), 0) as TargetQty
                FROM udt_CartonBoxAllocation
                WHERE ShipmentPlanId = @planId", conn))
                    {
                        cmd.Parameters.AddWithValue("@planId", planId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                targetPN = Convert.ToInt32(r["TargetPN"]);
                                targetBox = Convert.ToInt32(r["TargetBox"]);
                                targetQty = Convert.ToInt32(r["TargetQty"]);
                            }
                        }
                    }
                }
            }

            partDetail_TotalPartNumber.InnerHtml = FormatScanSummary(scannedPN, targetPN);
            partDetail_TotalBOX.InnerHtml = FormatScanSummary(scannedBox, targetBox);
            partDetail_TotalQTY.InnerHtml = FormatScanSummary(scannedQty, targetQty);
        }

        private void ShowInlineStatus(string message, bool isError)
        {
            lblMessage.InnerHtml = (isError ? "❌ " : "✅ ") + Server.HtmlEncode(message);
            string script = isError ? "playValidationSound(true);" : "playValidationSound(false);";
            ScriptManager.RegisterStartupScript(this, GetType(), "PlayValidationSound", script, true);
        }

        private void ClearFooterInputs()
        {
            (gvBoxes.FooterRow?.FindControl("txtBoxNumberFooter") as TextBox)?.SetText(string.Empty);
            (gvBoxes.FooterRow?.FindControl("txtPartNumberFooter") as TextBox)?.SetText(string.Empty);
            (gvBoxes.FooterRow?.FindControl("txtQtyFooter") as TextBox)?.SetText(string.Empty);
            (gvBoxes.FooterRow?.FindControl("txtPCFooter") as TextBox)?.SetText(string.Empty);
        }
        #endregion

        #region Grid Events (Box Management)
        protected void gvBoxes_RowEditing(object sender, GridViewEditEventArgs e) { gvBoxes.EditIndex = e.NewEditIndex; RebindBoxesAndSummary(); }
        protected void gvBoxes_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e) { gvBoxes.EditIndex = -1; RebindBoxesAndSummary(); }

        protected void gvBoxes_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            var boxes = GetSessionBoxes();
            GridViewRow row = gvBoxes.Rows[e.RowIndex];
            Guid rowId = (Guid)gvBoxes.DataKeys[e.RowIndex].Value;

            string newBoxNumber = ((TextBox)row.FindControl("txtBoxNumber"))?.Text?.Trim() ?? "";
            string partNumber = ((TextBox)row.FindControl("txtPartNumber"))?.Text?.Trim() ?? "";
            string qtyText = ((TextBox)row.FindControl("txtQty"))?.Text?.Trim() ?? "0";
            string pc = ((TextBox)row.FindControl("txtPC"))?.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(newBoxNumber)) { ShowInlineStatus("BOX NUMBER wajib diisi.", true); return; }
            if (string.IsNullOrWhiteSpace(partNumber)) { ShowInlineStatus("PART NUMBER wajib diisi.", true); return; }
            if (!int.TryParse(qtyText, out int qty) || qty < 0) { ShowInlineStatus("QTY harus bilangan bulat >= 0.", true); return; }
            if (string.IsNullOrWhiteSpace(pc)) { ShowInlineStatus("PC wajib diisi.", true); return; }

            var item = boxes.FirstOrDefault(b => b.RowId == rowId);
            if (item == null) { ShowInlineStatus("Row not found", true); return; }

            var validation = ValidateScanWithPlanAllocation(ddlShipmentPlan.SelectedValue, newBoxNumber, partNumber, qty, boxes);
            item.BoxNumber = newBoxNumber;
            item.PartNumber = partNumber;
            item.Qty = qty;
            item.PC = pc;
            item.IsValid = validation.IsValid;
            item.ValidationMessage = validation.Message;

            SetSessionBoxes(boxes);
            gvBoxes.EditIndex = -1;
            RebindBoxesAndSummary();
            BindShipmentPlanAutoSelectAndBind();
        }

        protected void gvBoxes_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            var boxes = GetSessionBoxes();
            boxes.RemoveAll(b => b.RowId == (Guid)gvBoxes.DataKeys[e.RowIndex].Value);
            SetSessionBoxes(boxes);
            gvBoxes.EditIndex = -1;
            RebindBoxesAndSummary();
            BindShipmentPlanAutoSelectAndBind();
        }

        protected void gvBoxes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var boxes = GetSessionBoxes();
            if (e.CommandName.Equals("AddNew", StringComparison.OrdinalIgnoreCase))
            {
                if (boxes.Count == 0) { ShowInlineStatus("Tidak ada baris untuk diduplikasi.", true); return; }
                var s = boxes.Last();
                boxes.Add(new BoxDetail { RowId = Guid.NewGuid(), BoxNumber = s.BoxNumber, PartNumber = s.PartNumber, Qty = s.Qty, PC = s.PC, IsValid = s.IsValid, ValidationMessage = s.ValidationMessage });
                ShowInlineStatus($"Row terakhir ({s.BoxNumber}) diduplikasi.", false);
            }
            else if (e.CommandName.Equals("AddBlank", StringComparison.OrdinalIgnoreCase))
            {
                boxes.Add(new BoxDetail { RowId = Guid.NewGuid(), BoxNumber = "", PartNumber = "", Qty = 0, PC = "", IsValid = false, ValidationMessage = "Data belum lengkap." });
                ShowInlineStatus("Blank row added.", false);
                gvBoxes.EditIndex = boxes.Count - 1;
            }
            else if (e.CommandName.Equals("Duplicate", StringComparison.OrdinalIgnoreCase))
            {
                int rowIndex = Convert.ToInt32(e.CommandArgument);
                if (rowIndex < 0 || rowIndex >= boxes.Count) { ShowInlineStatus("Row tidak ditemukan.", true); return; }
                var s = boxes[rowIndex];
                boxes.Insert(rowIndex + 1, new BoxDetail { RowId = Guid.NewGuid(), BoxNumber = s.BoxNumber, PartNumber = s.PartNumber, Qty = s.Qty, PC = s.PC, IsValid = s.IsValid, ValidationMessage = s.ValidationMessage });
                ShowInlineStatus($"Row {s.BoxNumber} diduplikasi.", false);
            }
            SetSessionBoxes(boxes);
            RebindBoxesAndSummary();
            BindShipmentPlanAutoSelectAndBind();
        }

        protected void gvBoxes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if (e.Row.DataItem is BoxDetail data) e.Row.ToolTip = data.ValidationMessage ?? string.Empty;
                if ((e.Row.RowState & DataControlRowState.Edit) > 0)
                {
                    (e.Row.FindControl("txtBoxNumber") as TextBox)?.Attributes.Add("placeholder", "BOX NUMBER");
                    (e.Row.FindControl("txtPartNumber") as TextBox)?.Attributes.Add("placeholder", "PART NUMBER");
                    (e.Row.FindControl("txtQty") as TextBox)?.Attributes.Add("placeholder", "Qty (int)");
                    (e.Row.FindControl("txtPC") as TextBox)?.Attributes.Add("placeholder", "PC");
                }
            }
        }
        #endregion
    }

    public static class TextBoxExtensions
    {
        public static void SetText(this TextBox tb, string text) { if (tb != null) tb.Text = text; }
    }
}