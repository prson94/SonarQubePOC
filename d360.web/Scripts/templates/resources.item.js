function resources_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/resources/:id', function (context) {
        context.app.swap('');

        var type = 'Resource';
        var id = context.params['id'];
        var assignmentsTile;

        $.getJSON('/api/resources/1/' + id, function (model) {

            pageViewModel.ID = id;
            pageViewModel.Title = model.FirstName + ' ' + model.LastName;
            context.title(pageViewModel.Title);

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Resources' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            
            var socialTile;

            function loadAssignments() {
                assignmentsTile.LookBackDays = 7;
                $.getJSON("/api/Count/Assignments/7?id=" +id , function (data) {
                    assignmentsTile.Rows([]);
                    assignmentsTile.Rows(data);
                });
            }

            //#region Event Handlers

            function commandExecuted(commandName) {                
                switch (commandName) {
                    case 'follow':
                        ResourceStatisticsTile('SocialTile', type, id);
                        YourFollowedItemsTile('FollowingTile', id, 'Items ' + model.FirstName + ' Follows');
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools('reload', type, id);
            }

            function saveAction(data) {
                try {                    
                    switch (data.context) {
                        case contextList.Resource:
                            ObjectDetail('DetailTile', type, id);
                            break;
                        case contextList.Comment:
                            ResourceStatisticsTile('SocialTile', type, id);
                            break;
                        case "OwnerCertificationWorkflow":
                        case "OwnerApprovalWorkflow":
                        case contextList.WorkflowIssue:
                            loadAssignments();
                            break;
                    }
                } catch (e) {
                    logError("resources.item : SaveAction", e);
                }
            }

            function unsubscribe(data) {                
                socialTile = null;
                assignmentsTile = null;

                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
                amplify.unsubscribe("SaveAction", saveAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'resources.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: type, id: id });

                    ObjectDetail('DetailTile', type, id);

                    YourFollowedItemsTile('FollowingTile', id, 'Items ' + model.FirstName  + ' Follows');
                    YourOwnedItemsTile('OwnedTile', id, 'Items ' + model.FirstName + ' Owns');

                    ResourceStatisticsTile('SocialTile', type, id);

                    assignmentsTile = new HomePageCountTileModel(model.FirstName + '\'s Assignments', 7);
                    assignmentsTile.NoDataMessage('');
                    ko.applyBindings(assignmentsTile, document.getElementById('AssignmentsTile'));

                    loadAssignments();

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                });
        });
    });
}