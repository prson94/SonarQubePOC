function taxonomies_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/catalogs/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'TaxonomyType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var TaxonomyTypeSource;
        var TaxonomyTypeAdapter;

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
                TaxonomyTypeLevelsGrid('LevelsTile', contextList, permissions, data.ID);
                FieldsGrid("FieldsTile", contextList, permissions, type, data.ID);
                $('#ClaimsTile').load('/parts/ResponsibilityTypeObjectClaimGrid?type=' + type + '&id=' + data.ID);
                PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, data.ID, 'Default Responsibilities', true);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.TaxonomyType:
                        //$('#List').one('bindingcomplete', function (event) {
                        //    var selectActiveRow = false;

                        //    if (data) {
                        //        if (data.id) {
                        //            selectActiveRow = true;
                        //        }
                        //    }

                        //    if (selectActiveRow) {
                        //        try {
                        //            $("#Tree").jqxTreeGrid('selectRow', data.id);
                        //        } catch (e) { }
                        //    }
                        //    else {
                        //        var rows = $("#Tree").jqxTreeGrid('getRows');
                        //        if (rows.length > 0) {
                        //            var firstRow = $("#Tree").jqxTreeGrid('getRows')[0];
                        //            $("#Tree").jqxTreeGrid('selectRow', firstRow.uid);
                        //        }
                        //    }
                        //});
                        $('#List').jqxGrid('updatebounddata');
                        amplify.publish("RefreshNavigation");
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            TaxonomyTypeAdapter = null;
            TaxonomyTypeSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'taxonomies.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: "/form/catalogs/add", context: contextList.TaxonomyType, title: 'Add model type' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    TaxonomyTypeSource = {
                        datatype: 'json',
                        url: '/api/catalogs',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Description' },
                            { name: 'TaxonomyTypeClass' },
                            { name: 'MaximumDepth' }
                        ]
                    };

                    TaxonomyTypeAdapter = new $.jqx.dataAdapter(TaxonomyTypeSource);

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
                        source: TaxonomyTypeAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            { datafield: "TaxonomyTypeClass", text: "Classification", filtertype: 'checkedlist', width: 120 },
                            { datafield: "MaximumDepth", text: "Maximum Depth", filtertype: 'checkedlist', width: 100 },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {

                                    var tools = [];
                                    if (permissions.HasPermission("Root", "Update")) {
                                        tools = [
                                            { isitemlink: true, urlprefix: '#/catalogs/{0}' },
                                            { icon: 'pencil', urlprefix: '/form/catalogs/{0}/edit' },
                                            { icon: 'trash-o', urlprefix: '/form/catalogs/{0}/delete' }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.TaxonomyType);
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
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}