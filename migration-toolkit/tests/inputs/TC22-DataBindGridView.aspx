<%@ Page Title="Products" Language="C#" CodeBehind="TC22-DataBindGridView.aspx.cs" Inherits="MyApp.TC22_DataBindGridView" %>
<asp:GridView ID="gvProducts" runat="server" AutoGenerateColumns="false">
    <Columns>
        <asp:BoundField DataField="Name" HeaderText="Product Name" />
        <asp:BoundField DataField="Price" HeaderText="Price" />
    </Columns>
</asp:GridView>
