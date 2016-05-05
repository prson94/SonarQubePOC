function rules_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/rules/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'RuleType';
        var toolsControlID = '#DimensionTools';
        var RuleTypeID = 0;
        var DimensionSource;
        var RuleTypeSource;
        var DimensionAdapter;
        var RuleTypeAdapter;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();
                
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

                RuleTypeID = data.ID;

              
                $('#ClaimsTile').load('/parts/ResponsibilityTypeObjectClaimGrid?type=' + type + '&id=' + RuleTypeID);
                PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, RuleTypeID, 'Default Responsibilities', true);
            }
        }

        
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
            
            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);            
        }

        //#endregion

        function renderDimensionsGrid() {
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
        }

        context
            .render(templatePath + 'rules.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    renderDimensionsGrid();

                    RuleTypeSource = {
                        datatype: 'json',
                        url: '/api/ruletypes',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Description' }
                        ]
                    };

                    RuleTypeAdapter = new $.jqx.dataAdapter(RuleTypeSource);

                    $("#List").jqxGrid({
                        altrows: true,
                        width: grid_width,                        
                        autoheight: true,
                        sortable: true,                                                
                        source: RuleTypeAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" }                            
                        ]
                    });

                    //#endregion

                    //PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, policyTypeID, 'Default Responsibilities', true);

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