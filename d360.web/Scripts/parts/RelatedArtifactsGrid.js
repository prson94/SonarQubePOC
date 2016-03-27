function RelatedArtifactsGrid(controlID, permissions, typeName, typeID, id) {

    controlID = '#' + controlID;
    var gridID = '#RelatedArtifactsTileGrid';
    var textID = '#RelatedArtifactsTileText';

    var source = $("#relatedArtifactsTileTmpl").html();
    var template = Handlebars.compile(source);
    $(controlID).html(template({ Title: typeName }));

    //#region Grid

    var source = {
        datatype: 'json',
        type: 'get',
        datafields: [
            { name: "ID" },
            { name: 'Name' },
            { name: 'Url' }
        ],
        url: '/queries/RelatedArtifacts?artifactID=' + id
    };

    var adapter = new $.jqx.dataAdapter(source);

    $(gridID).jqxGrid({
        source: adapter,
        width: grid_width,
        pagesizeoptions: ['5', '10', '20'],
        pagesize: 5,
        autoheight: true,
        sortable: true,
        altrows: true,
        showheader: false,
        showfilterrow: false,
        filterable: false,
        pageable: false,
        theme: list_theme,
        columns: [
            {
                datafield: "Name",
                text: "Name",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return previewLinkRenderer('Artifact', data.ID, data.Url, data.Name);
                }
            },
            {
                datafield: "ID",
                text: "",
                width: '40px',
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    if (permissions.HasPermission("Relationship", "Delete")) {
                        tools.push({ icon: 'trash-o', urlprefix: '/form/RelatedArtifact/' + id + '/' + data.ID, context: 'action', method: 'DELETE' });
                        tools.push();
                        tools.push();
                        tools.push();
                    }
                    return renderToolsHtml(value, tools, 'action');
                }
            }
        ]
    });

    //#endregion

    //#region Textbox

    var relatedArtifactsDataSource = {
        datatype: "json",
        datafields: [
            { name: 'ID' },
            { name: 'Name' }
        ],
        url: '/queries/RelatedArtifactOptions?typeID=' + typeID + '&artifactID=' + id
    };
    var relatedArtifactsDataAdapter = new $.jqx.dataAdapter(relatedArtifactsDataSource);

    $(textID).jqxInput({ disabled: !permissions.HasPermission("Relationship", "Create"), source: relatedArtifactsDataAdapter, placeHolder: "Search for a " + typeName.toLowerCase() + " to relate...", displayMember: "Name", valueMember: "ID", width: '100%', height: 25 });

    //#endregion

    //#region Events

    function commandExecuted(command) {
        if (command == "RelatedArtifactAdded") {
            $(gridID).jqxGrid('updatebounddata');
            $(textID).jqxInput('val', '');
        }
        if (command == "RelatedArtifactDeleted") {
            $(gridID).jqxGrid('updatebounddata');
        }
    }

    function pageResized() {
        $(gridID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case 'RelatedArtifacts':
                    $(gridID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : RelatedArtifactsGrid : SaveAction", e);
        }
    }

    function textSelect(event) {
        if (event.args) {
            var item = event.args.item;
            if (item) {
                amplify.publish("ToolAction", { context: 'action', customdata: { method: 'POST' }, uri: '/form/RelatedArtifact/' + id + '/' + item.value });
            }
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe('CommandExecuted', commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        $(textID).off('select', textSelect);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("CommandExecuted", commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    $(textID).on('select', textSelect);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}