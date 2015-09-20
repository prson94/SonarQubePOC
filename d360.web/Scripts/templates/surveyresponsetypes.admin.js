function surveyresponsetypes_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/surveyresponsetypes/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'ResponseType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Surveys' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var ResponseTypesSource;
        var ResponseTypesAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            $("#List").jqxGrid('selectrow', 0);
        }

        function listRowSelect(event) {
            var args = event.args;
            var row = args.rowindex;

            var data = $('#List').jqxGrid('getrowdata', row);

            if (data) {
                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools("reload", type, data.ID);
                DetailTile('DetailTile', contextList, permissions, type, data.ID);
                $('#OptionsTile').load('/resources/responsetypes/' + data.ID + '/options');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.ResponseType:
                        DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            ResponseTypesAdapter = null;
            ResponseTypesSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'surveyresponsetypes.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                permissions.GetPermissionsForObject(type, 0);

                //#region Grid

                ResponseTypesSource = {
                    datatype: 'json',
                    url: '/api/responsetypes',
                    datafields: [
                        { name: 'ID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'AllowOptions' },
                        { name: 'AllowValueOverride' }
                    ]
                };

                ResponseTypesAdapter = new $.jqx.dataAdapter(ResponseTypesSource);

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
                    source: ResponseTypesAdapter,
                    theme: theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        { datafield: "AllowOptions", text: "Allow Options?", width: 50, cellsrenderer: booleanrenderer },
                        { datafield: "AllowValueOverride", text: "Allow Override?", width: 50, cellsrenderer: booleanrenderer },
                        {
                            datafield: "ID",
                            text: "",
                            width: 80,
                            cellsrenderer: function (row, column, value) {

                                //var data = $("#List").jqxGrid('getrowdata', row);
                                var tools = [
                                    { icon: 'pencil', urlprefix: '/form/responsetypes/{0}/edit' },
                                    { icon: 'trash-o', urlprefix: '/form/responsetypes/{0}/delete' }
                                ];

                                return renderToolsHtml(value, tools, contextList.ResponseType);
                            }
                        }
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $("#List").on("bindingcomplete", listBindingComplete);
                $('#List').on('rowselect', listRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}