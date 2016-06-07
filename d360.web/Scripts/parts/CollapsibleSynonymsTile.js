function CollapsibleSynonymsTile(controlID, contextList, permissions, type, id) {
    var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';
    var toolsControlID = controlID + "_tools";
    var source;
    var adapter;

    //#region Event Handlers

    function bindingComplete(event) {
        var count = 0;
        try {
            count = $('#' + controlID_sub).jqxGrid('getrows').length;
        } catch (e) {
            count = 0;
        }
        $('#' + controlID_count).html("&#160;(<b>" + count + "</b>)");
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    if (data.custom) {
                        if (data.custom.AttributeTypeID === 1) {
                            $('#' + controlID_sub).jqxGrid('updatebounddata');
                        }
                    }
                    break;
                case contextList.Synonym:
                    $('#' + controlID_sub).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : SynonymsTile : SaveAction", e);
        }
    }

    function expanded() {
        $('#' + controlID_sub).jqxGrid('updatebounddata');
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $('#' + controlID_sub).off('bindingcomplete', bindingComplete);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        $('#' + controlID).off('expanded', expanded);
    }

    //#endregion

    //#region Clean up previous control logic before re-creating

    var exists = false;
    try {
        var exp = $('#' + controlID).jqxExpander('animationType');
        if (exp) {
            exists = true;
        }
    } catch (e) { }

    //#endregion

    if (!exists) {
        $('#' + controlID).css('margin', '10px');
        $('#' + controlID).html('<div>Synonyms<span id="' + controlID_count + '"></span></div><div><header style="width: 98%; margin-top: 10px"><div id="' + toolsControlID + '"></div></header><div id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });

        if (permissions.HasPermission("Relationship", "Create")) {
            toolsControlID = '#' + toolsControlID;
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddSynonym?type=' + type + '&id=' + id, context: contextList.Synonym, title: 'Add synonym' }
            ]);
        }
    }

    //#region Grid

    try {
        source = {
            datatype: 'json',
            url: '/api/' + type + '/' + id + '/synonyms',
            datafields:
            [
                { name: 'IntersectID' },
                { name: 'IntersectMapID' },
                { name: 'Object' },
                { name: 'ObjectID' },
                { name: 'Name' },
                { name: 'Description' },
                { name: 'ObjectTypeName' },
                { name: 'Url' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        $('#' + controlID_sub).jqxGrid({
            source: adapter,
            width: overlay_grid_width,
            pagesizeoptions: ['5', '10', '20'],
            pagesize: 5,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            altrows: true,
            showfilterrow: false,
            filterable: true,
            pageable: false,
            theme: 'flat',
            autoshowloadelement: false,
            selectionmode: 'none',
            columns: [
                { datafield: "ObjectTypeName", text: "Type", width: '200px' },
                {
                    datafield: "Name",
                    text: "Name",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return previewLinkRenderer(data.Object, data.ObjectID, data.Url, data.Name);
                    }
                },
                //{ datafield: "Description", text: "Description" },
                {
                    datafield: "IntersectID",
                    text: "",
                    sortable: false,
                    filterable: false,
                    width: '80px',
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        tools.push({ isitemlink: true, urlprefix: data.Url, type: data.Object, id: data.ObjectID, context: 'Preview' });
                        if (permissions.HasPermission("Relationship", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: 'form/DeleteSynonym?type=' + data.Object + '&id=' + data.ObjectID + '&intersectMapID=' + data.IntersectMapID });
                        }
                        return renderToolsHtml(value, tools, contextList.Synonym, data);
                    }
                }
            ]
        });
    }
    catch (e) {

    }

    //#endregion

    //#region Register Events

    $('#' + controlID_sub).on('bindingcomplete', bindingComplete);
    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
}