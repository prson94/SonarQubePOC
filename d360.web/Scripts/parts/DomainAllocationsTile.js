function DomainAllocationsTile(controlID, contextList, permissions, typeID, domainID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Usage</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var srcAllocationsGrid = {
        datatype: 'json',
        url: '/api/domains/' + typeID + '/' + domainID + '/allocations',
        datafields:
        [
            { name: 'AttributeTypeID', type: 'number' },
            { name: 'LocationType' },
            { name: 'Location' },
            { name: 'Type' },
            { name: 'Name' }
        ]
    };

    var adapterAllocationsGrid = new $.jqx.dataAdapter(srcAllocationsGrid);

    $(gridControlID).jqxGrid({
        altrows: true,
        width: grid_width,
        autoheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: true,
        pageable: true,
        pagesizeoptions: ['10', '20', '50'],
        pagesize: 20,
        source: adapterAllocationsGrid,
        theme: list_theme,
        columns: [
            { text: 'Object Type', dataField: 'Type', columntype: 'dropdownlist', filtertype: 'checkedlist' },
            { text: 'Object Name', dataField: 'Name' },
            { text: 'Location Type', dataField: 'LocationType', columntype: 'dropdownlist', filtertype: 'checkedlist' },
            { text: 'Location', dataField: 'Location' }
        ]
    });

    //#endregion

    //#region Event Subscriptions

    function unsubscribe(data) {
        srcAllocationsGrid = null;
        adapterAllocationsGrid = null;
    }

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}