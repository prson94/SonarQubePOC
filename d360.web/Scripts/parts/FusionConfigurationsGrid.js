function FusionConfigurationsGrid(controlID, contextList, permissions, type, id) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Configurations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    if (permissions.HasPermission("Root", "Update")) {
        TileTools(toolsControlID, [
            { icon: 'plus', uri: '/form/AddFusion?typeID=' + id, context: contextList.Fusion, title: 'Add configuration' }
        ]);
    }

    $.getJSON('/api/' + type + '/' + id + '/grid/definition', function (definition) {

        var source = {
            datatype: 'json',
            type: 'get',
            url: '/fusion/' + id + '/configurations',
            datafields: definition.Fields
        };

        var adapter = new $.jqx.dataAdapter(source);

        definition.Columns.push({
            text: '',
            dataField: 'ID',
            width: '160px',
            sortable: false,
            filterable: false,
            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                if (value != 0) {
                    var tools = [
                        { isitemlink: true, urlprefix: '#/fusion/' + definition.ID + '/{0}' }
                    ];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditFusion?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFusion?id={0}' });
                    }
                    tools.push({ icon: 'filter', urlprefix: '/overlays/FusionConfigurationFilters?fusionTypeID=' + id + '&fusionID={0}', title: 'View/modify synchronization filters' });

                    return renderToolsHtml(value, tools, contextList.Fusion);
                }
                else {
                    return "";
                }
            }
        });

        try {
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
                columnsresize: true,
                source: adapter,
                theme: theme,
                columns: definition.Columns
            });
        } catch (e) {
        }

        //#region Event Subscriptions

        function gridRowDoubleClick(event) {
            try {
                var args = event.args;
                var row = args.rowindex;
                var data = $(gridControlID).jqxGrid('getrowdata', row);
                location.assign('#/fusion/' + definition.ID + '/' + data.ID);
            } catch (e) {
                logError("Parts.js : FusionConfigurationsGrid", e);
            }
        }

        function pageResized() {
            $(gridControlID).jqxGrid('refresh');
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Fusion:
                        $(gridControlID).jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) {
                logError("Parts.js : FusionConfigurationsGrid", e);
            }
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;

            $(gridControlID).off('rowdoubleclick', gridRowDoubleClick);
            amplify.unsubscribe("PageResized", pageResized);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        $(gridControlID).on('rowdoubleclick', gridRowDoubleClick);
        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe("SaveAction", saveAction);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

        //#endregion
    });
}