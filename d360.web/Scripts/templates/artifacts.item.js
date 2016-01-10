function artifacts_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/:typeid/:id', function (context) {
        //context.spinner();
        context.app.swap('');
        
        var type = 'Artifact';
        var typeID = context.params['typeid'];
        var id = context.params['id'];
        var permissions = new PermissionsModel();

        $.getJSON('/api/artifact/' + id, function (json) {

            var getArtifactStatusForeColor = function (status) {
                var foreColor = '#000';
                switch (status) {
                    case 'Certified':
                        foreColor = '#3f9d40';
                        break;
                    case 'Under Review':
                        foreColor = '#e2792a';
                        break;
                    default:
                        foreColor = '#999';
                        break;
                }
                return foreColor;
            }

            pageViewModel.ObjectType = 'Artifact';
            pageViewModel.ObjectID = id;
            pageViewModel.Title = json.Name;
            pageViewModel.Type = json.TypeName;
            pageViewModel.Status = "<h4>Status: <b style='color:" + getArtifactStatusForeColor(json.Status) + "'>" + json.Status + "</b></h4>";
            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Glossary' });
            pageViewModel.breadcrumbs.push({ Name: json.TypeName });
            pageViewModel.breadcrumbs.push({ Name: json.Name, Active: true });
            //pageViewModel.Directions = json.Description;

            context.title(pageViewModel.Title);

            //#region Event Handlers

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        ObjectStatisticsTile('MicroWidget1', type, id);
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools('reload', type, id);
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.Comment:
                        case 'commentform':
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            break;
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTile', type, id, permissions);
                            break;
                        case "RequestCertification":
                        case "Workflow":
                            DetailTile('DetailTile', contextList, permissions, type, id);
                            break;
                        case contextList.SourceToTarget:
                            LineageDiagram('SourcingTile', type, id, null, true);
                            break;
                        case contextList.Responsibility:
                        case contextList.Artifact:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            break;
                        case contextList.Synonym:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                    }
                } catch (e) {
                    logError("artifact.item : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'artifacts.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {

                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: type, id: id });
                    $("#RandomQuestion").RandomSurveyQuestion({ objectType: type, objectID: id });

                    var loadPermissionsDependentTiles = function () {
                        ObjectStatisticsTile('MicroWidget1', type, id);
                        RelationshipAggregatesTile('AggregatesTile', type, id, permissions);

                        //Relationship_SimpleHierarchyTile('SimpleHierarchyTile', contextList, permissions, type, id);

                        PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '');
                        LineageDiagram('SourcingTile', type, id, null, true);
                        CertificationNotificationTile('CertificationNotification', id);

                        if (json.AllowRelatedArtifacts) {
                            RelatedArtifactsGrid('RelatedArtifactsTile', permissions, json.TypeName, typeID, id);
                        }
                        else {
                            $('#RelatedArtifactsTile').hide();
                        }
                        DetailsTile('DetailTile', contextList, permissions, type, id, contextList.Artifact);
                    }

                    permissions.GetPermissionsForObject(type, id).then(loadPermissionsDependentTiles);

                    //#region Event Subscriptions

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                });
        });
    });
}