function groups_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/groups/:id', function (context) {
        context.app.swap('');

        var id = context.params['id'];
        var type = 'Group';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Groups' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();
        var socialTile = new GroupSocialMicroTileModel(id);

        $.getJSON('/api/groups/' + id, function (json) {
            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;
            pageViewModel.breadcrumbs.push({ Name: json.Name, Active: true });

            context.title(pageViewModel.Title);

            //#region Event Handlers

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        socialTile.GetStatistics();
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools("reload", type, id);
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.ResourceGroup:
                            socialTile.GetStatistics();
                            break;
                        case contextList.Artifact:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                        case contextList.Synonym:
                            $('#DetailWidget .content').load('/parts/' + data.custom.ObjectType + '/' + data.custom.ObjectID + '/detailwithsynonyms');
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
                .render(templatePath + 'groups.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: type, id: id });

                    permissions.GetPermissionsForObject(type, id);

                    $('#OwnedTile').load('/parts/groups/' + id + '/ownership');
                    GroupMembersGrid("MembersTile", contextList, permissions, id);

                    var GroupSocial = new BoardViewModel();
                    ko.applyBindings(GroupSocial, document.getElementById('GroupBoard'));
                    GroupSocial.changeObject(type, id);

                    
                    ko.applyBindings(socialTile, document.getElementById('SocialTile'));
                    socialTile.GetStatistics();

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                });
        });
    });
}