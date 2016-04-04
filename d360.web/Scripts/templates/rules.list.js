function rules_list(app, pageViewModel, templatePath, contextList) {

    var getRuleRoute = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var ruleID = context.params['ruleid'];
        var permissions = new PermissionsModel();
        var type = 'Rule';
        var timescale;
        pageViewModel.Title = 'Rules';
        pageViewModel.Directions = '';

        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Rules', Active: true });

        var statisticsTileVm;
        var RuleGridSource;
        var RuleGridAdapter;

        //#region Event Handlers

        function eventHeaderSelected(data) {
            EventsGrid('EventsTile', contextList, data.GroupID, null, true);
        }

        function timeScaleChanged() {
            timescale = $('#EventBreakdownTimeScale').val();
            EventStatusBreakdownChart('EventStatusChart', contextList, type, ruleID, timescale);
            EventAgeBreakdownChart('EventAgeChart', contextList, type, ruleID, timescale);
            EventCriticalityBreakdownChart('EventCriticalityChart', contextList, type, ruleID, timescale);
        }

        function ruleGridRowSelect(evt) {
            try {
                var args = evt.args;    // event args.
                var row = args.row;     // row data.
                //var key = args.key;   // row key.

                ruleID = row.ID;

                if (!ruleID) ruleID = 0;


                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools('reload', type, ruleID, "");
                ObjectDetail('DetailTile', type, ruleID);

                var loadPermissionsDependentTiles = function () {
                    CollapsibleAttributesTile('AttributesTile', contextList, permissions, type, ruleID);
                    statisticsTileVm.ChangeObject(type, ruleID);
                    statisticsTileVm.GetStatistics();

                    PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, ruleID, '');
                    LineageDiagram('SourcingTile', type, ruleID, true);
                    RelationshipAggregatesTile('AggregatesTileContainer', type, ruleID, permissions);

                    EventStatusBreakdownChart('EventStatusChart', contextList, type, ruleID, timescale);
                    EventAgeBreakdownChart('EventAgeChart', contextList, type, ruleID, timescale);
                    EventCriticalityBreakdownChart('EventCriticalityChart', contextList, type, ruleID, timescale);

                    $('#EventsTile').html('');
                    if (permissions.HasPermission("Root", "Update")) {
                        $("#Fields").fadeIn(250);
                        FieldsGrid('Fields', contextList, permissions, type, ruleID, false);
                    }
                    else {
                        $("#Fields").fadeOut(250);
                    }

                    $('#EventHeadersTile').fadeIn(250);
                    EventHeadersGrid('EventHeadersTile', contextList, type, ruleID, null);
                    $('#EventsTile').fadeIn(250);
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
                            case "delete":
                                $("#RuleGrid").jqxGrid('selectrow', 0);
                                break;
                        }
                        break;
                    //case contextList.SourcingResponsibility:
                    //    environment_diagram('SourcingTile', permissions, type, ruleID);
                    //    break;
                }
            } catch (e) {
                logError("Children : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            RuleGridAdapter = null;
            RuleGridSource = null;
            statisticsTileVm = null;

            amplify.unsubscribe('EventHeaderSelected', eventHeaderSelected);
            $("#RuleGrid").off("rowselect", ruleGridRowSelect);
            $('#EventBreakdownTimeScale').off('change', timeScaleChanged);
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

                amplify.subscribe('EventHeaderSelected', eventHeaderSelected);
                $("#RuleGrid").on("rowselect", ruleGridRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                $('#EventBreakdownTimeScale').on('change', timeScaleChanged);
                //#endregion
            });
    }

    app.get('#/rules', getRuleRoute);
    app.get('#/rules/:ruleid', getRuleRoute);
}