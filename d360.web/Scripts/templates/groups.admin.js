function groups_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/groups/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'Group';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Security' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var GroupsAdapter;
        var GroupsSource;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var data = event.args.row;

            //#region Tiles

            amplify.publish(AmplifyActions.TileUnsubscribe, {});
            GroupMembersGrid("MembersTile", contextList, permissions, data.ID);

            //#endregion

            $('#SideIcons').PageTools("reload", type, data.ID, 'root');
        }

        function refreshActionMenu(data) {
            $('#SideIcons').PageTools('refresh');
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Group:
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            GroupsAdapter = null;
            GroupsSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $("#List").off("rowselect", listRowSelect);
            amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'groups.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0, context: 'root' });

                var loadPermissionsDependentTiles = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddGroup', context: contextList.Group, title: 'Add group' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    GroupsSource = {
                        datatype: 'json',
                        url: '/api/groups',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' }
                        ]
                    };

                    GroupsAdapter = new $.jqx.dataAdapter(GroupsSource);

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
                        source: GroupsAdapter,
                        theme: theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                sortable: false,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [
                                        { isitemlink: true, urlprefix: '#/groups/{0}' }
                                    ];
                                    if (permissions.HasPermission("Root", "Update")) {
                                        tools.push({ icon: 'pencil', urlprefix: '/form/EditGroup?id={0}' });
                                    }
                                    if (permissions.HasPermission("Root", "Delete")) {
                                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteGroup?id={0}' });
                                    }

                                    return renderToolsHtml(value, tools, contextList.Group);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $("#List").on("bindingcomplete", listBindingComplete);
                    $("#List").on("rowselect", listRowSelect);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion

                }

                permissions.GetPermissionsForObject(type, 0).then(loadPermissionsDependentTiles);
            });
    });
}