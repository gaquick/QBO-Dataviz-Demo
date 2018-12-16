<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="OAuth2_Dotnet_UsingSDK.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
    <head runat="server">
        <title>Data Visualisation Demo for PWC</title>
        <link rel="icon" href="include/favicon.png" type="image/x-icon" />
        <% if (dictionary.ContainsKey("accessToken"))
            {
                Response.Write("<script> window.opener.location.reload();window.close(); </script>");
            }
        %> 
        
        <link href="include/bootstrap-4.1.3-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
        <link href="include/MainPage.css" rel="stylesheet" type="text/css" />
    </head>
    <body>
        <header>
            <div class="loginHeader">
                <img id="pwclogo" src="include/PwC_Logo.png" style="position: absolute; top: 5px; right: 25px; width:50px; height:50px; " />
            </div>
        </header>
        <div class="mainSection" style="margin: 10px">
	        <div class="row">
                <div id="column_1" class="col-lg-6 col-md-6 col-sm-12 col-xs-12">
                <% Authenticate();
                    GetPayments();
                    GetVendorLocations();%>
                    <p class="title">Payments by Vendor</p>
                    <iframe height="470px" width="100%" frameborder="0" scrolling="no" src="Treemap.html" allowfullscreen="">
                    </iframe>
                </div>
                <div id="column_2" class="col-lg-6 col-md-6 col-sm-12 col-xs-12">
                    <p class="title">Vendor Locations</p>
                    <iframe height="470px" width="100%" frameborder="0" scrolling="no" src="GoogleMap.html" allowfullscreen="">
                    </iframe>
                </div>
            </div>
        </div>
    </body>
</html>