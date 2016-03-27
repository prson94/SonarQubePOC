function StatisticTypeAllocationGrid(controlID, contextList, permissions, id) {

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
            url: '/api/StatisticType/' + id + '/allocations',
            datafields:
            [
                { name: 'ObjectID' },
                { name: 'StatisticTypeID' },
                { name: 'ObjectName' },
                { name: 'ObjectType' },
                { name: 'Score' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        TileTools(toolsControlID, [
            { icon: 'plus', uri: "/form/AddStatisticTypeRelation?id=" + id, context: contextList.AttributeTypeRelation, title: 'Add allocation' }
        ]);

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
                { text: 'Score', dataField: 'Score', width: 75 },
                {
                    text: '',
                    dataField: 'StatisticTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { icon: 'pencil', urlprefix: '/form/EditStatisticTypeRelation?id=' + data.StatisticTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID },
                            { icon: 'trash-o', urlprefix: '/form/DeleteStatisticTypeRelation?id=' + data.StatisticTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID }
                        ];
                        return renderToolsHtml(value, tools, contextList.StatisticTypeRelation);
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
                case contextList.StatisticTypeRelation:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : StatisticTypeAllocationsGrid", e);
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