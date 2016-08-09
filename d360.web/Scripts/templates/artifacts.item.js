function artifacts_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/:typeid/:id', function (context) {        
        context.app.swap('');
        
        var type = 'Artifact';
        var typeID = context.params['typeid'];
        var id = context.params['id'];
        var permissions = new PermissionsModel();
        var survey;
        
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
                        case 'Issue':
                        case 'IssueWorkflow':
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            break;
                        case contextList.Synonym:
                            RelationshipAggregatesTile('AggregatesTile', type, id, permissions);
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
                            if (CompanySettings.UseNewRelationships == 'true')
                                NewLineageDiagram('SourcingTile', type, id, false);
                            else
                                LineageDiagram('SourcingTile', type, id, false);                            
                            break;
                        case contextList.Responsibility:                        
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);                            
                            break;
                        case contextList.Artifact:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            ObjectDetail('DetailTile', type, id);                                                                               
                            break;
                        case contextList.Synonym:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                        case 'Challenge':                            
                            setTimeout(function () { ChallengeNotificationTile('ChallengeNotification', contextList, id); }, 2000);
                            ObjectStatisticsTile('MicroWidget1', type, id);
                            $("#Challenge").hide();
                            break;                        
                    }
                } catch (e) {
                    logError("artifact.item : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                survey = null
                $('#AttributesTile').Attributes('destroy');
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
                    survey = new Survey('Survey', type, id, 'ArtifactType', typeID);
                    ObjectDetail('DetailTile', type, id);

                    var loadPermissionsDependentTiles = function () {
                        ObjectStatisticsTile('MicroWidget1', type, id);

                        //$('#RelationshipsTile').RelationshipsTile({ obj: type, objid: id });
                        RelationshipAggregatesTile('AggregatesTile', type, id, permissions);
                        PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '');

                        if (CompanySettings.UseNewRelationships == 'true')
                            NewLineageDiagram('SourcingTile', type, id, false);
                        else
                            LineageDiagram('SourcingTile', type, id, false);

                        CertificationNotificationTile('CertificationNotification', id);
                        ChallengeNotificationTile('ChallengeNotification', contextList, id);

                        if (json.AllowRelatedArtifacts) {
                            RelatedArtifactsGrid('RelatedArtifactsTile', permissions, json.TypeName, typeID, id);
                        }
                        else {
                            $('#RelatedArtifactsTile').hide();
                        }

                        if (json.AllowAttributes) {
                            $('#AttributesTile').Attributes({ object: type, objectID: id, readOnly: permissions.HasPermission("Attributes", "Update") });
                        }
                        else {
                            $('#AttributesTile').hide();
                        }

                        if (json.AllowSynonyms) {
                            $('#SynonymsTile').Synonyms({
                                object: type,
                                objectID: id,
                                canEdit: permissions.HasPermission("Relationship", "Update"),
                                canDelete: permissions.HasPermission("Relationship", "Delete")
                            });
                        }
                        else {
                            $('#SynonymsTile').hide();
                        }

                        if (json.AllowPredicateHierarchies) {
                            CollapsibleTypeHierarchyTile('StructureTile', contextList, permissions, type, id);                            
                        }
                        else {
                            $('#StructureTile').hide();                            
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