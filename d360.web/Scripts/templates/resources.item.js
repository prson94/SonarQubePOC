function resources_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/resources/:id', function (context) {
        context.app.swap('');

        var type = 'Resource';
        var id = context.params['id'];

        $.getJSON('/api/resources/1/' + id, function (model) {

            pageViewModel.ID = id;
            pageViewModel.Title = model.FirstName + ' ' + model.LastName;
            context.title(pageViewModel.Title);

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Resources' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            var ProfileSocial;
            var socialTile;

            //#region Event Handlers

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        ResourceStatisticsTile('SocialTile', type, id);
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
                            ResourceStatisticsTile('SocialTile', type, id);
                            break;
                    }
                } catch (e) {
                    logError("resources.item : SaveAction", e);
                }
            }

            function unsubscribe(data) {
                ProfileSocial = null;
                socialTile = null;

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

                    YourFollowedItemsTile('#FollowingTile', id, 'Items User Follows');
                    YourOwnedItemsTile('#OwnedTile', id, 'Items User Owns');

                    ResourceStatisticsTile('SocialTile', type, id);

                    ProfileSocial = new BoardViewModel();
                    ko.applyBindings(ProfileSocial, document.getElementById('ProfileBoard'));
                    ProfileSocial.changeObject(type, id);

                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                });
        });
    });
}