function templates_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/templates/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        context
            .render(templatePath + 'templates.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'EmailTemplate', id: 0 });
                //$('#EmailTile').load('/resources/templates/email');
                $('#TooltipTile').load('/resources/templates/tooltip');


                //amplify.subscribe(AmplifyActions.Unsubscribe, function (data) {
                //});
            });
    });
}