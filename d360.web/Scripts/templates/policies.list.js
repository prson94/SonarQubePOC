function policies_list(app, pageViewModel, templatePath, contextList) {

    var getPolicyRoute = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'Policy';
        var policyTypeID = context.params['policytypeid'];
        var policyID = context.params['policyid'];
        var permissions = new PermissionsModel();

        $.getJSON('/api/policytypes/' + policyTypeID, function (json) {

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;

            context.title(pageViewModel.Title);

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Policies' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            var statisticsTileVm;
            var PolicyGridSource;
            var PolicyGridAdapter;

            //#region Event Handlers

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools({ type: type, id: policyID });
            }

            function policyGridRowSelect(evt) {
                try {
                    var args = evt.args;    // event args.
                    var row = args.row;     // row data.
                    //var key = args.key;   // row key.

                    policyID = row.ID;

                    if (!policyID) policyID = 0;

                    var iType = type;
                    var iID = policyID;

                    if (iID == 0) {
                        iType = 'PolicyType';
                        iID = policyTypeID;

                        $('#SideIcons').PageTools('reload', type, policyTypeID, "root");
                    }
                    else
                    {
                        $('#SideIcons').PageTools('reload', type, policyID, "");
                    }

                    amplify.publish(AmplifyActions.TileUnsubscribe, {});
                    
                    ObjectDetail('DetailTile', iType, iID);

                    var loadPermissionsDependentTiles = function () {

                        if (json.AllowAttributes) {
                            CollapsibleAttributesTile('AttributesTile', contextList, permissions, iType, iID);
                        }
                        else {
                            $('#AttributesTile').hide();
                        }

                        statisticsTileVm.ChangeObject(iType, iID);
                        statisticsTileVm.GetStatistics();

                        PolicyStatusKpi('StatusTile', contextList, permissions, iID);
                        PeopleResponsibilityTile('Responsibilities', contextList, permissions, iType, iID, '');
                        LineageDiagram('SourcingTile', iType, iID, true);
                        RelationshipAggregatesTile('AggregatesTileContainer', iType, iID, permissions);
                    }
                    permissions.GetPermissionsForObject(iType, iID).then(loadPermissionsDependentTiles);

                } catch (e) {
                    logError("Monitor : PolicyGrid.select", e);
                }
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTileContainer', type, policyID, permissions);
                            break;
                        case contextList.SourceToTarget:
                            LineageDiagram('SourcingTile', type, policyID, true);
                            break;
                        case contextList.Policy:
                            $("#PolicyGrid").jqxTreeGrid('updateBoundData');
                            switch (data.action) {
                                case "add":
                                    //load child items under selected tree node.
                                    if (data.id) {
                                        policyID = data.id;
                                        $("#PolicyGrid").jqxTreeGrid('selectRow', policyID);
                                    }
                                    break;
                                case "edit":
                                    policyID = data.id;
                                    $("#PolicyGrid").jqxTreeGrid('selectRow', policyID);
                                    break;
                            }
                            break;
                    }
                } catch (e) {
                    logError("Children : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                PolicyGridAdapter = null;
                PolicyGridSource = null;
                statisticsTileVm = null;

                $("#PolicyGrid").off("rowSelect", policyGridRowSelect);
                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
            }

            //#endregion

            context
                .render(templatePath + 'policies.list.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: 'PolicyType', id: policyTypeID, context: 'root' });
                    statisticsTileVm = new PolicyRuleStatisticsTileModel(type, 0);
                    ko.applyBindings(statisticsTileVm, document.getElementById('StatisticsTile'));

                    //#region PolicyGrid

                    $.getJSON('/api/PolicyType/' + policyTypeID + '/grid/definition', function (gridinfo) {

                        PolicyGridSource = {
                            dataType: "json",
                            url: '/api/policytypes/' + policyTypeID + '/policies',
                            dataFields: gridinfo.Fields,
                            hierarchy:
                            {
                                keyDataField: { name: 'ID' },
                                parentDataField: { name: 'ParentID' }
                            },
                            id: 'ID'
                        };

                        PolicyGridAdapter = new $.jqx.dataAdapter(PolicyGridSource, {
                            beforeLoadComplete: function (records) {
                                $.each(records, function () {
                                    this.expanded = "true";
                                });
                                return records;
                            }
                        });

                        $("#PolicyGrid").jqxTreeGrid({
                            width: '99.5%',
                            height: '500px',
                            theme: list_theme,
                            //showHeader: false,
                            selectionMode: 'singleRow',
                            source: PolicyGridAdapter,
                            filterable: true,
                            filterMode: 'simple',
                            sortable: true,
                            icons: true,
                            columns: gridinfo.Columns,
                            columnsResize: true,
                            ready: function () {
                                try {
                                    if (policyID) {
                                        $('#PolicyGrid').jqxTreeGrid('selectRow', policyID);
                                    }
                                    else {
                                        var firstRow = $('#PolicyGrid').jqxTreeGrid('getRows')[0];
                                        var key = $("#PolicyGrid").jqxTreeGrid('getKey', firstRow);
                                        $('#PolicyGrid').jqxTreeGrid('selectRow', key);
                                    }
                                } catch (e) {
                                    console.log(e);
                                }
                            }
                        });

                    });

                    //#endregion

                    //#region Event Subscriptions

                    $("#PolicyGrid").on("rowSelect", policyGridRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);

                    //#endregion
                });

        });
    }

    app.get('#/policies/:policytypeid', getPolicyRoute);
    app.get('#/policies/:policytypeid/:policyid', getPolicyRoute);
}