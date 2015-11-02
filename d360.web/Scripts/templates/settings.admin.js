function settings_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/settings', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        //#region Event Handlers

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Role:
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {

            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'settings.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                var model = new CompanySettingsViewModel();
                ko.applyBindings(model, document.getElementById('SettingsModel'));
                model.loadCurrentSettings();


                //#region Event Subscriptions

                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}