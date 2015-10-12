function rules_list(app, pageViewModel, templatePath, contextList) {

    var getRuleRoute = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var ruleID = context.params['ruleid'];
        var permissions = new PermissionsModel();
        var type = 'Rule';
        pageViewModel.Title = 'Rules';
        pageViewModel.Directions = '';

        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Rules', Active: true });

        var statisticsTileVm;
        var RuleGridSource;
        var RuleGridAdapter;

        //#region Event Handlers

        function ruleGridRowSelect(evt) {
            try {
                var args = evt.args;    // event args.
                var row = args.row;     // row data.
                //var key = args.key;   // row key.

                ruleID = row.ID;

                if (!ruleID) ruleID = 0;


                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools('reload', type, ruleID, "");

                var loadPermissionsDependentTiles = function () {
                    DetailTile('DetailTile', contextList, permissions, type, ruleID);

                    statisticsTileVm.ChangeObject(type, ruleID);
                    statisticsTileVm.GetStatistics();

                    PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, ruleID, '');
                    environment_diagram('SourcingTile', permissions, type, ruleID);
                    AttributesTile('AttributesTile', contextList, permissions, type, ruleID, 'Business Attributes');
                    RelationshipAggregatesTile('AggregatesTileContainer', type, ruleID, permissions);
                }
                permissions.GetPermissionsForObject(type, ruleID).then(loadPermissionsDependentTiles);

            } catch (e) {
                logError("Monitor : RuleGrid.select", e);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Intersect:
                        RelationshipAggregatesTile('AggregatesTileContainer', type, ruleID, permissions);
                        break;
                    case contextList.Rule:
                        $("#RuleGrid").jqxGrid('updatebounddata');
                        switch (data.action) {
                            case "add":
                                //load child items under selected tree node.
                                if (data.id) {
                                    ruleID = data.id;
                                    var ix = $('#RuleGrid').jqxGrid('getrowboundindexbyid', ruleID);
                                    $("#RuleGrid").jqxGrid('selectrow', ix);
                                }
                                break;
                            case "edit":
                                ruleID = data.id;
                                var ix = $('#RuleGrid').jqxGrid('getrowboundindexbyid', ruleID);
                                $("#RuleGrid").jqxGrid('selectrow', ix);
                                break;
                        }
                        break;
                }
            } catch (e) {
                logError("Children : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            RuleGridAdapter = null;
            RuleGridSource = null;
            statisticsTileVm = null;

            $("#RuleGrid").off("rowselect", ruleGridRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'rules.list.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });
                statisticsTileVm = new PolicyRuleStatisticsTileModel(type, 0);
                ko.applyBindings(statisticsTileVm, document.getElementById('StatisticsTile'));

                //#region RuleGrid

                RuleGridSource = {
                    dataType: "json",
                    url: '/api/rules?$orderby=Name',
                    dataFields: [
                        { name: 'ID' },
                        { name: 'Name', type: 'string' }
                    ],
                    id: 'ID'
                };

                RuleGridAdapter = new $.jqx.dataAdapter(RuleGridSource);

                $("#RuleGrid").jqxGrid({
                    theme: list_theme,
                    width: grid_width,
                    pagesizeoptions: ['5', '10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    altrows: true,
                    source: RuleGridAdapter,
                    filterable: true,
                    showfilterrow: true,
                    columns: [
                        { text: 'ID', dataField: 'ID', width: '7%' },
                        { text: 'Name', dataField: 'Name', width: '93%' }
                    ],
                    ready: function () {
                        try {
                            if (ruleID) {
                                var ix = $('#RuleGrid').jqxGrid('getrowboundindexbyid', ruleID);
                                $('#RuleGrid').jqxGrid('selectrow', ix);
                            }
                            else {
                                $('#RuleGrid').jqxGrid('selectrow', 0);
                            }
                        } catch (e) {
                            console.log(e);
                        }
                    }
                });

                //#endregion

                //#region Event Subscriptions

                $("#RuleGrid").on("rowselect", ruleGridRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    }

    app.get('#/rules', getRuleRoute);
    app.get('#/rules/:ruleid', getRuleRoute);
}