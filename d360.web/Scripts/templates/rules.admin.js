function rules_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/rules/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'RuleType';
        var toolsControlID = '#DimensionTools';
        var RuleTypeID = 0;
        var DimensionSource;
        var DimensionAdapter;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();
                
        //#region Event Handlers
                

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.RuleDimension:
                        $('#DimensionGrid').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            DimensionSource = null;
            DimensionAdapter = null;
            
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);            
        }

        //#endregion

        context
            .render(templatePath + 'rules.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    DimensionSource = {
                        datatype: 'json',
                        url: '/api/ruledimensions',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Description' }
                        ]
                    };

                    DimensionAdapter = new $.jqx.dataAdapter(DimensionSource);

                    $("#DimensionGrid").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: DimensionAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            { datafield: "Description", text: "Description" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [];
                                    if (permissions.HasPermission("Root", "Update")) {
                                        tools = [
                                            { icon: 'pencil', urlprefix: '/form/EditRuleDimension?id={0}' },
                                            { icon: 'trash-o', urlprefix: '/form/DeleteRuleDimension?id={0}' }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.PolicyType);
                                }
                            }
                        ]
                    });

                    if (permissions.HasPermission("Root", "Update")) {
                        TileTools(toolsControlID, [
                            { icon: 'plus', uri: '/form/AddRuleDimension', context: contextList.RuleDimension, title: 'Add Dimension' }
                        ]);
                    }

                    //#region Event Subscriptions

                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
                
            });
    });
}