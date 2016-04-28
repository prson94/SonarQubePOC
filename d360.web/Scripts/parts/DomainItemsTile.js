function DomainItemsTile(controlID, contextList, permissions, typeID, domainID) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Items<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    var srcDomainItemsGrid = {
        datatype: 'json',
        url: '/services/domains/' + typeID + '/lists/' + domainID,
        datafields:
        [
            { name: 'ID' },
            { name: 'Name' },
            { name: 'Code' },
            { name: 'Description' }
        ]
    };

    var adapterDomainItemsGrid = new $.jqx.dataAdapter(srcDomainItemsGrid);

    var tools = [];
    if (permissions.HasPermission("Root", "Update")) {
        tools.push({ icon: 'plus', uri: '/form/AddDomainItem?typeID=' + typeID + '&listID=' + domainID, context: contextList.DomainItem, title: 'Add domain item' });
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
        source: adapterDomainItemsGrid,
        theme: list_theme,
        columns: [
            { text: 'Name', dataField: 'Name' },
            { text: 'Code', dataField: 'Code' },
            { text: 'Description', dataField: 'Description' },
            {
                text: '',
                dataField: 'ID',
                width: 80,
                filterable: false,
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                    var tools = [];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditDomainItem?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteDomainItem?id={0}' });
                    }

                    return renderToolsHtml(value, tools, contextList.DomainItem);
                }
            }
        ]
    }).on('bindingcomplete', function () {
        $(this).jqxGrid('selectrow', 0);
    });


    function itemSelect(evt) {
        var args = evt.args;
        var row = args.row;
        
        if (!row) {
            $('#XrefTile').html('').fadeOut(300);
            return;
        }
        var oID = row.ID;

        if (oID && oID > 0) {
            DomainItemXrefTile('XrefTile', contextList, permissions, oID);
            $('#XrefTile').fadeIn(300);

        } else {
            $('#XrefTile').html('').fadeOut(300);
        }
        
    }

    $(gridControlID).on('rowselect', itemSelect);
    //#endregion

    //#region Event Subscriptions

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.DomainItem:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("DomainItemsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        srcDomainItemsGrid = null;
        adapterDomainItemsGrid = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}