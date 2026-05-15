<%@ Page Title="Scan Pallet" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ScanPallet.aspx.cs" Inherits="ShippingAppPallet.ScanPallet" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .form-control-plaintext {
            border-bottom: 1px solid #808080
        }

        .form-control-plaintext:focus {
            border-bottom: 1px solid #007bff
        }
    </style>

    <div class="row">
        <div class="col-md-12 mt-3">
            <h3 class="text-center">VALIDATE PALLET</h3>
        </div>
    </div>

    <div class="row mt-5">
        <div class="col">
            <%-- Card Shipment Plan --%>
            <div class="card card-box bg-white p-3" style="border-top: 5px solid #28a745;">
                <div class="form-group w-100">
                    <label for="selectPlan" class="form-label mb-2">Shipment Plan</label>
                    <asp:DropDownList ID="selectPlan" runat="server" AutoPostBack="true" OnSelectedIndexChanged="selectPlan_SelectedIndexChanged" CssClass="form-select w-100" />
                </div>
                <div>
                    <asp:GridView ID="gvShipmentPlan" runat="server" AutoGenerateColumns="true"></asp:GridView>
                </div>
            </div>
        </div>
        <div class="col">
            <div class="row">
                <div class="col-md-12">
                    <%-- Card Scan --%>
                    <div class="card card-box bg-white mb-3 p-3" style="border-top: 5px solid #007bff;">
                        <div class="align-items-center justify-content-center row w-full">
                            <div class="text-center">
                                <asp:TextBox ID="txtScanBox" runat="server" AutoPostBack="true" OnTextChanged="txtScanBox_TextChanged" />
                            </div>
                            <div class="col-md-12">
                                <div id='loading_' style="margin-top: 10px; display: none;">
                                    <i class="fa fa-spinner fa-pulse fa-fw"></i><span>Loading...</span>
                                </div>
                                <p id="ScanReportStatus" style="font-weight: bolder; margin-top: 10px;"></p>
                                <div style="margin-top: 35px; display: none;" id="div-stop-alert">
                                    <button type="button" id="stop-alert" class="btn btn-warning">Skip</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <%-- Card Pallet --%>
                    <div class="card card-box bg-white p-3" style="border-top: 5px solid #007bff;">
                        <div class="row">
                            <div class="col-md-12 mb-3">
                                <div style="font-size: 14px; font-weight: bold;">Pallet ID: </div>
                            </div>
                            <div class="col-md-12 mb-3">
                                <h5>Total PN : <i id="partDetail_TotalPartNumber"></i></h5>
                                <h5>Total BOX : <i id="partDetail_TotalBOX"></i></h5>
                            </div>
                            <div class="col-md-12 m-b-30">
                                <asp:GridView ID="gvPalletDetails" runat="server" AutoGenerateColumns="true"></asp:GridView>
                                <asp:Label ID="lblStatus" runat="server" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function setFocus() {
            document.getElementById('<%= txtScanBox.ClientID %>').focus();
        }
        window.onload = setFocus;
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(setFocus);
    </script>

</asp:Content>
