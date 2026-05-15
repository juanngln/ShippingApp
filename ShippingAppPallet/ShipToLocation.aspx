<%@ Page Title="Project & Site" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ShipToLocation.aspx.cs" Inherits="ShippingAppPallet.ShipToLocation" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <style>
        body {
            background-color: #f6f6f6;
        }
    </style>

    <div class="col-md-12 mt-3">
        <div class="card bg-light rounded-0 shadow-sm mt-1">
            <div class="card-body row">
                <label class="form-label fw-bold">Upload Excel Project Site</label>
                <div class="col-md-10">
                    <asp:PlaceHolder ID="phAlert" runat="server" />
                    <asp:FileUpload ID="uploadExcel" runat="server" CssClass="form-control" />
                </div>
                <div class="col-md-2">
                    <asp:Button ID="btnUpload" runat="server"
                        Text="Upload"
                        CssClass="btn btn-primary w-100"
                        OnClick="btnUpload_Click" />
                </div>
            </div>
        </div>
    </div>

    <div class="col-md-12 mt-4">
        <div class="fs-6 fw-bold">LIST PROJECT SITE</div>
        <div class="card rounded-0 shadow-sm mt-1">
            <div class="card-body">
                <asp:PlaceHolder ID="PlaceHolder1" runat="server" />
            </div>
        </div>
    </div>

    <script>
        let table = new DataTable('#datatable-buttons', {

        });
    </script>
</asp:Content>
