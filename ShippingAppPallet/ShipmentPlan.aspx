<%@ Page Title="Shipment Plan" Language="C#" Async="true" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ShipmentPlan.aspx.cs" Inherits="ShipmentPlan" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <%-- List Shipment Plan --%>
    <div class="col-md-12 mt-3">
        <h5 class="fw-bold">LIST SHIPMENT PLAN</h5>
        <div class="card">
            <div class="card-body">
                <asp:Literal ID="listShipmentPlanTable" runat="server"></asp:Literal>
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
