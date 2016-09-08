function domains_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/domains/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
        var type = 'DomainType';
        var id = 0;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var DomainTypesSource;
        var DomainTypesAdapter;

        //#region Event Handlers

        function refreshActionMenu(data) {
            $('#SideIcons').PageTools({ type: type, id: id });
        }

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;
            var data = args.row;

            amplify.publish(AmplifyActions.TileUnsubscribe, {});
            if (data) {
                id = data.ID;
                $('#SideIcons').PageTools("reload", type, data.ID);
                var loadPermissionsDependentTiles = function () {
                    ObjectDetail('DetailTile', type, data.ID);
                    FieldsGrid("FieldsTile", contextList, permissions, type, data.ID);
                    PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, data.ID, 'Default Responsibilities', true);
                }
                permissions.GetPermissionsForObject(type, data.ID).then(loadPermissionsDependentTiles);
            }
            else {
                $('#SideIcons').PageTools("reload", type, 0);
                $('#DetailTile').html('');
                $('#GovernanceTile').html('');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.DomainType:
                        $('#List').jqxGrid('updatebounddata');
                        amplify.publish("RefreshNavigation");
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            DomainTypesAdapter = null;
            DomainTypesSource = null;

            $("#List").off("rowselect", listRowSelect);
            $("#List").off("bindingcomplete", listBindingComplete);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
        }

        //#endregion

        context
            .render(templatePath + 'domains.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                //#region Grid

                DomainTypesSource = {
                    datatype: 'json',
                    url: '/services/domains',
                    datafields:
                    [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'Description' }
                    ]
                };

                DomainTypesAdapter = new $.jqx.dataAdapter(DomainTypesSource);

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/domains/add', context: contextList.DomainType, title: 'Add reference type' });
                    }
                    TileTools('#ListTools', tools);

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
                        source: DomainTypesAdapter,
                        theme: theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [
                                        { isitemlink: true, urlprefix: '#/domains/{0}' }
                                    ];

                                    if (permissions.HasPermission("Root", "Update")) {
                                        tools.push({ icon: 'pencil', urlprefix: '/form/domains/{0}/edit' });
                                    }
                                    if (permissions.HasPermission("Root", "Delete")) {
                                        tools.push({ icon: 'trash-o', urlprefix: '/form/domains/{0}/delete' });
                                    }

                                    return renderToolsHtml(value, tools, contextList.DomainType);
                                }
                            }
                        ]
                    });

                }

                //#endregion

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);

                //#region Event Subscriptions

                $("#List").on("rowselect", listRowSelect);
                $("#List").one("bindingcomplete", listBindingComplete);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                amplify.subscribe("RefreshActionMenu", refreshActionMenu);

                //#endregion
            });
    });
}