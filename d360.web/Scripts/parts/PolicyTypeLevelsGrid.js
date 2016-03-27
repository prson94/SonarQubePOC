function PolicyTypeLevelsGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Levels<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/PolicyType/' + id + '/levels',
            datafields:
            [
                { name: 'PolicyTypeID' },
                { name: 'Name' },
                { name: 'Level' },
                { name: 'Description' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddPolicyTypeLevel?id=" + id, context: contextList.PolicyTypeLevel, title: 'Add level' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            autorowheight: true,
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
                { datafield: "Level", text: "Level", width: '10%' },
                { datafield: "Name", text: "Name", width: '30%' },
                { datafield: "Description", text: "Description" },
                {
                    text: '',
                    dataField: 'PolicyTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditPolicyTypeLevel?id=' + data.PolicyTypeID + '&level=' + data.Level });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeletePolicyTypeLevel?id=' + data.PolicyTypeID + '&level=' + data.Level });
                        }

                        return renderToolsHtml(value, tools, contextList.PolicyTypeLevel);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.PolicyTypeLevel:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : PolicyTypeLevelsGrid", e);
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