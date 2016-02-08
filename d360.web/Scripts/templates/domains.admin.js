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
                    DetailTile('DetailTile', contextList, permissions, type, data.ID);
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

                permissions.GetPermissionsForObject(type, 0);

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
                                    { isitemlink: true, urlprefix: '#/domains/{0}' },
                                    { icon: 'pencil', urlprefix: '/form/domains/{0}/edit' },
                                    { icon: 'trash-o', urlprefix: '/form/domains/{0}/delete' }
                                ];
                                return renderToolsHtml(value, tools, contextList.DomainType);
                            }
                        }
                    ]
                });

                //#endregion

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