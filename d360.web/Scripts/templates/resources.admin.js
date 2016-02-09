function resources_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/resources/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'ResourceType';
        var typeID = 1;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Security' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var UsersSource;
        var UsersAdapter;

        //#region Event Handlers

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.FieldType:
                    case contextList.Resource:
                        $('#Users').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) {
                logError("UserList : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            UsersAdapter = null;
            UsersSource = null;

            amplify.unsubscribe('SaveAction', saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'resources.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: typeID });

                var loadPermissionsDependentTiles = function () {
                    FieldsGrid("FieldsTile", contextList, permissions, type, typeID);

                    //#region Grid

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/resources/1/add', context: contextList.Resource, title: 'Add resource' });
                    }
                    TileTools('#UsersTools', tools);

                    $.getJSON('/api/' + type + '/' + typeID + '/grid/definition', function (data) {

                        UsersSource = {
                            datatype: 'json',
                            url: '/api/resources/' + typeID + "?$orderby=LastName,FirstName",
                            datafields: data.Fields
                        };

                        UsersAdapter = new $.jqx.dataAdapter(UsersSource);

                        data.Columns.push({
                            text: '',
                            dataField: 'ResourceID',
                            width: 160,
                            sortable: false,
                            filterable: false,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                if (value != 0) {
                                    var tools = [
                                        { isitemlink: true, urlprefix: '#/resources/{0}' }
                                    ];

                                    if (permissions.HasPermission('Root', 'Update')) {
                                        tools.push({ icon: 'pencil', urlprefix: '/form/resources/1/{0}/edit' });
                                        tools.push({ icon: 'asterisk', urlprefix: '/form/resources/1/{0}/password' });
                                    }
                                    if (permissions.HasPermission('Root', 'Delete')) {
                                        tools.push({ icon: 'trash-o', urlprefix: '/form/resources/1/{0}/delete' });
                                    }

                                    return renderToolsHtml(value, tools, contextList.Resource);
                                }
                                else {
                                    return "";
                                }
                            }
                        });

                        $("#Users").jqxGrid({
                            altrows: true,
                            width: grid_width,
                            autoheight: true,
                            sortable: true,
                            filterable: true,
                            showfilterrow: true,
                            pagesizeoptions: ['10', '20', '50'],
                            pagesize: 20,
                            pageable: true,
                            source: UsersAdapter,
                            columnsresize: true,
                            theme: list_theme,
                            columns: data.Columns
                        });
                    });

                    //#endregion
                }
                permissions.GetPermissionsForObject(type, typeID).then(loadPermissionsDependentTiles);



                //#region Event Subscriptions

                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}