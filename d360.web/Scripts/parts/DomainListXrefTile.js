function DomainListXrefTile(controlID, contextList, permissions, domainID) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Cross Reference Lists</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var domains = {
        datatype: 'json',
        url: '/services/domains/lists/domain/' + domainID,
        datafields: [
            { name: 'ID' },
            { name: 'List' }
        ]
    };

    var domainAdapter = new $.jqx.dataAdapter(domains);


    $(gridControlID).jqxDataTable({
        theme: theme,
        width: field_width98,
        height: 150,
        source: domainAdapter,
        columnsResize: true,
        columns: [
          { text: 'List', dataField: 'List' }
        ]
    });

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Domain:
                    $(gridControlID).jqxGrid('updateBoundData');
                    break;
            }
        } catch (e) {
            logError("DomainItemsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        domains = null;
        domainAdapter = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}