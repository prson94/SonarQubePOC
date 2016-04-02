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
            pageViewModel.Title = $('<div/>').html(json.Name).text();
            pageViewModel.Type = json.TypeName;
            pageViewModel.Status = "<h4>Status: <b style='color:" + getArtifactStatusForeColor(json.Status) + "'>" + json.Status + "</b></h4>";
            pageViewModel.breadcrumbs = json.Breadcrumbs;
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

            function refreshArtifactTitle() {                
                $.getJSON('/api/artifact/' + id, function (json) {
                    pageViewModel.Title = $('<div/>').html(json.Name).text();
                    pageViewModel.Status = "<h4>Status: <b style='color:" + getArtifactStatusForeColor(json.Status) + "'>" + json.Status + "</b></h4>";
                    pageViewModel.breadcrumbs = json.Breadcrumbs;
                    context.contentHeader(pageViewModel);
                });
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
                            PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '');
                            break;
                        case "RequestCertification":
                        case "Workflow":
                            ObjectDetail('DetailTile', type, id);
                            break;
                        case contextList.SourceToTarget:
                            LineageDiagram('SourcingTile', type, id, permissions, false);
                            break;
                        case contextList.Responsibility:                        
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);                            
                            break;
                        case contextList.Artifact:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            ObjectDetail('DetailTile', type, id);
                            refreshArtifactTitle();
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
                    ObjectDetail('DetailTile', type, id);

                    var loadPermissionsDependentTiles = function () {
                        ObjectStatisticsTile('MicroWidget1', type, id);
                        RelationshipAggregatesTile('AggregatesTile', type, id, permissions);
                        PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '');
                        LineageDiagram('SourcingTile', type, id, permissions, false);
                        CertificationNotificationTile('CertificationNotification', id);

                        if (json.AllowRelatedArtifacts) {
                            RelatedArtifactsGrid('RelatedArtifactsTile', permissions, json.TypeName, typeID, id);
                        }
                        else {
                            $('#RelatedArtifactsTile').hide();
                        }

                        CollapsibleSynonymsTile('SynonymsTile', contextList, permissions, type, id);
                        CollapsibleAttributesTile('AttributesTile', contextList, permissions, type, id);
                        if (json.AllowPredicateHierarchies) {
                            CollapsibleTypeHierarchyTile('StructureTile', contextList, permissions, type, id);
                            //HierarchyTile('GroupHierarchyTile', contextList, permissions, type, id, 4, 'Groupings');
                            // HierarchyTile('ParentHierarchyTile', contextList, permissions, type, id, 5, 'Parent/Child Hierarchy');
                        }
                        else {
                            $('#StructureTile').hide();
                            //$('#GroupHierarchyTile').hide();
                        }
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