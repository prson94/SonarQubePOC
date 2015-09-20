function synonyms_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/synonyms/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'SynonymType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var SynonymTypeSource;
        var SynonymTypeAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;
            var row = args.rowindex;

            var data = $('#List').jqxGrid('getrowdata', row);

            if (data) {
                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools("reload", type, data.ID);
                DetailTile('DetailTile', contextList, permissions, type, data.ID);
                SynonymTypeAllocationGrid('AllocationsTile', contextList, permissions, data.ID); //$('#AllocationsTile').load('/parts/' + type + '/' + data.ID + '/allocations');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.SynonymType:
                        DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        amplify.publish("RefreshNavigation");
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            SynonymTypeAdapter = null;
            SynonymTypeSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'synonyms.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                permissions.GetPermissionsForObject(type, 0);

                //#region Grid

                var pagingRenderer = function () {
                    return renderMinimalPagingHtml($("#List"));
                }

                SynonymTypeSource = {
                    datatype: 'json',
                    url: '/api/synonyms',
                    datafields: [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'Description' }
                    ]
                };

                SynonymTypeAdapter = new $.jqx.dataAdapter(SynonymTypeSource);

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
                    source: SynonymTypeAdapter,
                    theme: theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        {
                            text: '',
                            dataField: 'ID',
                            width: 80,
                            filterable: false,
                            cellsrenderer: function (row, column, value) {
                                var tools = [];

                                if (permissions.HasPermission("Root", "Update")) {
                                    tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditSynonymType?id={0}' },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteSynonymType?id={0}' }
                                    ];
                                }

                                return renderToolsHtml(value, tools, contextList.SynonymType);
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