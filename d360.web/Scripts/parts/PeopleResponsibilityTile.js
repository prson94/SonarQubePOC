function PeopleResponsibilityTile(controlID, contextList, permissions, type, id, title, showHidden) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    try {
        var html = "";
        if (title == '' || !title) title = 'People Responsibilities';
        html = '<header>' + title + '<div id="' + toolsControlID + '"></div></header>';
        html += '<div id="' + gridControlID + '"></div>';
        $(controlID).html(html);
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        if (!showHidden) showHidden = false;

        if (permissions.HasPermission("Governance", "Create")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddResponsibility?type=" + type + "&id=" + id, context: contextList.Responsibility, title: 'Add responsibility' }
            ]);
        }

        source = {
            datatype: 'json',
            url: '/api/' + type + '/' + id + '/ownership?showHidden=' + showHidden,
            datafields:
            [
                { name: 'ResponsibilityID' },
                { name: 'AssigningItemType' },
                { name: 'AssigningItemID' },
                { name: 'ResponsibleObjectType' },
                { name: 'ResponsibleObjectID' },
                { name: 'ResponsibleObjectName' },
                { name: 'PrimaryOwnerResourceID' },
                { name: 'PrimaryOwnerResourceName' },
                { name: 'PrimaryOwnerResourceUrl' },
                { name: 'ObjectType' },
                { name: 'ObjectID' },
                { name: 'Role' },
                { name: 'ResponsibleObjectUrl' },
                { name: 'ContextItems' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            selectionmode: 'none',
            autorowheight: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "Role", text: "Role", width: '20%' },
                {
                    columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "ResponsibleObjectName", text: "Resource", width: '20%',
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return previewLinkRenderer(data.ResponsibleObjectType, data.ResponsibleObjectID, data.ResponsibleObjectUrl, data.ResponsibleObjectName);
                    }
                },
                {
                    columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "PrimaryOwnerResourceName", text: "Group Owner", width: '20%',
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        if (data.PrimaryOwnerResourceName && data.PrimaryOwnerResourceName != '')
                            return previewLinkRenderer('Resource', data.PrimaryOwnerResourceID, data.PrimaryOwnerResourceUrl, data.PrimaryOwnerResourceName);
                        else
                            return '';
                    }
                },
                { datafield: "ContextItems", text: "Context" },
                {
                    datafield: "ResponsibilityID", text: "", width: '80px', filterable: false, sortable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID) {
                            if (permissions.HasPermission("Governance", "Update")) {
                                tools.push({ icon: 'pencil', urlprefix: '/form/EditResponsibility?id={0}' });
                            }
                            if (permissions.HasPermission("Governance", "Delete")) {
                                tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteResponsibility?id={0}' });
                            }
                        }

                        return renderToolsHtml(value, tools, contextList.Responsibility, data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#endregion

    //#region Event Subscriptions

    function gridBindingComplete(event) {
        try {
            $(gridControlID).jqxGrid('sortby', 'Role', 'asc');
        } catch (e) { }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Artifact:
                case contextList.DomainList:
                case contextList.Responsibility:
                case contextList.PeopleResponsibility:
                case contextList.Taxonomy:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('bindingcomplete', gridBindingComplete)
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on("bindingcomplete", gridBindingComplete);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}