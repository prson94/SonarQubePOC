function analytics_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/analytics/administration', function (context) {
        context.app.swap('');

        var type = 'StatisticType';

        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var StatisticTypeSource;
        var StatisticTypeAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            try {
                var args = event.args;
                var data = args.row;

                $('#SideIcons').PageTools("reload", type, data.ID);

                var loadPermissionsDependentTiles = function () {
                    amplify.publish(AmplifyActions.TileUnsubscribe, {});

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddStatisticType', context: contextList.StatisticType, title: 'Add analytic' });
                    }
                    TileTools('#ListTools', tools);

                    $('#DetailTile').load('/parts/' + type + '/' + data.ID + '/detail');
                }

                permissions.GetPermissionsForObject(type, data.ID).then(loadPermissionsDependentTiles);
            }
            catch (e) {
                console.log("analytics.admin.js : List.rowselect : " + e);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.StatisticType:
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            }
            catch (e) {
            }
        }

        function unsubscribe(data) {
            StatisticTypeAdapter = null;
            StatisticTypeSource = null;

            $("#List").off("rowselect", listRowSelect);
            $("#List").off("bindingcomplete", listBindingComplete);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'analytics.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {

                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'StatisticType', id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddStatisticType', context: contextList.StatisticType, title: 'Add analytic type' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    StatisticTypeSource = {
                        datatype: 'json',
                        url: '/api/statistics',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Score' },
                            { name: 'ObjectName' }
                        ]
                    };

                    StatisticTypeAdapter = new $.jqx.dataAdapter(StatisticTypeSource);

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
                        source: StatisticTypeAdapter,
                        theme: theme,
                        columns: [
                            { datafield: "ObjectName", text: "Object", width: '30%', filtertype: 'checkedlist' },
                            { datafield: "Name", text: "Name" },
                            { datafield: "Score", text: "Score", width: '15%', filtertype: 'checkedlist' },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                sortable: false,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditStatisticType?id={0}' },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteStatisticType?id={0}' }
                                    ];
                                    return renderToolsHtml(value, tools, contextList.StatisticType);
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

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);


            });
    });
}