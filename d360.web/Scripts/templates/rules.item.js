function rules_item(app, pageViewModel, templatePath, contextList) {
    var getRuleRoute = function (context) {
        context.app.swap('');
        

        var ruleID = context.params['ruleid'];
        var permissions = new PermissionsModel();
        var type = 'Rule';
        var timescale;

        $.getJSON('/api/rule/' + ruleID, function (json) {
            pageViewModel.Title = json.Name
            pageViewModel.Directions = '';                                    
            pageViewModel.Status = "<h4>Rule Type: <b>" + getTypeNameFromID(json.TypeID) + "</b></h4>";
            context.title(pageViewModel.Title);

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Policies' });
            pageViewModel.breadcrumbs.push({ Name: 'Rules', Url: '#/rules' });
            pageViewModel.breadcrumbs.push({ Name: json.Name, Active: true });
            

            var statisticsTileVm;

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

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        statisticsTileVm.GetStatistics();
                        break;
                }
            }

            function saveAction(data) {                
                try {                    
                    switch (data.context) {
                        case 'commentform':
                            statisticsTileVm.GetStatistics();
                            break;
                        case contextList.Rule:
                            ObjectDetail('DetailTile', type, ruleID);
                            break;
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTileContainer', type, ruleID, permissions);
                            break;
                    }
                } catch (e) {
                    logError("Children : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                statisticsTileVm = null;

                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe('EventHeaderSelected', eventHeaderSelected);
                $('#EventBreakdownTimeScale').off('change', timeScaleChanged);
                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            function getTypeNameFromID(typeId) {
                switch (typeId) {
                    case 1:
                        return 'Informational';                        
                    case 2:
                        return 'Quality Check';                        
                    case 3:
                        return 'Metric';                        
                    case 4:
                        return 'Profile';                        
                    default:
                        return 'Unknown';                        
                }
            }

            context
                .render(templatePath + 'rules.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: type, id: ruleID });
                    statisticsTileVm = new PolicyRuleStatisticsTileModel(type, 0);
                    ko.applyBindings(statisticsTileVm, document.getElementById('StatisticsTile'));

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


                    //#region Event Subscriptions

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe('EventHeaderSelected', eventHeaderSelected);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                    $('#EventBreakdownTimeScale').on('change', timeScaleChanged);
                    //#endregion
                });
        });
    }
        
    app.get('#/rules/:ruleid', getRuleRoute);
}