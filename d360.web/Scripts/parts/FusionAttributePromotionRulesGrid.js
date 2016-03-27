function FusionAttributePromotionRulesGrid(controlID, contextList, permissions, typeID, fusionID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Promotion Rules</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var srcPromotionRulesGrid = {
        datatype: 'json',
        url: '/services/fusion/' + typeID + '/configurations/' + fusionID + '/promotionrules',
        datafields:
        [
            { name: 'ID' },
            { name: 'ObjectName' },
            { name: 'ObjectType' },
            { name: 'ParentName' },
            { name: 'ParentObjectType' },
            { name: 'PromotionName' },
            { name: 'PromotionObjectType' },
            { name: 'PromotionParentName' },
            { name: 'PromotionParentObjectType' },
            { name: 'Enabled' }
        ]
    };

    var adapterPromotionRulesGrid = new $.jqx.dataAdapter(srcPromotionRulesGrid);

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
        source: adapterPromotionRulesGrid,
        theme: list_theme,
        columns: [
            { text: 'Name', dataField: 'ObjectName' },
            { text: 'Parent', dataField: 'ParentName' },
            { text: 'Promote To', dataField: 'PromotionName' },
            { text: 'Parent to Promote To', dataField: 'PromotionParentName' },
            {
                text: '',
                dataField: 'ID',
                width: 80,
                filterable: false,
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var url = '/form/domains/' + typeID + '/' + fusionID + '/items/{0}/';
                    var tools = [
                        { icon: 'pencil', urlprefix: url + 'edit' },
                        { icon: 'trash-o', urlprefix: url + 'delete' }
                    ];

                    return renderToolsHtml(value, tools, contextList.DomainItem);
                }
            }
        ]
    });

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
            logError("FusionAttributePromotionRulesGrid : SaveAction", e);
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