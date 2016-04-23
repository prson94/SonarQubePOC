function EventsGrid(controlID, contextList, id, selectedEventID, showCommands, hideTitle) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    var html = "";
    if (!hideTitle) {
        html += "<header>Event Details</header>";
    }
    html += '<div id="' + gridControlID + '"></div>';
    $(controlID).html(html);
    gridControlID = '#' + gridControlID;

    $.getJSON('/api/EventGroup/' + id + '/grid/definition', function (gridinfo) {

        var src = {
            datatype: 'json',
            url: '/Monitor/EventsByHeader?groupID=' + id,
            datafields: gridinfo.Fields,
            beforeprocessing: function (data) {
                src.totalrecords = data.total;
            },
            filter: function () {
                $(gridControlID).jqxGrid('updatebounddata');
            },
            sort: function () {
                $(gridControlID).jqxGrid('updatebounddata');
            },
            id: 'ID'
        };

        var adapter = new $.jqx.dataAdapter(src);

        if (showCommands) {
            gridinfo.Columns.push({
                datafield: "ID",
                text: "",
                sortable: false,
                filterable: false,
                resizable: false,
                width: '40px',
                resizable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    //tools.push({ icon: 'info', urlprefix: '/overlays/Event/' + data.ID + '/detail' });
                    tools.push({ icon: 'pencil', urlprefix: '/form/EditEvent?id=' + data.ID });

                    return renderToolsHtml(value, tools, contextList.Event, data);
                }
            });
        }

        try {
            $(gridControlID).jqxGrid({
                width: grid_width,
                pagesizeoptions: ['5', '10', '20', '50'],
                pagesize: 20,
                autoheight: true,
                autorowheight: true,
                sortable: true,
                altrows: true,
                filterable: true,
                showfilterrow: true,
                virtualmode: true,
                rendergridrows: function () {
                    return adapter.records;
                },
                pageable: true,
                columnsresize: true,
                source: adapter,
                theme: list_theme,
                columns: gridinfo.Columns,
                columngroups: (gridinfo.ColumnGroups.length > 0) ? gridinfo.ColumnGroups : null,
                ready: function () {
                    if (selectedEventID) {
                        var selectedEventIndex = $(gridControlID).jqxGrid('getrowboundindexbyid', selectedEventID);
                        if (selectedEventIndex > -1) {
                            $(gridControlID).jqxGrid('selectrow', selectedEventIndex);
                        }
                    }
                    //$(gridControlID).jqxGrid('autoresizecolumns');
                }
            });
        } catch (e) {

        }
    });

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Event:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : EventsGrid", e);
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