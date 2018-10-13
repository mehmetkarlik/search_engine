<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebSearcher.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            Arama Kelimesi: &nbsp;
            <asp:TextBox runat="server" ID="txtSearch"></asp:TextBox>
            <asp:Button runat="server" ID="btnSearch" OnClick="btnSearch_Click" Text="Arama Yap"/>
            <div style="height: 13px">
                
            </div>
            
            <asp:Repeater ID="RepCourse" runat="server">

                <ItemTemplate>
                    
                        <div style="width: 800px; font-size:13px;">
                            <a href="<%#Eval("url") %>">
                                <%#Eval("title") %>
                            </a>
                            <div style="color:chocolate">
                                <%#Eval("url") %>
                            </div>
                            <div id="description">
                                <%#Eval("description") %>
                            </div>
                            <div style="font-size:13px; color:gray; text-decoration:line-through;">
                                <%#Eval("drawed") %>
                            </div>
                            <div style="height: 11px"></div>
                        </div>
                        
                </ItemTemplate>

            </asp:Repeater>
            <asp:Panel runat="server" ID="pnlSonucYok" Visible="false">
                <br />
                <span style="margin-left:100px"></span><asp:Label runat="server" ForeColor="Red" Text="Sonuç Bulunamadı"></asp:Label>
            </asp:Panel>

            <asp:Label style="height: 20px" ID="lblTime" runat="server" ForeColor="GrayText" Text=""></asp:Label>
            <br />
            <br />
            <div style="overflow: hidden;">

                <asp:Repeater ID="rptPaging" runat="server" OnItemCommand="rptPaging_ItemCommand">
                    <ItemTemplate>
                        <asp:LinkButton ID="btnPage"
                            Style="padding: 8px; margin: 2px; background: #ffa100; border: solid 1px #666; font: 8pt tahoma;"
                            CommandName="Page" CommandArgument="<%# Container.DataItem %>"
                            runat="server" ForeColor="White" Font-Bold="True">
    <%# Container.DataItem %>
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:Repeater>

            </div>
        </div>

    </form>
</body>
</html>
