<%@ Page Title="Add Pallet" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AddPallet.aspx.cs" Inherits="ShippingAppPallet.AddPallet" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        body {
            background-color: Background;
        }

        .btn-circle {
            width: 20px;
            height: 20px;
            padding: 0;
            border-radius: 50%;
            text-align: center;
            font-weight: 700;
        }

        .btn-sm {
            font-size: 14px;
        }

        .table-shipment {
            min-width: 650px;
            white-space: nowrap;
        }

        .rotate-icon {
            transition: transform 0.3s ease-in-out;
        }

        .rotate-icon.rotated {
            transform: rotate(180deg);
        }
    </style>

    <div class="">
        <div class="row">
            <div class="col-md-12 mt-2">
                <h5><i class="fa-solid fa-box-archive"></i> PREPARE BY SCAN BOX BARCODE</h5>
            </div>
        </div>

        <div class="row mt-2">
            <%-- Shipment Plan --%>
            <div class="col-md-4">
                <div class="card rounded-0 shadow-sm bg-white" style="border-top: 5px solid #28a745;">
                    <div class="card-body">
                        <div class="fw-bold mb-3">Shipment Plan</div>
                        <div class="form-goup w-100">
                            <div class="form-label">Pallet Type</div>
                            <asp:RadioButton ID="palletType1" Text="V-Hub" Checked="true" GroupName="RadioGroup1" runat="server" />
                            <asp:RadioButton ID="palletType2" Text="Non-V-Hub" GroupName="RadioGroup1" runat="server" />
                        </div>
                        <div class="form-group w-100 mt-2">
                            <label for="selectCustomer" class="form-label mb-2">Customer</label>
                            <asp:DropDownList ID="ddlCustomer" runat="server" CssClass="form-select">
                                <asp:ListItem Text="-- select customer --" Value="" />
                                <asp:ListItem Text="globaltech" Value="GlobalTech" />
                                <asp:ListItem Text="majujaya" Value="MajuJaya" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-group w-100 mt-2">
                            <label for="ddlShipmentPlan" class="form-label mb-2">Shipment Plan</label>
                            <asp:DropDownList ID="ddlShipmentPlan" runat="server"
                                AutoPostBack="true"
                                OnSelectedIndexChanged="ddlShipmentPlan_SelectedIndexChanged"
                                CssClass="form-select w-100">
                                <asp:ListItem Text="-- select shipment plan --" Value=""></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mt-3" style="overflow-x: auto">
                            <asp:Repeater ID="rptShipmentPlan" runat="server">
                                <HeaderTemplate>
                                    <table class="table table-bordered table-sm table-hover table-shipment" id="tblMain">
                                        <thead class="table-light">
                                            <tr>
                                                <th style="width: 30px;"></th>
                                                <th>Status</th>
                                                <th>Item</th>
                                                <th>Total Qty</th>
                                                <th>Total Box</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr data-bs-toggle="collapse" data-bs-target='#collapse_<%# Container.ItemIndex %>' style="cursor: pointer;" class="bg-white">
                                        <td class="text-center">
                                            <button class="btn btn-sm btn-expand" type="button" aria-expanded="false">
                                                <i class="fa-solid fa-circle-chevron-down text-primary rotate-icon btn-expand"></i>
                                            </button>
                                        </td>
                                        <td class="fw-bold"><%# Eval("OverallStatus") %></td>
                                        <td class="fw-bold"><%# Eval("Item") %></td>
                                        <td><%# Eval("TotalQty") %></td>
                                        <td><%# Eval("TotalBox") %></td>
                                    </tr>
                                    <tr id='collapse_<%# Container.ItemIndex %>' class="collapse">
                                        <td colspan="5" class="p-0">
                                            <table class="table table-sm table-bordered mb-0 bg-light" style="font-size: 0.9em;">
                                                <thead class="table-secondary">
                                                    <tr>
                                                        <th>Status</th>
                                                        <th>Box Number</th>
                                                        <th>Qty</th>
                                                        <th>SO</th>
                                                        <th>Position</th>
                                                        <th>Sequence</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    <asp:Repeater ID="rptBoxes" runat="server" DataSource='<%# Eval("Boxes") %>'>
                                                        <ItemTemplate>
                                                            <tr>
                                                                <td><%# Eval("ValidStatus") %></td>
                                                                <td><%# Eval("CartonBoxId") %></td>
                                                                <td><%# Eval("Qty") %></td>
                                                                <td><%# Eval("SO") %></td>
                                                                <td><%# Eval("Position") %></td>
                                                                <td><%# Eval("Sequence") %></td>
                                                            </tr>
                                                        </ItemTemplate>
                                                    </asp:Repeater>
                                                </tbody>
                                            </table>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                        </tbody>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </div>
            <%-- Pallet --%>
            <div class="col-md-8">
                <div class="row">
                    <div class="col-md-12">
                        <div class="mb-2">
                            <label class="text-muted me-2">Pallet Number:</label>
                            <span id="lblPalletNumber" runat="server" class="fw-bold"></span>
                        </div>
                        <div class="d-flex mb-3">
                            <a href="ListPallet.aspx" id="btnList" class="btn btn-light btn-sm px-3">
                                <i class="fa fa-list"></i>Go to Pallet List
                            </a>
                            <asp:Button ID="btnClear" runat="server" CssClass="btn btn-danger btn-sm ms-2 px-3" Text="Clear Data" OnClick="btnClear_Click" />
                            <asp:Button ID="btnComplete" runat="server" CssClass="btn btn-success btn-sm ms-2 px-3" Text="Complete" OnClick="btnComplete_Click" />
                        </div>
                        <%-- Card Scan Box --%>
                        <div class="card rounded-0 shadow-sm bg-white mb-3">
                            <div class="card-body bg-light">
                                <div class="d-flex align-items-center justify-content-center row w-full">
                                    <div class="d-flex text-center">
                                        <asp:TextBox ID="txtBoxScan" runat="server" AutoPostBack="true" OnTextChanged="txtBoxId_TextChanged" CssClass="form-control text-center" placeholder="Scan Box QR Code" />
                                    </div>
                                    <div class="text-center mt-3">
                                        <span class="fw-bold" id="lblMessage" runat="server"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <%-- Card List Box --%>
                        <div class="card rounded-0 shadow-sm bg-whit" style="border-top: 5px solid #007bff;">
                            <div class="card-body">
                                <div class="row">
                                    <div class="col-md-12 mb-3">
                                        <h5>Total PN : <i id="partDetail_TotalPartNumber" runat="server"></i></h5>
                                        <h5>Total BOX : <i id="partDetail_TotalBOX" runat="server"></i></h5>
                                    </div>
                                    <div class="col-md-12 m-b-30">
                                        <%-- Table Box --%>
                                        <asp:GridView ID="gvBoxes"
                                            runat="server"
                                            AutoGenerateColumns="false"
                                            CssClass="table table-bordered table-hover table-sm"
                                            DataKeyNames="BoxNumber"
                                            ShowHeader="true"
                                            ShowHeaderWhenEmpty="true"
                                            ShowFooter="true"
                                            OnRowEditing="gvBoxes_RowEditing"
                                            OnRowUpdating="gvBoxes_RowUpdating"
                                            OnRowCancelingEdit="gvBoxes_RowCancelingEdit"
                                            OnRowDeleting="gvBoxes_RowDeleting"
                                            OnRowCommand="gvBoxes_RowCommand"
                                            OnRowDataBound="gvBoxes_RowDataBound">
                                            <Columns>
                                                <asp:TemplateField HeaderText="" ItemStyle-Wrap="false" HeaderStyle-Wrap="false" ItemStyle-Width="1%">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="btnDelete" runat="server"
                                                            CssClass="btn btn-sm rounded-circle"
                                                            CommandName="Delete"
                                                            ToolTip="Delete row"
                                                            aria-label="Delete row">
                                                            <i class="fa-solid fa-circle-minus text-danger fs-5"></i>
                                                        </asp:LinkButton>
                                                    </ItemTemplate>
                                                    <FooterStyle CssClass="table-light" />
                                                    <FooterTemplate>
                                                        <asp:LinkButton ID="btnAddNew" runat="server" CommandName="AddNew" CssClass="btn btn-sm rounded-circle">
                                                            <i class="fa-solid fa-circle-plus text-primary fs-5"></i>                                                        
                                                        </asp:LinkButton>
                                                    </FooterTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="SHIPMENT PLAN" ItemStyle-Wrap="false" HeaderStyle-Wrap="false">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblShipmentPlan" runat="server" Text='<%# Eval("ShipmentPlan") %>' />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtShipmentPlan" runat="server" CssClass="form-control form-control-sm"
                                                            Text='<%# Bind("ShipmentPlan") %>' />
                                                        <asp:RequiredFieldValidator ID="rfvShipmentPlan" runat="server"
                                                            ControlToValidate="txtShipmentPlan"
                                                            ErrorMessage="Shipment Plan is empty"
                                                            CssClass="text-danger" Display="Dynamic" />
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:TextBox ID="txtShipmentPlanFooter" runat="server" CssClass="form-control form-control-sm" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="BOX NUMBER" ItemStyle-Wrap="false" HeaderStyle-Wrap="false">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblBoxNumber" runat="server" Text='<%# Eval("BoxNumber") %>' />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtBoxNumber" runat="server" CssClass="form-control form-control-sm"
                                                            Text='<%# Bind("BoxNumber") %>' />
                                                        <asp:RequiredFieldValidator ID="rfvBoxNumber" runat="server"
                                                            ControlToValidate="txtBoxNumber"
                                                            ErrorMessage="Box Number is empty"
                                                            CssClass="text-danger" Display="Dynamic" />
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:TextBox ID="txtBoxNumberFooter" runat="server" CssClass="form-control form-control-sm" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="PART NUMBER" ItemStyle-Wrap="false" HeaderStyle-Wrap="false">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblPartNumber" runat="server" Text='<%# Eval("PartNumber") %>' />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtPartNumber" runat="server" CssClass="form-control form-control-sm"
                                                            Text='<%# Bind("PartNumber") %>' />
                                                        <asp:RequiredFieldValidator ID="rfvPartNumber" runat="server"
                                                            ControlToValidate="txtPartNumber"
                                                            ErrorMessage="Part Number is empty"
                                                            CssClass="text-danger" Display="Dynamic" />
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:TextBox ID="txtPartNumberFooter" runat="server" CssClass="form-control form-control-sm" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="QTY@BOX" ItemStyle-Wrap="false" HeaderStyle-Wrap="false">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblQty" runat="server" Text='<%# Eval("Qty") %>' />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtQty" runat="server" CssClass="form-control form-control-sm"
                                                            Text='<%# Bind("Qty") %>' />
                                                        <asp:RequiredFieldValidator ID="rfvQty" runat="server"
                                                            ControlToValidate="txtQty"
                                                            ErrorMessage="QTY is empty"
                                                            CssClass="text-danger" Display="Dynamic" />
                                                        <asp:RegularExpressionValidator ID="revQty" runat="server"
                                                            ControlToValidate="txtQty"
                                                            ValidationExpression="^\d+$"
                                                            ErrorMessage="QTY harus bilangan bulat."
                                                            CssClass="text-danger" Display="Dynamic" />
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:TextBox ID="txtQtyFooter" runat="server" CssClass="form-control form-control-sm" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="PC" ItemStyle-Wrap="false" HeaderStyle-Wrap="false">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblPC" runat="server" Text='<%# Eval("PC") %>' />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtPC" runat="server" CssClass="form-control form-control-sm"
                                                            Text='<%# Bind("PC") %>' />
                                                        <asp:RequiredFieldValidator ID="rfvPC" runat="server"
                                                            ControlToValidate="txtPC"
                                                            ErrorMessage="PC is empty"
                                                            CssClass="text-danger" Display="Dynamic" />
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:TextBox ID="txtPCFooter" runat="server" CssClass="form-control form-control-sm" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:CommandField ShowEditButton="true"
                                                    EditText="Edit"
                                                    UpdateText="Simpan"
                                                    CancelText="Batal"
                                                    ItemStyle-Wrap="false"
                                                    HeaderStyle-Wrap="false"
                                                    ItemStyle-Width="1%" />
                                            </Columns>
                                        </asp:GridView>
                                        <%-- Table Summary --%>
                                        <asp:GridView ID="gvSummary" runat="server"
                                            AutoGenerateColumns="False"
                                            ShowHeader="true"
                                            ShowHeaderWhenEmpty="true"
                                            CssClass="table table-bordered table-sm">
                                            <Columns>
                                                <asp:BoundField DataField="ShipmentPlan" HeaderText="Shipment Plan" />
                                                <asp:BoundField DataField="PartNumber" HeaderText="Part Number" />
                                                <asp:BoundField DataField="TotalBox" HeaderText="Total BOX" />
                                                <asp:BoundField DataField="TotalQty" HeaderText="Total Qty" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function setFocus() {
            var txtScan = document.getElementById('<%= txtBoxScan.ClientID %>');
            if (txtScan) txtScan.focus();
        }

        function initExpandableRows() {
            $('[data-bs-toggle="collapse"]').removeAttr('data-bs-toggle');

            $('#tblMain tr.collapse').removeClass('collapse').hide();

            $('#tblMain').off('click', 'tr[data-bs-target]').on('click', 'tr[data-bs-target]', function () {
                let $masterRow = $(this);
                let targetSelector = $masterRow.attr('data-bs-target');
                let $childRow = $(targetSelector);
                let $icon = $masterRow.find('.rotate-icon');

                if ($childRow.is(':visible')) {
                    $childRow.hide();
                    $icon.removeClass('rotated');
                } else {
                    $childRow.show();
                    $icon.addClass('rotated');
                }
            });
        }

        $(document).ready(function () {
            setFocus();
            initExpandableRows();
        });

        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                setFocus();
                initExpandableRows();
            });
        }
    </script>
</asp:Content>
