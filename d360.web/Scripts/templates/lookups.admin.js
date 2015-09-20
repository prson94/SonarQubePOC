function lookups_admin(app, pageViewModel, templatePath, contextList) {
    var routeLookup = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var selectedID = context.params['id'];
        var type = 'LookupType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var LookupTypeSource;
        var LookupTypeAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            if (selectedID) {
                try {
                    var lookupRows = $('#List').jqxGrid('getboundrows');
                    for (var i in lookupRows) {
                        var rowData = lookupRows[i];
                        if (rowData.ID == selectedID) {
                            var selectedIndex = i;
                            $('#List').jqxGrid('ensurerowvisible', selectedIndex);
                            $('#List').jqxGrid('selectrow', selectedIndex);
                            break;
                        }
                    }
                    lookupRows = null;
                } catch (e) {
                    logError("Lookups Administration", e);
                }
            }
            else {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;
            var data = args.row;

            amplify.publish(AmplifyActions.TileUnsubscribe, {});

            if (data) {
                $('#SideIcons').PageTools("reload", type, data.ID);
                LookupTypeItemsGrid('ItemsTile', contextList, permissions, data.ID);
                FieldsGrid("FieldsTile", contextList, permissions, type, data.ID, 'Lookup Definition');
            }
            else {
                $('#SideIcons').PageTools("reload", type, 0);
                $('#LevelsTile').html('');
                $('#FieldsTile').html('');
                $('#ClaimsTile').html('');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.LookupType:
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            LookupTypeAdapter = null;
            LookupTypeSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $("#List").off("rowselect", listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'lookups.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {
                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddLookupType', context: contextList.LookupType, title: 'Add lookup' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    LookupTypeSource = {
                        datatype: 'json',
                        url: '/resources/_Lookups',
                        id: 'ID',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'ItemCount' },
                            { name: 'DateCreated' },
                            { name: 'DateUpdated' }
                        ]
                    };

                    LookupTypeAdapter = new $.jqx.dataAdapter(LookupTypeSource);

                    $("#List").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: LookupTypeAdapter,
                        theme: theme,
                        columns: [
                            { datafield: "ID", text: "ID", width: 80 },
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ItemCount',
                                width: 80,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                                    var tools = [];

                                    if (permissions.HasPermission('Root', 'Update')) {
                                        tools = [
                                            { icon: 'pencil', urlprefix: '/form/EditLookupType?id=' + data.ID },
                                            { icon: 'trash-o', urlprefix: '/form/DeleteLookupType?id={0}' + data.ID }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.LookupType);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $("#List").one("bindingcomplete", listBindingComplete);
                    $("#List").on("rowselect", listRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    }

    app.get('#/lookups/administration/:id', routeLookup);
    app.get('#/lookups/administration', routeLookup);
}