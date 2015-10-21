function home(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/', function (context) {
        context.app.swap('');

        var type = "Resource";
        var id = currentResourceID;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var homeSocialTile;
        var HomeSocial;
        var ResponsibilityAdapter;
        var ResponsibilitySource;

        //#region Event Handlers

        function unsubscribe(data) {
            HomeSocial = null;
            homeSocialTile = null;
            ResponsibilityAdapter = null;
            ResponsibilitySource = null;

            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context.title(pageViewModel.Title);
        context
            .render(templatePath + 'home.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'Resource', id: currentResourceID });
                $('#SideIcons').PageTools("clear");

                //#region Tiles

                HomeSocial = new BoardViewModel();
                ko.applyBindings(HomeSocial, document.getElementById('HomeBoard'));
                HomeSocial.getMoreComments();

                YourFollowedItemsTile('#FollowingTile', id, 'Items You Follow');
                YourOwnedItemsTile('#OwnedTile', id, 'Items You Own');

                homeSocialTile = new HomeSocialMicroTileModel(id);
                ko.applyBindings(homeSocialTile, document.getElementById('HomeSocialTile'));
                homeSocialTile.GetStatistics();

                YourWorkflowTasks('WorkflowTasksTile', 'Your Assigned Tasks', true);

                //#endregion

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}