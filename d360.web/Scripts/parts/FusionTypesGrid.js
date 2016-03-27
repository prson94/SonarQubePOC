function FusionTypesGrid(controlID, contextList, permissions) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + gridControlID + '"></div>'); //<header>Types</header>
    gridControlID = '#' + gridControlID;

    var source = {
        datatype: 'json',
        type: 'get',
        url: '/services/fusion?$orderby=Name',
        datafields: [
            { name: 'ID', type: 'number' },
            { name: 'Name', type: 'string' },
            { name: 'Description', type: 'string' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesize: 10,
            pageable: true,
            pagermode: 'simple',
            columnsresize: true,
            source: adapter,
            theme: theme,
            ready: function () {
                var rowCount = $(gridControlID).jqxGrid('getdisplayrows').length;
                if (rowCount > 0) {
                    $(gridControlID).jqxGrid('selectrow', 0);
                }
            },
            columns: [
                { text: 'Name', dataField: 'Name' },
                {
                    text: '',
                    dataField: 'ID',
                    width: 80,
                    filterable: false,
                    sortable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditFusionType?id={0}' });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFusionType?id={0}' });
                        }
                        return renderToolsHtml(value, tools, contextList.FusionType);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function gridRowSelect(event) {
        try {
            amplify.publish('FusionTypeSelected', event.args.row);
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
                case contextList.FusionType:
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

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on('rowselect', gridRowSelect);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}