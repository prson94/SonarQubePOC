function RelationshipTypeTreeTile(controlID, permissions, type, id) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    var title = 'Relationship Types';

    try {
        $(controlID).html('<header>' + title + '<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        var source = {
            dataType: "json",
            dataFields: [
                { name: 'IntersectTypeID', type: 'number' },
                { name: 'TargetObjectType', type: 'string' },
                { name: 'TargetObjectID', type: 'number' },
                { name: 'TextPath', type: 'string' },
                { name: 'Level', type: 'number' },
                { name: 'relationships', type: 'array' },
                { name: 'predicates', type: 'array' }
                //{ name: 'expanded', type: 'bool' }
            ],
            hierarchy:
            {
                root: 'relationships'
            },
            //id: 'IntersectTypeID',
            url: '/relations/' + type + '/' + id + '/RelationshipTypeTree.json'
        };
        var dataAdapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddIntersectType?type=' + type + '&id=' + id, context: contextList.IntersectType, title: 'Add relationship type' }
            ]);
        }

        $(gridControlID).jqxTreeGrid({
            width: grid_width,
            pageable: true,
            pagerMode: 'advanced',
            pageSizeMode: 'root',
            pageSize: 10,
            pageSizeOptions: ['5', '10', '25'],
            theme: theme,
            source: dataAdapter,
            sortable: true,
            columns: [
                {
                    text: 'Name',
                    dataField: 'TextPath',
                    cellsRenderer: function (row, column, value, data) {
                        var html = "";
                        html += ((data.Level == 1) ? "<b>" : "") + data.TextPath + ((data.Level == 1) ? "</b>" : "");
                        return html;
                    }
                },
                {
                    text: 'Predicates', dataField: 'predicates', width: '40%', filterable: false,
                    cellsRenderer: function (row, column, value, data) {
                        var html = "";
                        if (data.predicates) {
                            html += "";//"<ul>";
                            $.each(data.predicates, function () {
                                html += ((html !== "") ? ", " : "") + this.Name;//"<li>" + this.Name + "</li>";
                            });
                            //html += "</ul>";
                        }
                        return html;
                    }
                },
                {
                    text: '', dataField: 'IntersectTypeID', width: '160px', filterable: false,
                    cellsRenderer: function (row, column, value, data) {
                        var tools = [];

                        if (permissions.HasPermission("Root", "Create")) {
                            tools.push({ icon: 'plus', urlprefix: '/form/AddIntersectType?type=IntersectType&id={0}', title: 'Add fusion relationship type' });
                        }
                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditPredicateAllocation?id={0}', title: 'Edit predicates' });
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditIntersectType?id={0}', title: 'Edit relationship type' });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteIntersectType?id={0}', title: 'Remove relationship type' });
                        }

                        return renderToolsHtml(value, tools, contextList.ArtifactType, data);
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
        //try {
        //    if (command == "FieldMove") {
        //        $(gridControlID).jqxTreeGrid('updateBoundData');
        //    }
        //} catch (e) {
        //    logError("Parts.js : FieldsGrid", e);
        //}
    }

    function pageResized() {
        $(gridControlID).jqxTreeGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.IntersectType:
                    $(gridControlID).jqxTreeGrid('updateBoundData');
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