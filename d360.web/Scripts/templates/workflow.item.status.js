function workflow_item_status(app, pageViewModel, templatePath, contextList) {
    app.get('#/workflow/:id/status', function (context) {
        context.app.swap('');

        var workflowID = context.params['id'];

        pageViewModel.Title = "Workflow Item Status";
        pageViewModel.Directions = "";
        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: "Tasks", Active: true });

        var permissions = new PermissionsModel();

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

        context.title(pageViewModel.Title);
        context
            .render(templatePath + 'workflow.item.status.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: null, id: null, context: null });

                $.getJSON(
                    '/services/workflow/' + workflowID + '/status',
                    function (data) {
                        var workflowStatusTileSource = $("#workflowStatusTile").html();
                        var workflowStatusTileTemplate = Handlebars.compile(workflowStatusTileSource);
                        $('#Detail').html(
                            workflowStatusTileTemplate(data)
                        );
                    }
                );

                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

            });
    });
}