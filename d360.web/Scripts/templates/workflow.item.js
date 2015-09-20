function workflow_item(app, pageViewModel, templatePath, contextList) {
    app.get('#/workflow/:id', function (context) {
        context.app.swap('');

        var workflowID = context.params['id'];

        pageViewModel.Title = "Perform Workflow Task";
        pageViewModel.Directions = "";
        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: "Tasks", Active: true });

        var permissions = new PermissionsModel();

        context.title(pageViewModel.Title);

        //#region Event Handlers

        function saveAction(data) {
            try {
                switch (data.context) {
                    case "Workflow":
                        location.assign('/#/');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            amplify.unsubscribe('SaveAction', saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'workflow.item.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: null, id: null, context: null });

                $('#Detail').load('/workflow/' + workflowID + '/overlay');

                amplify.subscribe("SaveAction", saveAction );
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}