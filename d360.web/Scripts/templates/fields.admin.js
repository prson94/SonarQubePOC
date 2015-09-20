function fields_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/fields/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
        
        var type = 'FieldType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var FieldTypesSource;
        var FieldTypesAdapter;

        //#region Event Handlers

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

            $('#SideIcons').PageTools("reload", type, data.ID);
            DetailTile('DetailTile', contextList, permissions, type, data.ID);
            $('#AllocationsTile').load('/fields/Allocations?id=' + data.ID);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.FieldType:
                        DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            FieldTypesAdapter = null;
            FieldTypesSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $("#List").off("rowselect", listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'fields.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                permissions.GetPermissionsForObject(type, 0);

                //#region Grid

                FieldTypesSource = {
                    datatype: 'json',
                    url: '/api/FieldTypes',
                    datafields:
                    [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'FriendlyName' }
                    ]
                };

                FieldTypesAdapter = new $.jqx.dataAdapter(FieldTypesSource);

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
                    pagerrenderer: function () {
                        return renderMinimalPagingHtml($("#List"));
                    },
                    source: FieldTypesAdapter,
                    theme: theme,
                    columns: [
                        { datafield: "FriendlyName", text: "Friendly Name" },
                        { datafield: "Name", text: "Name" },
                        {
                            text: '',
                            dataField: 'ID',
                            width: 80,
                            filterable: false,
                            cellsrenderer: function (row, column, value) {

                                //var data = $("#List").jqxGrid('getrowdata', row);
                                var tools = [
                                    { icon: 'pencil', urlprefix: '/form/fields/types/{0}/edit' },
                                    { icon: 'trash-o', urlprefix: '/form/fields/types/{0}/delete' }
                                ];

                                return renderToolsHtml(value, tools, contextList.FieldType);
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
            });
    });
}