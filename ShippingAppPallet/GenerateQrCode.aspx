<%@ Page Title="QR Code" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GenerateQrCode.aspx.cs" Inherits="ShippingAppPallet.GenerateQrCode" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="col-md-12 mt-3">
        <div style="margin-top: 50px;">
            <h3 class="fw-bold">QR Code Pallet</h3>

            <div class="d-flex align-items-center mb-4">
                <div>
                    <asp:Image ID="imgQrCode" runat="server" />
                </div>
                <div class="ms-4">
                    <asp:Button ID="btnPrint" runat="server" Text="Print QR & Detail" OnClientClick="printQrCode(); return false;" CssClass="btn btn-primary" />
                </div>
            </div>

            <div id="tableContainer" class="table-responsive" style="max-width: 800px;">
                <h4 class="fw-bold mb-3">Pallet Detail</h4>
                <asp:GridView ID="gvPalletDetail" runat="server" CssClass="table table-bordered table-striped" AutoGenerateColumns="true">
                    <EmptyDataTemplate>
                        Data detail pallet tidak ditemukan.
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function printQrCode() {
            // Ambil elemen gambar QR Code
            var qrImage = document.getElementById('<%= imgQrCode.ClientID %>');

            // Ambil struktur HTML dari tabel Pallet Detail
            var tableHtml = document.getElementById('tableContainer').innerHTML;

            // Buka window baru untuk print
            var printWindow = window.open('', '', 'width=800, height=800');
            printWindow.document.write('<html><head><title>Print QR Code & Detail</title>');

            // Tambahkan styling dasar untuk print agar tabel terlihat rapi
            printWindow.document.write('<style>');
            printWindow.document.write('body { font-family: Arial, sans-serif; padding: 20px; }');
            printWindow.document.write('table { width: 100%; border-collapse: collapse; margin-top: 20px; }');
            printWindow.document.write('table, th, td { border: 1px solid black; }');
            printWindow.document.write('th, td { padding: 8px; text-align: left; }');
            printWindow.document.write('</style>');
            printWindow.document.write('</head><body>');

            // Masukkan gambar QR Code
            printWindow.document.write('<h3 style="margin-bottom: 20px;">QR Code Pallet</h3>');
            printWindow.document.write('<img src="' + qrImage.src + '" style="width:200px;height:200px;" /><br/><br/>');

            // Masukkan HTML Tabel
            printWindow.document.write(tableHtml);

            printWindow.document.write('</body></html>');
            printWindow.document.close();

            // Beri sedikit waktu agar gambar/DOM selesai dimuat di window baru sebelum mencetak
            setTimeout(function () {
                printWindow.focus();
                printWindow.print();
            }, 250);
        }
    </script>
</asp:Content>
