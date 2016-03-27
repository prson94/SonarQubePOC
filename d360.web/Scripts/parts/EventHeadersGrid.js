function EventHeadersGrid(controlID, contextList, type, id, selectedGroupID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Event History</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var EventHeadersGridSource = {
        datatype: 'json',
        url: '/Monitor/EventHeaders?ruleID=' + id,
        datafields: [
            { name: 'ID', type: 'number' },
            { name: 'PublicID', type: 'string' },
            { name: 'Date', type: 'date' },
            { name: 'Rule', type: 'string' },
            { name: 'NumberOfEvents', type: 'number' },
            { name: 'Name', type: 'string' }
        ],
        beforeprocessing: function (data) {
            EventHeadersGridSource.totalrecords = data.total;
        },
        filter: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        sort: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        id: 'ID'
    };

    var EventHeadersGridAdapter = new $.jqx.dataAdapter(EventHeadersGridSource);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            virtualmode: true,
            pageable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            columnsresize: true,
            enabletooltips: true,
            rendergridrows: function () {
                return EventHeadersGridAdapter.records;
            },
            source: EventHeadersGridAdapter,
            theme: list_theme,
            columns: [
                { datafield: "Date", text: "Last Event Date", cellsformat: 'MM/dd/yyyy HH:mm:ss', columntype: "datetimeinput", filtertype: "range", width: 150 },
                { datafield: "Name", text: "Name" },
                { datafield: "NumberOfEvents", text: "# Events", filtertype: "number", width: 100 },
                { datafield: "PublicID", text: "Public ID", width: 250 },
                {
                    text: '',
                    dataField: 'ID',
                    width: '80px',
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { icon: 'info', urlprefix: '/overlays/EventGroup/' + data.ID + '/detail' },
                            { icon: 'pencil', urlprefix: '/form/EditEventGroup?id=' + data.ID }
                        ];
                        return renderToolsHtml(value, tools, contextList.EventGroup);
                    }
                }
            ],
            ready: function () {
                if (selectedGroupID) {
                    var selectedGroupIndex = $(gridControlID).jqxGrid('getrowboundindexbyid', selectedGroupID);
                    if (selectedGroupIndex > -1) {
                        $(gridControlID).jqxGrid('selectrow', selectedGroupIndex);
                    }
                }
            }
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function gridRowSelect(event) {
        var args = event.args;              // event arguments.
        var rowBoundIndex = args.rowindex;  // row's bound index.
        var rowData = args.row;             // row's data.

        amplify.publish('EventHeaderSelected', {
            GroupID: rowData.ID
        });
    }

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
            logError("Parts.js : EventHeadersGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on('rowselect', gridRowSelect);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}