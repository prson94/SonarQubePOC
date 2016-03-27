function GroupMembersGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var html = "";
    html += '<header>Members<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>';
    $(controlID).html(html);
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    var source = {
        datatype: 'json',
        type: 'get',
        url: '/api/groups/' + id + '/resources',
        datafields:
        [
            { name: 'ResourceID', type: 'number' },
            { name: 'FirstName', type: 'string' },
            { name: 'LastName', type: 'string' },
            { name: 'Owner', type: 'string' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    var tools = [];
    if (permissions.HasPermission("Root", "Update")) {
        tools.push({ icon: 'plus', uri: '/form/AddGroupUser?id=' + id, context: contextList.ResourceGroup, title: 'Add member' });
    }
    TileTools(toolsControlID, tools);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            virtualmode: false,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            filterable: true,
            showfilterrow: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "LastName", text: "Last Name" },
                { datafield: "FirstName", text: "First Name" },
                { datafield: "Owner", text: "Owner?", width: 125 },
                {
                    datafield: "ResourceID",
                    text: "",
                    width: 80,
                    sortable: false,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { isitemlink: true, urlprefix: '#/resources/{0}', type: 'Group', context: 'Preview' }
                        ];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteGroupUser?groupID=' + id + '&resourceID={0}' });
                        }

                        return renderToolsHtml(value, tools, contextList.ResourceGroup, data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.ResourceGroup:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : GroupMembersGrid", e);
        }
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