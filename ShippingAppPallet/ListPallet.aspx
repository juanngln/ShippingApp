<%@ Page Title="List Pallet" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ListPallet.aspx.cs" Inherits="ShippingAppPallet.ListPallet" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        body {
            background-color: #f6f6f6;
        }
    </style>

    <%-- List Pallet ID --%>
    <div class="col-md-12 mt-3">
        <div class="fs-6 fw-bold">LIST PALLET</div>
        <div class="card rounded-0 shadow-sm">
            <div class="card-header bg-white d-flex justify-content-between align-items-center flex-wrap">
                <div>
                    <a href="<%= Page.ResolveUrl("~/AddPallet.aspx") %>" class="btn btn-sm btn-primary">
                        <i class="fa fa-plus"></i>Add New Pallet
                    </a>
                </div>
                <%-- Menambahkan UI Filter Tanggal dan Trip beserta tombol Export --%>
                <div class="d-flex align-items-center mt-2 mt-md-0">
                    <label class="me-2 fw-bold text-muted small">Date:</label>
                    <asp:TextBox ID="txtFilterDate" runat="server" CssClass="form-control form-control-sm me-3" TextMode="Date" Width="140px"></asp:TextBox>

                    <label class="me-2 fw-bold text-muted small">Trip:</label>
                    <asp:DropDownList ID="ddlFilterTrip" runat="server" CssClass="form-select form-select-sm me-3" Width="140px">
                        <asp:ListItem Text="All Trips" Value="" />
                        <asp:ListItem Text="Trip 1 (Pagi)" Value="1" />
                        <asp:ListItem Text="Trip 2 (Siang)" Value="2" />
                    </asp:DropDownList>
                    <asp:Button ID="btnFilter" runat="server" Text="Filter" CssClass="btn btn-sm btn-secondary me-1" OnClick="btnFilter_Click" />
                    <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-sm btn-outline-secondary me-1" OnClick="btnClear_Click" />
                    <%-- TOMBOL EXPORT BARU --%>
                    <asp:Button ID="btnExport" runat="server" Text="Export Excel" CssClass="btn btn-sm btn-success" OnClick="btnExport_Click" />
                </div>
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
                    <h5 class="modal-title">Pallet Detail</h5>
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
        function DetailPallet(id) {
            $.ajax({
                url: 'PalletDetail.aspx?ID=' + id,
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
            layout: {
                topStart: 'buttons'
            },
            buttons: ['excel'],
        });
    </script>
</asp:Content>
