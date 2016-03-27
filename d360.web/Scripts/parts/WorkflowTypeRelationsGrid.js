function WorkflowTypeRelationsGrid(controlID, contextList, permissions, type, id, headerTitle) {
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    var html = "";
    var showHeader = (headerTitle != '');
    if (!headerTitle) headerTitle = 'Allocated Workflows';
    $(controlID).html((showHeader ? '<header>' + headerTitle + '</header>' : '') + '<div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var initrowdetails = function (index, parentElement, gridElement, datarecord) {

        var i = 1;
        var col = 0;
        for (var name in datarecord.Properties) {

            col = (i % 2) + 1;

            if (name == 'Responsibilities') {
                col = 1;
            }

            var div = $($(parentElement).children()[col]);

            var value = datarecord.Properties[name];
            div.append('<label for="lbl' + name + '">' + name + '</label>');
            div.append('<div id="lbl' + name + '">' + ((value == '') ? 'No value' : value) + '</div>');

            i++;
        }
    }

    var source = {
        datatype: 'json',
        url: '/api/' + type + '/' + id + '/workflowtypes',
        datafields:
        [
            { name: 'WorkflowType' },
            { name: 'WorkflowTypeName' },
            { name: 'WorkflowTypeDisplayName' },
            { name: 'Enabled' },
            { name: 'Required' },
            { name: 'Properties' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: false,
            pageable: false,
            selectionmode: 'none',
            autorowheight: true,
            source: adapter,
            theme: list_theme,
            rowdetails: true,
            rowdetailstemplate: {
                rowdetails: "<h4>Workflow Settings</h4><div class='pull-left' style='width: 49%'></div><div class='pull-right' style='width: 49%'></div><div class='clearfix'></div>"
            },
            initrowdetails: initrowdetails,
            columns: [
                {
                    columntype: 'dropdownlist',
                    filtertype: 'checkedlist',
                    datafield: "WorkflowTypeDisplayName",
                    text: "Workflow Type"
                },
                //{ datafield: "Required", text: "Required?", columntype: 'checkbox', filtertype: 'bool', width: '15%' },
                { datafield: "Enabled", text: "Enabled?", columntype: 'checkbox', filtertype: 'bool', width: '15%' },
                {
                    datafield: "WorkflowType",
                    text: "",
                    width: '80px',
                    filterable: false,
                    sortable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        //if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID) {
                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditWorkflowAllocation?workflowType={0}&type=' + type + '&id=' + id });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteWorkflowAllocation?workflowType={0}&type=' + type + '&id=' + id });
                        }
                        //}

                        return renderToolsHtml(value, tools, "WorkflowTypeRelation", data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case "WorkflowTypeRelation":
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}