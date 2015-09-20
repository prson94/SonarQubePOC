function surveys_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/surveys/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'SurveyType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var SurveyTypesSource;
        var SurveyTypesAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;
            var row = args.rowindex;

            var data = $('#List').jqxGrid('getrowdata', row);

            if (data) {
                amplify.publish(AmplifyActions.TileUnsubscribe, {});
                $('#SideIcons').PageTools("reload", type, data.ID);
                DetailTile('DetailTile', contextList, permissions, type, data.ID);
                $('#QuestionsTile').load('/resources/surveys/' + data.ID + '/questions');
                $('#EntriesTile').load('/resources/surveys/' + data.ID + '/entries');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.SurveyType:
                        DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            SurveyTypesAdapter = null;
            SurveyTypesSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'surveys.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                permissions.GetPermissionsForObject(type, 0);

                //#region Grid

                SurveyTypesSource = {
                    datatype: 'json',
                    url: '/api/surveys',
                    datafields: [
                        { name: 'ID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'AllowMultiple' },
                        { name: 'ObjectType', type: 'string' }
                    ]
                };

                SurveyTypesAdapter = new $.jqx.dataAdapter(SurveyTypesSource);

                $("#List").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    source: SurveyTypesAdapter,
                    theme: theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        {
                            datafield: "ID",
                            text: "",
                            width: 80,
                            cellsrenderer: function (row, column, value) {
                                var tools = [
                                    { icon: 'pencil', urlprefix: '/form/surveys/{0}/edit' },
                                    { icon: 'trash-o', urlprefix: '/form/surveys/{0}/delete' }
                                ];
                                return renderToolsHtml(value, tools, contextList.SurveyType);
                            }
                        }
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $("#List").on("bindingcomplete", listBindingComplete);
                $('#List').on('rowselect', listRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}