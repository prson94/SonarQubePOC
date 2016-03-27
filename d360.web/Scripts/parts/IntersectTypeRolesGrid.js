function IntersectTypeRolesGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Available Roles<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/relationshiptypes/' + id + '/roles',
            datafields:
            [
                { name: 'ID' },
                { name: 'IntersectTypeID' },
                { name: 'Name' },
                { name: 'Side1Label' },
                { name: 'Side2Label' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddIntersectTypeRole?intersectTypeID=' + id, context: contextList.IntersectTypeRole, title: 'Add role' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "Name", text: "Name", width: '40%' },
                { datafield: "Side1Label", text: "Side 1 Label", width: '25%' },
                { datafield: "Side2Label", text: "Side 2 Label", width: '25%' },
                {
                    text: '', dataField: 'ID', width: '10%', filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditIntersectTypeRole?id={0}&intersectTypeID=' + data.IntersectTypeID });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteIntersectTypeRole?id={0}&intersectTypeID=' + data.IntersectTypeID });
                        }

                        return renderToolsHtml(value, tools, contextList.IntersectTypeRole);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function commandExecuted(command) {
        if (command == "FieldMove") {
            $(gridControlID).jqxGrid('updatebounddata');
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("CommandExecuted", commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe('CommandExecuted', commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}