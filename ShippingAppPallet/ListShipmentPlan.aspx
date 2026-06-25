<%@ Page Title="Shipment Plan List" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ListShipmentPlan.aspx.cs" Inherits="ShippingAppPallet.ListShipmentPlan" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        body {
            background-color: #F6F6F6;
        }
    </style>

    <%-- List Shipment Plan --%>
    <div class="col-md-12 mt-3">
        <div class="fs-6 fw-bold">LIST SHIPMENT PLAN</div>
        <div class="card rounded-0 shadow-sm mt-1">
            <div id="cardHeader" runat="server" class="card-header bg-white">
                <a id="btnAddShipmentPlan" runat="server" href="~/AddShipmentPlan.aspx" class="btn btn-sm btn-primary">
                    <i class="fa fa-plus"></i>
                    Add Shipment Plan
                </a>
<%--                <asp:LinkButton ID="btnExportLOD" runat="server" CssClass="btn btn-sm btn-success ms-2" OnClick="btnExportLOD_Click">
                    <i class="fa fa-download"></i>
                    Export LOD Template
                </asp:LinkButton>--%>
            </div>
            <div class="card-body">
                <asp:PlaceHolder ID="PlaceHolder1" runat="server" />
            </div>
        </div>
    </div>

    <%-- Modal Detail --%>
    <div class="modal fade" id="detail-modal" tabindex="-1" aria-labelledby="detailModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-fullscreen">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Shipment Plan Detail</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body" id="modalContent"></div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <script>
        function DetailPlan(shipId) {
            $.ajax({
                url: 'ShipmentPlanDetail.aspx?ID=' + shipId,
                type: 'GET',
                success: function (response) {
                    $('#modalContent').html(response);
                    var detailModal = new bootstrap.Modal(document.getElementById('detail-modal'));
                    detailModal.show();
                },
                error: function () {
                    alert('Failed to get detail data');
                }
            });
        }

        let table = new DataTable('#datatable-buttons', {

        });
    </script>
</asp:Content>
