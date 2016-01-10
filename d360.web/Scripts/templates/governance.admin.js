function governance_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/governance/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'ResponsibilityType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });
        var permissions = new PermissionsModel();

        var GovernanceAdapter;
        var GovernanceSource;

        //#region Event Handlers

        function adminResponsibilityTypeGridBindingComplete(event) {
            var displayCount = $('#AdminResponsibilityTypeGrid').jqxGrid('getdisplayrows').length;
            if (displayCount > 0) {
                $('#AdminResponsibilityTypeGrid').jqxGrid('selectrow', 0);
            }
        }

        function adminResponsibilityTypeGridRowSelect(event) {
            var args = event.args;
            var data = args.row;

            amplify.publish(AmplifyActions.TileUnsubscribe, {});
            $('#SideIcons').PageTools("reload", type, data.ID);

            DetailTile('DetailTile', contextList, permissions, type, data.ID);
            //$('#UsageTile').load('/parts/ResponsibilityTypeUsageGrid?id=' + data.ID);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.ResponsibilityType:
                        DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#AdminResponsibilityTypeGrid').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            GovernanceAdapter = null;
            GovernanceSource = null;

            $("#AdminResponsibilityTypeGrid").off("bindingcomplete", adminResponsibilityTypeGridBindingComplete);
            $("#AdminResponsibilityTypeGrid").off("rowselect", adminResponsibilityTypeGridRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'governance.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                GovernanceSource =
                            {
                                datatype: 'json',
                                url: '/api/ownership/types',
                                datafields:
                                [
                                    { name: 'ID' },
                                    { name: 'Name' }
                                ]
                            };

                GovernanceAdapter = new $.jqx.dataAdapter(GovernanceSource);

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddResponsibilityType?Group=1', context: contextList.ResponsibilityType, title: 'Add responsibility type' });
                    }
                    TileTools('#AdminResponsibilityTypeGridTools', tools);

                    $("#AdminResponsibilityTypeGrid").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        groupable: false,
                        source: GovernanceAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                                    var tools = [];

                                    if (permissions.HasPermission('Root', 'Update')) {
                                        tools.push({ icon: 'pencil', urlprefix: '/form/EditResponsibilityType?id={0}' });
                                    }

                                    if (permissions.HasPermission('Root', 'Delete')) {
                                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteResponsibilityType?id={0}' });
                                    }

                                    return renderToolsHtml(value, tools, contextList.OwnershipType);
                                }
                            }
                        ]
                    });

                };

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);

                //#region Event Subscriptions

                $("#AdminResponsibilityTypeGrid").one("bindingcomplete", adminResponsibilityTypeGridBindingComplete);
                $("#AdminResponsibilityTypeGrid").on("rowselect", adminResponsibilityTypeGridRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}