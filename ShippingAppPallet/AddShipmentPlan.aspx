<%@ Page Title="Add Shipment Plan" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AddShipmentPlan.aspx.cs" Inherits="ShippingAppPallet.AddShipmentPlan" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .select2-container {
            width: 100% !important;
        }

        .table-inventory thead th {
            vertical-align: middle;
            background-color: #f8f9fa;
        }

        .child-row {
            background-color: #FBFBFB;
            border-left: 3px solid #0d6efd;
        }

        .child-content {
            padding: 15px;
        }

        .rotate-icon {
            transition: transform .25s ease;
            transform-origin: center center;
            display: inline-block;
        }

            .rotate-icon.down {
                transform: rotate(90deg);
            }

        .inner-table-wrapper {
            max-height: 300px;
            overflow-y: auto;
        }

        .inner-table thead {
            position: sticky;
            top: 0;
            background: #fff;
            z-index: 5;
        }

        .badge-stock {
            font-size: 0.9em;
            margin-left: 5px;
        }
    </style>

    <div class="row">
        <div class="col-12">

            <%-- 1. HEADER INFO --%>
            <div class="card rounded-0 shadow-sm mb-3">
                <div class="card-header bg-white">
                    <h5 class="mb-0 text-primary"><i class="fa fa-truck"></i>Create Shipment Plan</h5>
                </div>
                <div class="card-body py-2">
                    <div class="row">
                        <div class="col-md-3">
                            <label class="small text-muted">Shipment Plan ID</label>
                            <input type="text" runat="server" id="PlanId" class="form-control form-control-sm fw-bold" disabled>
                            <asp:HiddenField ID="hfShipmentPlanId" runat="server" />
                        </div>
                        <div class="col-md-3">
                            <label class="small text-muted">Ship Date</label>
                            <input type="text" class="form-control form-control-sm" id="dateShip" runat="server" disabled>
                        </div>
                        <div class="col-md-3">
                            <label class="small text-muted">Customer</label>
                            <asp:DropDownList ID="selectCustomer" CssClass="form-select form-select-sm" runat="server">
                                <asp:ListItem Selected="True" Value="globaltech">GlobalTech</asp:ListItem>
                                <asp:ListItem Value="whirlpool">MajuJaya</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-3">
                            <label class="small text-muted">Status</label>
                            <asp:DropDownList ID="selectStatus" CssClass="form-select form-select-sm" runat="server">
                                <asp:ListItem Selected="True" Value="vhub">V-Hub</asp:ListItem>
                                <asp:ListItem Value="non-vhub">Non V-Hub</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                </div>
            </div>

            <%-- 2. FILTER AREA (Product Family & BP Code) --%>
            <div class="card rounded-0 shadow-sm mb-3">
                <div class="card-body bg-light">
                    <div class="row align-items-end">
                        <div class="col-md-4">
                            <label class="form-label fw-bold">1. Product Family</label>
                            <select id="ddlFamily" class="form-control select2-multiple" multiple="multiple" data-placeholder="Select Family..."></select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-bold">2. BP Code</label>
                            <select id="ddlBpCode" class="form-control select2-multiple" multiple="multiple" data-placeholder="Select BP Code..."></select>
                        </div>
                        <div class="col-md-2">
                            <button type="button" id="btnLoadData" class="btn btn-primary w-100">
                                <i class="fa fa-search"></i>Load Data
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <%-- 3. MAIN TABLE (Inventory & SO List) --%>
            <div class="card rounded-0 shadow-sm mb-3">
                <div class="card-header bg-white d-flex justify-content-between align-items-center">
                    <h6 class="mb-0 text-muted"><i class="fa fa-list"> Inventory & SO List</i></h6>
                    <div class="input-group input-group-sm w-25">
                        <span class="input-group-text"><i class="fa fa-search"></i></span>
                        <input type="text" id="txtSearchItem" class="form-control" placeholder="Search Item..." />
                    </div>
                </div>
                <div class="card-body p-0">
                    <table class="table mb-0 table-inventory" id="tblMain">
                        <thead>
                            <tr>
                                <th style="width: 50px;"></th>
                                <th>Family</th>
                                <th>BP Code</th>
                                <th>Item</th>
                                <th class="text-end">Available QTY</th>
                                <th class="text-end">Open SO</th>
                                <th class="text-center">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td colspan="7" class="text-center p-2 text-muted">Select Product Family & BP Code then click Load Data</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <div class="text-end mb-5">
                <a href="/ListShipmentPlan" class="btn btn-light me-2">Cancel</a>
                <asp:Button ID="Upload" runat="server" Text="Complete" CssClass="btn btn-success px-3"
                    OnClick="Upload_Click" OnClientClick="return confirm('Are you sure you want to complete this Shipment Plan?');" />
            </div>

        </div>
    </div>

    <script type="text/javascript">
        // --- CONSTANTS & GLOBALS ---
        const ALLOWED_SO_STATUSES = new Set(["generate logical trucks", "awaiting delivery."]);
        let usedCartons = new Set();

        const isSelectableStatus = (status) => ALLOWED_SO_STATUSES.has((status || "").trim().toLowerCase());

        // --- AJAX HELPER ---
        const ajaxPost = (url, dataObj, onSuccess, onError) => {
            let baseUrl = window.location.pathname;
            let targetUrl = baseUrl.endsWith('/') ? baseUrl + url : baseUrl + '/' + url;

            $.ajax({
                type: "POST",
                url: targetUrl,
                data: dataObj ? JSON.stringify(dataObj) : "{}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: onSuccess,
                error: onError || ((err) => {
                    console.error("AJAX Error:", err);
                    alert("System Error. Please try again.");
                })
            });
        };

        // --- INITIALIZATION ---
        $(document).ready(function () {
            $('.select2-multiple').select2({ width: '100%' });

            generateShipmentPlanId();
            loadFamilies();

            $('#ddlFamily').on('change', function () { loadBpCodes($(this).val()); });
            $('#btnLoadData').on('click', loadInventoryTable);

            $('#txtSearchItem').on('keyup', function () {
                let keyword = $(this).val().toLowerCase();

                $('#tblMain tbody tr').not('.child-row').each(function () {
                    let $row = $(this);

                    if ($row.attr('data-item')) {
                        let itemText = $row.find('td:eq(3)').text().toLowerCase();
                        let isMatch = itemText.includes(keyword);

                        $row.toggle(isMatch);

                        let $child = $row.next('.child-row');
                        if ($child.length > 0) {
                            if (isMatch && $row.find('.btn-expand').hasClass('expanded')) {
                                $child.show();
                            } else {
                                $child.hide();
                            }
                        }
                    }
                });
            });

            $('#tblMain').on('click', '.btn-expand', function () {
                let $btn = $(this);
                let $tr = $btn.closest('tr');
                let item = $tr.data('item');
                let $icon = $btn.find('.rotate-icon');

                if ($btn.hasClass('expanded')) {
                    $tr.next('.child-row').remove();
                    $btn.removeClass('expanded').attr('aria-expanded', 'false');
                    $icon.removeClass('down');
                } else {
                    $btn.addClass('expanded').attr('aria-expanded', 'true');
                    $icon.addClass('down');
                    renderChildRow($tr, item);
                }
            });

            $(document).on('change', '.chk-carton, .chk-so', function () {
                calculateMatch($(this).closest('.child-content'));
            });

            $(document).on('click', '.btn-allocate-row', function () {
                saveAllocation($(this).closest('.child-content'), $(this));
            });
        });

        // --- DATA BINDING FUNCTIONS ---
        function generateShipmentPlanId() {
            ajaxPost("GenerateShipmentPlanId", null, res => {
                if (res.d) {
                    $('[id$="PlanId"], [id$="hfShipmentPlanId"]').val(res.d);
                }
            });
        }

        function loadFamilies() {
            ajaxPost("GetProductFamilies", null, res => {
                let ddl = $('#ddlFamily');
                $.each(res.d, (i, v) => ddl.append(new Option(v, v)));
            });
        }

        function loadBpCodes(families) {
            ajaxPost("GetBPCode", { families: families || [] }, res => {
                let ddl = $('#ddlBpCode').empty();
                $.each(res.d, (i, v) => ddl.append(new Option(v, v)));
            });
        }

        function loadInventoryTable() {
            let filters = { families: $('#ddlFamily').val() || [], bpCodes: $('#ddlBpCode').val() || [] };
            let $tbody = $('#tblMain tbody').html('<tr><td colspan="7" class="text-center"><i class="fa fa-spinner fa-spin"></i> Loading data...</td></tr>');

            ajaxPost("GetInventorySummary", filters, res => {
                $tbody.empty();
                if (!res.d || res.d.length === 0) {
                    return $tbody.html('<tr><td colspan="7" class="text-center text-muted">No data found matching filters.</td></tr>');
                }

                res.d.forEach(item => {
                    $tbody.append(` 
                <tr data-item="${item.item}" data-bp="${item.bp}">
                    <td class="text-center"> 
                        <button class="btn btn-sm btn-light btn-expand border" type="button" aria-expanded="false"> 
                            <i class="fa fa-chevron-right rotate-icon"></i> 
                        </button>
                    </td>
                    <td>${item.family}</td> 
                    <td>${item.bp}</td> 
                    <td class="fw-bold">${item.item}</td> 
                    <td class="text-end fw-bold">${item.stockQty}</td> 
                    <td class="text-end fw-bold">${item.demandQty}</td> 
                    <td class="text-center status-cell">
                        <span class="badge bg-secondary">Unchecked</span>
                    </td>
                </tr> 
            `);
                });
            });
        }

        // --- EXPANDABLE ROW ---
        function renderChildRow($parentTr, item) {
            let colSpan = $parentTr.children('td').length;
            let html = ` 
            <tr class="child-row"> 
                <td colspan="${colSpan}" class="p-0"> 
                    <div class="child-content"> 
                        <div class="row"> 
                            <div class="col-md-5 border-end"> 
                                <h6 class="text-muted mb-2"> <i class="fa-solid fa-box-archive"></i> Available Stock <span class="badge bg-secondary float-end total-carton-disp">0</span> </h6> 
                                <div class="inner-table-wrapper"> 
                                    <table class="table table-bordered table-hover table-sm inner-table" id="tblCarton_${item}"> 
                                        <thead class="table-light"> 
                                            <tr><th style="width:30px;"><input type="checkbox" disabled></th><th>Carton Box</th><th class="text-end">QTY</th></tr> 
                                        </thead> 
                                        <tbody><tr><td colspan="3" class="text-center"><i class="fa fa-spinner fa-spin"></i></td></tr></tbody> 
                                    </table> 
                                </div> 
                            </div> 
                            <div class="col-md-7"> 
                                <h6 class="text-muted mb-2"> <i class="fa-solid fa-file"></i> Open Sales Orders <span class="badge bg-secondary float-end total-so-disp">0</span> </h6> 
                                <div class="inner-table-wrapper"> 
                                    <table class="table table-bordered table-hover table-sm inner-table" id="tblSo_${item}"> 
                                        <thead class="table-light"> 
                                            <tr><th style="width:30px;"><input type="checkbox" disabled></th><th>SO Number</th><th>Position</th><th>Sequence</th><th class="text-end">QTY Ordered</th><th>Sales Price</th><th>Status</th></tr> 
                                        </thead> 
                                        <tbody><tr><td colspan="7" class="text-center"><i class="fa fa-spinner fa-spin"></i></td></tr></tbody> 
                                    </table> 
                                </div> 
                                <div class="mt-3 pt-2 bottom-0 border-top d-flex justify-content-between align-items-center"> 
                                    <span class="match-status fw-bold text-muted small">Select Cartons &amp; SOs to allocate</span> 
                                    <button class="btn btn-sm btn-primary btn-allocate-row" disabled> <i class="fa fa-check"></i> Match &amp; Allocate </button> 
                                </div> 
                            </div> 
                        </div> 
                    </div> 
                </td> 
            </tr>`;
            $parentTr.after(html);

            let dCarton = loadChildCartons(item);
            let dSo = loadChildSo(item);

            $.when(dCarton, dSo).done(() => autoSelectRow($parentTr.next('.child-row').find('.child-content')));
        }

        function setMainRowStatus($container, state, $parentRow = null) {
            let badge = (state || '').toLowerCase() === 'matched'
                ? '<span class="badge bg-success">Matched</span>'
                : '<span class="badge bg-danger">Not Matched</span>';

            let targetCell = $parentRow
                ? $parentRow.find('td:last')
                : $container.closest('.child-row').prev().find('td:last');

            targetCell.html(badge);
        }

        // --- CHILD DATA ---
        function loadChildCartons(item) {
            return $.Deferred(dfd => {
                ajaxPost("GetCartonList", { item: item }, res => {
                    let $tbody = $(`#tblCarton_${item} tbody`).empty();
                    if (!res.d || res.d.length === 0) return $tbody.html('<tr><td colspan="3" class="text-center small">No stock available</td></tr>'), dfd.resolve();

                    res.d.forEach(o => {
                        if (!usedCartons.has(o.box)) {
                            $tbody.append(`<tr><td><input type="checkbox" class="chk-carton" value="${o.box}" data-qty="${o.qty}"></td><td class="small">${o.display}</td><td class="text-end small">${o.qty}</td></tr>`);
                        }
                    });
                    dfd.resolve();
                });
            }).promise();
        }

        function loadChildSo(item) {
            return $.Deferred(dfd => {
                ajaxPost("GetSoList", { item: item, totalQty: 0 }, res => {
                    let $tbody = $(`#tblSo_${item} tbody`).empty();
                    if (!res.d || res.d.length === 0) return $tbody.html('<tr><td colspan="7" class="text-center small">No Open SO</td></tr>'), dfd.resolve();

                    let eligible = [], nonEligible = [];
                    res.d.forEach(o => isSelectableStatus(o.status) ? eligible.push(o) : nonEligible.push(o));

                    eligible.concat(nonEligible).forEach(o => {
                        let disabled = !isSelectableStatus(o.status);
                        let trClass = disabled ? ' class="text-muted"' : '';
                        let disabledAttr = disabled ? 'disabled title="Status invalid"' : '';
                        $tbody.append(`
                            <tr${trClass}>
                                <td><input type="checkbox" class="chk-so" value="${o.so}" data-pos="${o.position}" data-seq="${o.sequence}" data-price="${o.salesPrice}" data-qty="${o.qty}" ${disabledAttr}></td>
                                <td class="small">${o.so}</td>
                                <td class="small">${o.position}</td>
                                <td class="small">${o.sequence}</td>
                                <td class="text-end small">${o.qty}</td>
                                <td class="small">$${o.salesPrice}</td>
                                <td class="small">${o.status}</td>
                            </tr>
                        `);
                    });
                    dfd.resolve();
                });
            }).promise();
        }

        // --- LOGIC FUNCTIONS ---
        function autoSelectRow($container) {
            $container.find('.chk-so, .chk-carton').prop('checked', false);

            let soQueue = $container.find('.chk-so:not(:disabled)').map(function () {
                return { $el: $(this), totalNeeded: parseInt($(this).data('qty')) || 0, currentFilled: 0 };
            }).get();

            if (soQueue.length === 0) return calculateMatch($container);

            let currentSoIndex = 0;
            $container.find('.chk-carton').each(function () {
                if (currentSoIndex >= soQueue.length) return false;

                let $box = $(this);
                let boxQty = parseInt($box.data('qty')) || 0;
                let currentSo = soQueue[currentSoIndex];
                let remainingSpace = currentSo.totalNeeded - currentSo.currentFilled;

                if (boxQty <= remainingSpace) {
                    $box.prop('checked', true);
                    currentSo.currentFilled += boxQty;
                    if (currentSo.currentFilled === currentSo.totalNeeded) currentSoIndex++;
                } else {
                    for (let tempIdx = currentSoIndex + 1; tempIdx < soQueue.length; tempIdx++) {
                        let nextSo = soQueue[tempIdx];
                        if (boxQty <= (nextSo.totalNeeded - nextSo.currentFilled)) {
                            $box.prop('checked', true);
                            nextSo.currentFilled += boxQty;
                            currentSoIndex = tempIdx;
                            if (nextSo.currentFilled === nextSo.totalNeeded) currentSoIndex++;
                            break;
                        }
                    }
                }
            });

            soQueue.forEach(so => { if (so.currentFilled > 0) so.$el.prop('checked', true); });
            calculateMatch($container);
        }

        function calculateMatch($container) {
            let sumCarton = $container.find('.chk-carton:checked').toArray().reduce((sum, el) => sum + (parseInt($(el).data('qty')) || 0), 0);
            let sumSoAllocated = $container.find('.chk-so:checked').toArray().reduce((sum, el) => sum + (parseInt($(el).data('qty')) || 0), 0);

            $container.find('.total-carton-disp').text(sumCarton);
            $container.find('.total-so-disp').text(sumSoAllocated);

            let $btn = $container.find('.btn-allocate-row');
            let $status = $container.find('.match-status');

            setMainRowStatus($container, (sumCarton > 0 && sumCarton <= sumSoAllocated) ? 'matched' : 'not matched');

            if (sumCarton > 0 && sumSoAllocated > 0) {
                let isValid = sumCarton <= sumSoAllocated;
                $btn.prop('disabled', !isValid);
                $status.html(isValid
                    ? `<span class="text-success"><i class="fa fa-check-circle"></i> Ready (${sumCarton} qty)</span>`
                    : `<span class="text-danger">Over Allocation! Box (${sumCarton}) > SO (${sumSoAllocated})</span>`
                );
            } else {
                $btn.prop('disabled', true);
                $status.text("Select Cartons & SOs...");
            }
        }

        // --- ALLOCATE ---
        function saveAllocation($container, $btn) {
            let $parentRow = $container.closest('.child-row').prev();
            let item = $parentRow.data('item');
            let planId = $('[id$="PlanId"]').val();

            if (!planId) return alert("Shipment Plan ID is missing!");

            let selectedBoxes = $container.find('.chk-carton:checked').map(function () {
                return { box: $(this).val(), qty: parseInt($(this).data('qty')) };
            }).get();

            let selectedSos = $container.find('.chk-so:checked').map(function () {
                let $el = $(this);
                return {
                    so: $el.val(), position: String($el.data('pos')), sequence: String($el.data('seq')),
                    qty: parseInt($el.data('qty')), salesPrice: parseFloat($el.data('price')) || 0,
                    status: $el.closest('tr').find('td:last').text().trim()
                };
            }).get();

            if (selectedBoxes.length === 0 || selectedSos.length === 0) return;

            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Processing...');

            ajaxPost("BatchAllocate", { item, planId, boxes: selectedBoxes, sos: selectedSos }, res => {
                if (res.d && res.d.ok) {
                    let totalAllocatedQty = selectedBoxes.reduce((sum, b) => { usedCartons.add(b.box); return sum + b.qty; }, 0);
                    let [$tdStock, $tdDemand, $tdStatus] = [$parentRow.find('td').eq(4), $parentRow.find('td').eq(5), $parentRow.find('td').eq(6)];

                    $tdStock.text(Math.max(0, (parseInt($tdStock.text()) || 0) - totalAllocatedQty));
                    $tdDemand.text(Math.max(0, (parseInt($tdDemand.text()) || 0) - totalAllocatedQty));

                    $tdStatus.html('<span class="badge bg-warning text-dark">Evaluating...</span>');

                    $container.find('.chk-carton, .chk-so').prop('disabled', true);
                    $btn.html('<i class="fa fa-spinner fa-spin"></i> Reloading...');

                    $.when(loadChildCartons(item), loadChildSo(item)).done(() => {
                        autoSelectRow($container);
                        $btn.html('<i class="fa fa-check"></i> Match & Allocate');

                        if ($container.find('.chk-carton').length === 0 || $container.find('.chk-so:not(:disabled)').length === 0) {
                            setMainRowStatus(null, 'Matched', $parentRow);

                            $container.closest('.child-row').remove();
                            $parentRow.find('.btn-expand').removeClass('expanded').find('i').removeClass('down'); // Catatan: sebelumnya pakai rotate-90, tapi CSS-nya class .down
                        }
                    });
                } else {
                    alert(`Error: ${res.d ? res.d.message : "Unknown error"}`);
                    $btn.prop('disabled', false).text("Try Again");
                }
            }, () => {
                $btn.prop('disabled', false).text("Try Again");
            });
        }
    </script>
</asp:Content>
