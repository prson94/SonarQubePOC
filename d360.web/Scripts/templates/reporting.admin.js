function reporting_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/reporting/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'Report';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var ReportsSource;
        var ReportsAdapter;
        var TilesSource;
        var TilesAdapter;
        var layoutTileVm;

        //#region Event Handlers

        function listRowSelect(event) {
            var args = event.args;
            var row = args.rowindex;

            var data = $('#List').jqxGrid('getrowdata', row);
            if (data) {
                amplify.publish(AmplifyActions.TileUnsubscribe, {});

                $('#SideIcons').PageTools("reload", type, data.ID);

                DetailTile('DetailTile', contextList, permissions, type, data.ID);

                if (permissions.HasPermission('Root', 'Update')) {
                    TileTools('#TilesTileTools', [
                        { icon: 'plus', uri: "/form/AddReportTile?reportID=" + data.ID, context: contextList.ReportTile, title: 'Add tile' }
                    ]);
                }

                TilesSource.url = '/reports/' + data.ID + '/tiles';
                $("#TilesTile").jqxGrid('updatebounddata');

                var changePromise = layoutTileVm.ChangeObject(data.ID);
                changePromise
                    .then(layoutTileVm.GetLayout)
                    .done(function (message) {
                        //layoutTileVm.Render();
                    });

            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Report:
                        $('#List').one('bindingcomplete', function (event) {
                            var selectActiveRow = false;

                            if (data) {
                                if (data.id) {
                                    selectActiveRow = true;
                                }
                            }
                            if (selectActiveRow) {
                                try {
                                    var selectedRowIndex = $('#List').jqxGrid('getrowboundindexbyid', data.id);
                                    $("#List").jqxGrid('ensurerowvisible', selectedRowIndex);
                                    $("#List").jqxGrid('selectrow', selectedRowIndex);
                                } catch (e) { }
                            }
                            else {
                                var rowCount = $('#List').jqxGrid('getdisplayrows').length;
                                if (rowCount > 0) {
                                    $('#List').jqxGrid('selectrow', 0);
                                }
                            }
                        });
                        $('#List').jqxGrid('updatebounddata');
                        break;
                    case contextList.ReportTile:
                        $('#TilesTile').jqxGrid('updatebounddata');
                        layoutTileVm.GetLayout();
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            ReportsAdapter = null;
            ReportsSource = null;
            TilesAdapter = null;
            TilesSource = null;
            layoutTileVm = null;

            //$("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe(AmplifyActions.Save, saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'reporting.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadPermissionsDependentTiles = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddReport', context: contextList.Report, title: 'Add report' });
                    }
                    TileTools('#ListTools', tools);

                    layoutTileVm = new ReportDesignerModel(0);
                    ko.applyBindings(layoutTileVm, document.getElementById('LayoutTile'));

                    //#region Grid

                    ReportsSource = {
                        datatype: 'json',
                        url: '/reports',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' }
                        ],
                        id: 'ID'
                    };

                    ReportsAdapter = new $.jqx.dataAdapter(ReportsSource);

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
                        source: ReportsAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                sortable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    var tools = [];

                                    tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditReport?id=' + data.ID },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteReport?id=' + data.ID },
                                        { icon: 'search', urlprefix: '/reports/PreviewOverlay?id=' + data.ID }
                                    ];

                                    return renderToolsHtml(value, tools, contextList.Report);
                                }
                            }
                        ],
                        ready: function () {
                            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
                            if (rowCount > 0) {
                                $('#List').jqxGrid('selectrow', 0);
                            }
                        }
                    });

                    //#endregion

                    //#region Tiles Grid

                    TilesSource = {
                        datatype: 'json',
                        url: null,
                        datafields: [
                            { name: 'ID' },
                            { name: 'ReportID' },
                            { name: 'Name' },
                            { name: 'ContentAreaNumber' }
                        ]
                    };

                    TilesAdapter = new $.jqx.dataAdapter(TilesSource);

                    $("#TilesTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: TilesAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            { datafield: "ContentAreaNumber", text: "Content Area #", width: '120', filterable: false },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    var tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditReportTile?id=' + data.ID },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteReportTile?id=' + data.ID }
                                    ];

                                    return renderToolsHtml(value, tools, contextList.ReportTile);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    //$("#List").on("bindingcomplete", listBindingComplete);
                    $('#List').on('rowselect', listRowSelect);
                    amplify.subscribe(AmplifyActions.Save, saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadPermissionsDependentTiles);


            });
    });
}