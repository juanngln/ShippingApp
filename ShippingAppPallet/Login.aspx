<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="ShippingAppPallet.Login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link href="~/Content/bootstrap.min.css" rel="stylesheet" />

    <title>Shipping App - Login</title>

    <style>
        body {
            background: #0094ff;
        }

        .login-wrapper {
            min-height: 100vh;
        }

        .card-login {
            max-width: 400px;
            width: 100%;
        }

        .brand {
            font-weight: 700;
            letter-spacing: .3px;
        }

        .form-text-small {
            font-size: .85rem;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="login-wrapper d-flex align-items-center justify-content-center p-3">
            <div class="card card-login shadow">
                <div class="card-body p-4">
                    <div class="text-center mb-3">
                        <div class="brand mb-1">
                            <h1 class="text-primary">XYZ</h1>
                        </div>
                        <div class="text-muted">Please enter your account</div>
                    </div>

                    <asp:ValidationSummary ID="vsErrors" runat="server" CssClass="alert alert-danger py-2"
                        HeaderText="Periksa kembali input Anda:" DisplayMode="BulletList" EnableClientScript="true" />

                    <asp:Label ID="lblMessage" runat="server" CssClass="alert alert-warning d-none" EnableViewState="false"></asp:Label>

                    <div class="mb-3">
                        <label for="txtUsername" class="form-label">Username</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" MaxLength="100" />
                        <asp:RequiredFieldValidator ID="rfvUsername" runat="server"
                            ControlToValidate="txtUsername" Display="Dynamic"
                            CssClass="text-danger form-text-small"
                            ErrorMessage="Please enter username" />
                    </div>

                    <div class="mb-2">
                        <label for="txtPassword" class="form-label">Password</label>
                        <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" />
                        <asp:RequiredFieldValidator ID="rfvPassword" runat="server"
                            ControlToValidate="txtPassword" Display="Dynamic"
                            CssClass="text-danger form-text-small"
                            ErrorMessage="Please enter password" />
                    </div>

                    <div class="form-check mb-3">
                        <asp:CheckBox ID="RememberMe" runat="server" />
                        <label class="form-check-label" for="RememberMe">Remember me</label>
                    </div>

                    <div class="d-grid gap-2">
                        <asp:Button ID="btnLogin" OnClick="btnLogin_Click" runat="server" Text="Login" CssClass="btn btn-primary" />
                    </div>

                    <div class="text-center mt-3">
                        <small class="text-muted">© <%= DateTime.Now.Year %> Shipping Team</small>
                    </div>
                </div>
            </div>
        </div>

        <script src="Scripts/bootstrap/js/bootstrap.min.js"></script>

        <script>
            (function () {
                var lbl = document.getElementById('<%= lblMessage.ClientID %>');
                if (lbl && lbl.textContent && lbl.textContent.trim().length > 0) {
                    lbl.classList.remove('d-none');
                }
            })();
        </script>
    </form>
</body>
</html>
