<%@ Page Title="Form" Language="C#" CodeBehind="TC24-EventWiringMultiple.aspx.cs" Inherits="MyApp.TC24_EventWiringMultiple" %>
<asp:TextBox ID="txtName" runat="server" OnTextChanged="Name_Changed" />
<asp:DropDownList ID="ddlCategory" runat="server" OnSelectedIndexChanged="Category_Changed" />
<asp:CheckBox ID="chkActive" runat="server" OnCheckedChanged="Active_Changed" />
<asp:Button ID="btnSave" runat="server" Text="Save" OnClick="Save_Click" />
