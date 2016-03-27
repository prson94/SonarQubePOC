function FieldsGrid(controlID, contextList, permissions, type, id, title) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var displayTools = ((type != 'AttributeType') || (type == 'AttributeType' && id >= 50000));

    var source;
    var adapter;

    //#region Grid

    if (!title || title <= '') {
        title = 'Field Definition';
    }

    try {
        $(controlID).html('<header>' + title + '<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/fields/' + type + '/' + id + '.json',
            datafields:
            [
                { name: 'ObjectType' },
                { name: 'ObjectID' },
                { name: 'ID' },
                { name: 'Category' },
                { name: 'FriendlyName' },
                { name: 'SortOrder' },
                { name: 'IsRequired' },
                { name: 'IsListable' },
                { name: 'DisplayDescription' },
                { name: 'FormDescription' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (displayTools && permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddFieldType?type=' + type + '&id=' + id, context: contextList.FieldType, title: 'Add definition attribute' }
            ]);
        }

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
                { datafield: "FriendlyName", text: "Field" },
                { datafield: "Category", text: "Category", filtertype: 'checkedlist', width: 125 },
                //{ datafield: "SortOrder", text: "Order", columntype: 'numberinput', filtertype: 'number', width: 70 },
                { datafield: "IsRequired", text: "Required?", columntype: 'checkbox', filtertype: 'bool', width: 70 },
                { datafield: "IsListable", text: "Listable?", columntype: 'checkbox', filtertype: 'bool', width: 70 },
                {
                    text: '', dataField: 'ID', width: '150px', filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (displayTools) {
                            if (permissions.HasPermission("Root", "Update")) {
                                tools.push({ icon: 'caret-up', urlprefix: '/fields/' + data.ObjectType + '/' + data.ObjectID + '/' + data.ID + '/move/up', context: 'action' });
                                tools.push({ icon: 'caret-down', urlprefix: '/fields/' + data.ObjectType + '/' + data.ObjectID + '/' + data.ID + '/move/down', context: 'action' });
                                tools.push({ icon: 'pencil', urlprefix: '/form/EditFieldType?id={0}' });
                                tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFieldType?id={0}' });
                            }
                        }

                        return renderToolsHtml(value, tools, contextList.FieldType);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function commandExecuted(command) {
        try {
            if (command == "FieldMove") {
                $(gridControlID).jqxGrid('updatebounddata');
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("CommandExecuted", commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe('CommandExecuted', commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}
