<%@ Page Title="Dashboard" Language="C#" CodeBehind="TC23-DataBindMultiple.aspx.cs" Inherits="MyApp.TC23_DataBindMultiple" %>
<asp:GridView ID="gvOrders" runat="server" />
<asp:Repeater ID="rptCategories" runat="server">
    <ItemTemplate>
        <span><%# Eval("Name") %></span>
    </ItemTemplate>
</asp:Repeater>
