function LookupTypeItemsGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Event Methods

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    unsubscribe();
                    $(toolsControlID).html('');
                    loadGridConfiguration();
                    break;
                case contextList.Lookup:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : LookupTypeItemsGrid", e);
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

    //#endregion

    function loadGridConfiguration() {
        $.getJSON('/api/LookupType/' + id + '/grid/definition', function (gridinfo) {

            source = {
                datatype: 'json',
                url: '/resources/lookups/' + id + '/items.json',
                datafields: gridinfo.Fields,
            };

            adapter = new $.jqx.dataAdapter(source);

            if ((gridinfo.FieldsCount > 0) && permissions.HasPermission("Root", "Update")) {
                TileTools(toolsControlID, [
                    { icon: 'plus', uri: '/form/AddLookup?id=' + id, context: contextList.Lookup, title: 'Add item' }
                ]);
            }

            gridinfo.Columns.push({
                text: '', dataField: 'ID', width: '10%', filterable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditLookup?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteLookup?id={0}' });
                    }

                    return renderToolsHtml(value, tools, contextList.Lookup);
                }
            });

            try {
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
                    columns: gridinfo.Columns
                });
            } catch (e) { }

            //#region Event Subscriptions
            amplify.subscribe("PageResized", pageResized);
            amplify.subscribe("SaveAction", saveAction);
            amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            //#endregion
        });
    }

    //#region Grid

    try {
        $(controlID).html('<header>Items<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        loadGridConfiguration();

    } catch (e) {
        console.log(e);
    }

    //#endregion
}