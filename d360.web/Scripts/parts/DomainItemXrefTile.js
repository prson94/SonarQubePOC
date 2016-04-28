function DomainItemXrefTile(controlID, contextList, permissions, domainItemID) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Cross Reference Items<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    var srcDomainXrefItemsGrid = {
        datatype: 'json',
        url: '/services/domains/lists/xref/' + domainItemID,
        datafields:
        [
            {name: 'ID' },
            { name: 'HouseDomainItemID' },
            { name: 'DomainItemID' },
            { name: 'HouseCode' },
            { name: 'Code' },
            { name: 'SourceArtifactID' },
            { name: 'SourceArtifactName' },
            { name: 'ListName' }
        ]
    };

    var adapterDomainXrefItemsGrid = new $.jqx.dataAdapter(srcDomainXrefItemsGrid);

    var tools = [];
    if (permissions.HasPermission("Root", "Update")) {
        tools.push({ icon: 'plus', uri: '/form/AddDomainXrefItem?domainItemID=' + domainItemID, context: contextList.DomainXrefItem, title: 'Add xref item' });
    }
    TileTools(toolsControlID, tools);

    $(gridControlID).jqxGrid({
        altrows: true,
        width: grid_width,
        autoheight: true,
        autorowheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: true,
        pageable: true,
        pagesizeoptions: ['10', '20', '50'],
        pagesize: 20,
        source: adapterDomainXrefItemsGrid,
        theme: list_theme,
        columns: [
            { text: 'House Code', datafield: 'HouseCode', width: 100 },
            { text: 'Source', datafield: 'SourceArtifactName' },
            { text: 'List', datafield: 'ListName' },
            { text: 'Code', datafield: 'Code' }
            ,{
                text: '',
                dataField: 'ID',
                width: 80,
                filterable: false,
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                    var tools = [];

                    if (permissions.HasPermission("Root", "Update")) {
                       // tools.push({ icon: 'pencil', urlprefix: '/form/EditDomainXrefItem?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteDomainItemXref?id={0}' });
                    }
                    return renderToolsHtml(value, tools, contextList.DomainXrefItem);
                }
            }
        ]
    });

    //#endregion

    //#region Event Subscriptions

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.DomainXrefItem:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("DomainItemsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        srcDomainXrefItemsGrid = null;
        adapterDomainXrefItemsGrid = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}