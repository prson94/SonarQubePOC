<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="d360.web.Report" %>

<%@ Register Assembly="Microsoft.ReportViewer.WebForms, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" Namespace="Microsoft.Reporting.WebForms" TagPrefix="rsweb" %>
<script runat="server">
    protected override void EnsureChildControls()
    {
        if (!IsPostBack)
        {
            rpt.ServerReport.ReportPath = string.Format("/{0}", Request["ReportPath"]);
            //rpt.ServerReport.ReportPath = HttpUtility.UrlDecode(Request["ReportPath"]);
            rpt.ServerReport.ReportServerCredentials = new d360.web.Models.ReportServerCredentials();
            var parameters = new List<ReportParameter>();

            foreach (var key in Request.QueryString.AllKeys)
            {
                if (key != "ReportPath")
                {
                    parameters.Add(new ReportParameter(key, Request[key], false));
                }
            }
            //parameters.Add(new ReportParameter("@ObjectID", "2936", false));
            rpt.ServerReport.SetParameters(parameters);
        }
        
        base.EnsureChildControls();
    }
</script>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server"><title>Report</title></head>
<body>
    <form id="frm" runat="server">
        <asp:ScriptManager ID="mgr" runat="server"></asp:ScriptManager>
        <rsweb:ReportViewer ID="rpt" runat="server" 
            BorderStyle="None" InternalBorderStyle="None" 
            InteractivityPostBackMode="AlwaysAsynchronous" ProcessingMode="Remote" 
            ShowPageNavigationControls="true" Width="100%" Height="575" ShowToolBar="true">
            <%--<LocalReport ReportPath="Reports/ArtifactRelationship.rdl" />--%>
            <ServerReport ReportServerUrl="http://d3s-sql.cloudapp.net/ReportServer"  /> <%--ReportPath="/Client Reports/9/ArtifactRelationship"--%>
        </rsweb:ReportViewer>
    </form>
</body>
</html>
