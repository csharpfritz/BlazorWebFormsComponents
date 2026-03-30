<%@ Page Title="Products" Language="C#" CodeBehind="TC25-DataBindAndEvents.aspx.cs" Inherits="MyApp.TC25_DataBindAndEvents" %>
<asp:GridView ID="gvProducts" runat="server" OnRowCommand="Grid_RowCommand" AutoGenerateColumns="false">
    <Columns>
        <asp:BoundField DataField="Name" HeaderText="Name" />
        <asp:ButtonField CommandName="Select" Text="Select" />
    </Columns>
</asp:GridView>
<asp:Button ID="btnRefresh" runat="server" Text="Refresh" OnClick="Refresh_Click" />
