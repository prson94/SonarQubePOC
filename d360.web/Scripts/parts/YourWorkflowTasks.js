function YourWorkflowTasks(controlID, givenWorkflowType, givenObjectType, givenObjectID, givenResourceID) {
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;
    var html = "";
    html += '<div class="row">';
    html += '<div class="col s12"><div id="' + gridControlID + '"></div></div>';
    html += '</div>';
    $(controlID).html(html);
    gridControlID = '#' + gridControlID;


    var gridSource;
    var gridAdapter;
    var inputWorkflowID = givenWorkflowType;
    var objectType = givenObjectType === undefined ? "" : givenObjectType;
    var objectID = givenObjectID === undefined ? -1 : givenObjectID;
    var resourceID = givenResourceID === undefined ? -1 : givenResourceID;

    //#region Event Subscriptions
    
    function saveAction(data) {      
        try {
            switch (data.context) {                
                case "IssueWorkflow":
                    switchToViewer();
                     $(gridControlID).jqxGrid('updatebounddata');
            }
        } catch (e) {
            logError("YourWorkflowTasks : SaveAction", e);
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('autoresizecolumns');
    }
    
    function localAction(data) {        
        try {
            switch (data.context) {
                case "workflowform":
                case "artifactform":
                    switchToEditor(data.uri);
                    break;
            }
        } catch (e) {
            logError("YourWorkflowTasks : LocalAction", e);
        }
    };

    function unsubscribe(data) {
        gridSource = null;
        gridAdapter = null;
        
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.OverlayUnsubscribe, unsubscribe);
        amplify.unsubscribe('ToolAction', localAction);       
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.OverlayUnsubscribe, unsubscribe);
    amplify.subscribe('ToolAction', localAction);   

    //#endregion

    //#region Helper Functions

    var switchToViewer = function () {        
        $('#AssignmentViewer').fadeIn(10);
        $('#AssignmentEditor').fadeOut(10);
    }

    var switchToEditor = function (uri) {
        try {
            $('#AssignmentViewer').fadeOut(10);
              $('#AssignmentEditor').fadeIn(10);
              $('#AssignmentEditor').html(progressIndicatorHtml);
              $('#AssignmentEditor').load(uri, function (response, status, xhr) {
                  if (status == "error") {
                      amplify.publish("ShowMessage", { title: "Something unexpected happened!", message: xhr.status + ' ' + xhr.statusText, type: 'error' });
                      switchToViewer();
                  }
              });
        } catch (e) {

        }
    }

    var getWorkflowUrl = function (workflowTypeID) {
        return '/services/workflow/tasks/types/' + workflowTypeID + (objectID >0 ? "/" + objectID : "") + (objectType != "" ? "/" + objectType : "") + '?' + (resourceID > 0 ? ('resourceID='+resourceID +'&'):'') + '$orderby=DateStarted%20asc';
    }

    var gridDataSource = function (workflowTypeID) {
        var gridSource;
        switch (workflowTypeID) {
            case 1:
                // Suggest
                gridSource = {
                    datatype: 'json',
                    url: getWorkflowUrl(workflowTypeID),
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'ID', type: 'number' },
                        { name: 'StartDate', type: 'date' },
                        { name: 'Name', type: 'string' },
                        { name: 'Url', type: 'string' },
                        { name: 'ProposedName', type: 'string' },
                        { name: 'ProposedDescription', type: 'string' },
                        { name: 'RequestingResourceID', type: 'number' },
                        { name: 'RequestingResourceName', type: 'string' },
                        { name: 'TaxonomyTypeID', type: 'number' },
                        { name: 'TaxonomyTypeName', type: 'string' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };

                break;
            case 2:
                // Certify
                gridSource = {
                    datatype: 'json',
                    url: getWorkflowUrl(workflowTypeID),
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'ID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'TypeName', type: 'string' },
                        { name: 'Url', type: 'string' },
                        { name: 'StartDate', type: 'date' },
                        { name: 'DueDate', type: 'date' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };
                break;
            case 3:
                // WorkIssue
                gridSource = {
                    datatype: 'json',
                    url: getWorkflowUrl(workflowTypeID),
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'Issue', type: 'string' },
                        { name: 'ResourceID', type: 'number' },
                        { name: 'ResourceName', type: 'string' },
                        { name: 'ResourceUrl', type: 'string' },
                        { name: 'DateStarted', type: 'date' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };
                break;
            case 4:
                // WorkIssue
                gridSource = {
                    datatype: 'json',
                    url: getWorkflowUrl(workflowTypeID),
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'Issue', type: 'string' },
                        { name: 'Url', type: 'string' },
                        { name: 'Name', type: 'string' },
                        { name: 'ArtifactID', type: 'number' },
                        { name: 'ResourceID', type: 'number' },
                        { name: 'ResourceName', type: 'string' },
                        { name: 'ResourceUrl', type: 'string' },
                        { name: 'DateStarted', type: 'date' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };
                break;
            default:
                console.log("unknown workflow type");
                break;
        }
        return gridSource;
    }

    var gridColumns = function (workflowTypeID) {
        var cols = null;
        switch (workflowTypeID) {
            case 1:
                //#region Suggest                
                cols = [
                    {
                        datafield: "Name", text: "Type", filtertype: 'checkedlist',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('ArtifactType', data.ID, data.Url, data.Name);
                        }
                    },
                    {
                        filtertype: 'checkedlist', datafield: "RequestingResourceName", text: "Requestor",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Resource', data.RequestingResourceID, '#/resources/' + data.RequestingResourceID, data.RequestingResourceName);
                        }
                    },
                    { datafield: "StartDate", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ProposedName", text: "Proposed Name" },
                    { datafield: "TaxonomyTypeName", text: "Subject Area", filtertype: 'checkedlist' },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            if (resourceID < 0)
                                tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Workflow, data);
                        }
                    }
                ];
                //#endregion
                break;
            case 2:

                cols = [
                    { datafield: "TypeName", text: "Type", filtertype: 'checkedlist' },
                    {
                        filtertype: 'checkedlist', datafield: "Name", text: "Name",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Artifact', data.ID, data.Url, data.Name);
                        }
                    },
                    { datafield: "StartDate", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "DueDate", text: "Date Due", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            if (resourceID < 0)
                                tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Workflow, data);
                        }
                    }
                ];
                //#endregion
                break;
            case 3:
                cols = [
                    { datafield: "Issue", text: "Issue" },
                    {
                        filtertype: 'checkedlist', datafield: "ResourceName", text: "Reporting User",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Resource', data.ResourceID, data.ResourceUrl, data.ResourceName);
                        }
                    },
                    { datafield: "DateStarted", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {                                                        
                            return '<div style="overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 15px;"' + (data.Activity == 0 ? 'data-type="WorkflowTypeRelation" data-context="list" data-id="' + data.WorkflowID + '"' : '') + '>' + value + '</div>';
                        }
                    },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            if(data.Activity > 0 && resourceID < 0)
                                tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Workflow, data);
                        }
                    }
                ];
                //#endregion
                break;
            case 4:
                cols = [
                    {
                        datafield: "Name", text: "Name",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return (data.ArtifactID > 0 ? previewLinkRenderer('Artifact', data.ArtifactID, data.Url, data.Name) : textrenderer("Deleted Item"));
                        }
                    },
                    { datafield: "Issue", text: "Reason" },
                    {
                        filtertype: 'checkedlist', datafield: "ResourceName", text: "Reporting User",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Resource', data.ResourceID, data.ResourceUrl, data.ResourceName);
                        }
                    },
                    { datafield: "DateStarted", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            if (resourceID < 0)
                                tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Workflow, data);
                        }
                    }
                ];
                //#endregion
                break;
        }
        return cols;

    };

    //#endregion

    //#region Item Grid

    var gridAdapter = new $.jqx.dataAdapter(gridDataSource(inputWorkflowID));

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 10,
            pageable: true,
            selectionmode: 'none',
            autorowheight: true,
            source: gridAdapter,
            theme: list_theme,
            columns: gridColumns(inputWorkflowID)
        });
    } catch (e) {
    }

    //#endregion

    //#endregion
}