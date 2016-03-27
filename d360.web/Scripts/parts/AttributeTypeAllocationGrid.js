function AttributeTypeAllocationGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Allocations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/AttributeType/' + id + '/allocations',
            datafields:
            [
                { name: 'ObjectID' },
                { name: 'AttributeTypeID' },
                { name: 'ObjectName' },
                { name: 'ObjectType' },
                { name: 'AllowMultipleEntries' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddAttributeTypeRelation?id=" + id, context: contextList.AttributeTypeRelation, title: 'Add allocation' }
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
                { text: 'Object Type', dataField: 'ObjectType' },
                { text: 'Object Name', dataField: 'ObjectName' },
                { text: 'Allow Multiple Entries?', dataField: 'AllowMultipleEntries', width: 125, cellsrenderer: booleanrenderer },
                {
                    text: '',
                    dataField: 'AttributeTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditAttributeTypeRelation?id=' + data.AttributeTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteAttributeTypeRelation?id=' + data.AttributeTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID });
                        }

                        return renderToolsHtml(value, tools, contextList.AttributeTypeRelation);
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
                case contextList.AttributeTypeRelation:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : AttributeTypeAllocationsGrid", e);
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
