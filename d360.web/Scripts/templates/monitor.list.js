function monitor_list(app, pageViewModel, templatePath, contextList) {

    var getEventRoute = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var policyID = context.params['policyid'];
        var ruleID = context.params['ruleid']
        var groupID = context.params['groupid'];
        var eventID = context.params['eventid'];
        var permissions = new PermissionsModel();

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var statisticsTileVm;
        var PolicyRuleGridSource;
        var PolicyRuleGridAdapter;

        //#region Event Handlers

        function eventHeaderSelected(data) {
            //GroupID
            EventsGrid('EventsTile', contextList, data.GroupID, eventID, true);
            eventID = null;
        }

        function policyRuleGridRowSelect(evt) {
            try {
                var args = evt.args;    // event args.
                var row = args.row;     // row data.
                //var key = args.key;   // row key.

                var type = row.Type;
                var id = row.ID;

                if (!id) id = 0;


                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools('reload', type, id, "");

                var loadPermissionsDependentTiles = function () {
                    //$('#Detail').load('/parts/' + type + '/' + id + '/detail');

                    EventStatusBreakdownChart('EventStatusChart', contextList, type, id);
                    EventAgeBreakdownChart('EventAgeChart', contextList, type, id);
                    EventCriticalityBreakdownChart('EventCriticalityChart', contextList, type, id);

                    statisticsTileVm.ChangeObject(type, id);
                    statisticsTileVm.GetStatistics();

                    $("#ResponsibilitiesWrapper").fadeIn(250);
                    PeopleResponsibilityTile('Responsibilities', contextList, permissions, type, id, '', false);

                    //#region Event List Logic

                    $('#EventsTile').html('');
                    if (type == 'Rule') {

                        if (permissions.HasPermission("Root", "Update")) {
                            $("#FieldsWrapper").fadeIn(250);
                            FieldsGrid('Fields', contextList, permissions, type, id, false);
                        }
                        else {
                            $("#FieldsWrapper").fadeOut(250);
                        }

                        $('#EventHeadersTile').fadeIn(250);
                        EventHeadersGrid('EventHeadersTile', contextList, type, id, groupID);
                        groupID = null;
                        $('#EventsTile').fadeIn(250);
                    }
                    else {
                        $("#FieldsWrapper").fadeOut(250);
                        $('#EventHeadersTile').fadeOut(250);
                        $('#EventsTile').fadeOut(250);
                        $('#EventHeadersTile').html('');
                    }
                    AttributesTile('AttributesTile', contextList, permissions, type, id, 'Business Attributes');

                    //#endregion
                }
                permissions.GetPermissionsForObject(type, id).then(loadPermissionsDependentTiles);

            } catch (e) {
                logError("Monitor : PolicyRuleGrid.select", e);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.FieldTypeRelation:
                        FieldsGrid('Fields', contextList, permissions, data.custom.ObjectType, data.custom.ObjectID, false);
                        break;
                    case contextList.Policy:
                    case contextList.Rule:
                        $("#PolicyRuleGrid").jqxTreeGrid('updateBoundData');
                        switch (data.action) {
                            case "add":
                                //load child items under selected tree node.
                                if (data.id) {
                                    $("#PolicyRuleGrid").jqxTreeGrid('selectRow', data.id);
                                }
                                break;
                            case "edit":
                                $("#PolicyRuleGrid").jqxTreeGrid('selectRow', data.id);
                                break;
                        }
                        break;
                }
            } catch (e) {
                logError("Children : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            PolicyRuleGridAdapter = null;
            PolicyRuleGridSource = null;
            statisticsTileVm = null;

            amplify.unsubscribe('EventHeaderSelected', eventHeaderSelected);
            $("#PolicyRuleGrid").off("rowSelect", policyRuleGridRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'monitor.list.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'Policy', id: 0 });
                $("#FieldsWrapper").jqxExpander({ width: '100%', expanded: false, toggleMode: 'click', theme: 'plain' });
                $("#ResponsibilitiesWrapper").jqxExpander({ width: '100%', expanded: false, toggleMode: 'click', theme: 'plain' });

                //#region PolicyRuleGrid

                PolicyRuleGridSource = {
                    dataType: "json",
                    url: '/monitor/hierarchy',
                    dataFields: [
                        { name: 'ID' },
                        { name: 'MergedID' },
                        { name: 'Type', type: 'string' },
                        { name: 'expanded', type: 'bool' },
                        { name: 'Name', type: 'string' },
                        { name: 'Items', type: 'array' }
                    ],
                    hierarchy:
                    {
                        root: 'Items'
                    },
                    id: 'MergedID'
                };

                PolicyRuleGridAdapter = new $.jqx.dataAdapter(PolicyRuleGridSource);

                $("#PolicyRuleGrid").jqxTreeGrid({
                    width: '99.5%',
                    height: '500px',
                    theme: list_theme,
                    //showHeader: false,
                    selectionMode: 'singleRow',
                    source: PolicyRuleGridAdapter,
                    filterable: true,
                    filterMode: 'simple',
                    sortable: true,
                    icons: true,
                    columns: [
                      {
                          text: 'Name',
                          dataField: 'Name',
                          width: '90%',
                          cellsRenderer: function (rowKey, dataField, value, data) {
                              if (data.Type == "Rule") {
                                  return "<i>" + data.Name + "</i>";
                              }
                              else {
                                  return data.Name;
                              }
                          }
                      },
                      {
                          text: '',
                          dataField: 'Type',
                          width: '10%',
                          cellsRenderer: function (rowKey, dataField, value, data) {
                              return "<i title='" + data.Type + "' class='fa fa-" + ((data.Type == "Rule") ? "gavel" : "institution") + "'></i>";
                          }
                      }
                    ],
                    ready: function () {
                        try {
                            if (policyID) {
                                if (ruleID) {
                                    $('#PolicyRuleGrid').jqxTreeGrid('selectRow', 'Rule|' + ruleID);
                                }
                                else {
                                    $('#PolicyRuleGrid').jqxTreeGrid('selectRow', 'Policy|' + policyID);
                                }
                            }
                            else {
                                var firstRow = $('#PolicyRuleGrid').jqxTreeGrid('getRows')[0];
                                var key = $("#PolicyRuleGrid").jqxTreeGrid('getKey', firstRow);
                                $('#PolicyRuleGrid').jqxTreeGrid('selectRow', key);
                            }
                            policyID = null;
                            ruleID = null;
                        } catch (e) {
                            console.log(e);
                        }
                    }
                });

                //#endregion

                statisticsTileVm = new PolicyRuleStatisticsTileModel('Policy', 0);
                ko.applyBindings(statisticsTileVm, document.getElementById('StatisticsTile'));

                //#region Event Subscriptions

                amplify.subscribe('EventHeaderSelected', eventHeaderSelected);
                $("#PolicyRuleGrid").on("rowSelect", policyRuleGridRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    }

    app.get('#/monitor/:policyid/:ruleid/:groupid/:eventid', getEventRoute);
    app.get('#/monitor/:policyid/:ruleid/:groupid', getEventRoute);
    app.get('#/monitor/:policyid/:ruleid', getEventRoute);
    app.get('#/monitor/:policyid', getEventRoute);
    app.get('#/monitor', getEventRoute);
}