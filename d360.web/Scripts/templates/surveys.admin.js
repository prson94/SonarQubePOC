function surveys_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/surveys/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'SurveyType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var QuestionTypesSource;
        var QuestionTypesAdapter;

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
                ObjectDetail('DetailTile', type, data.ID);

                $('#QuestionTools').html('');

                var tools = [];
                if (permissions.HasPermission("Root", "Create")) {
                    tools.push({ icon: 'plus', uri: '/form/AddQuestionType?surveyTypeID=' + data.ID, context: contextList.SurveyType, title: 'Add survey' });
                }
                TileTools('#QuestionTools', tools);

                QuestionTypesSource.url = '/api/surveys/' + data.ID + '/questions'
                $('#QuestionsTile').jqxGrid('updatebounddata');
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.SurveyType:
                        ObjectDetail('DetailTile', type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                    case contextList.QuestionType:
                        $('#QuestionsTile').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            QuestionTypesAdapter = null;
            QuestionTypesSource = null;

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

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddSurveyType', context: contextList.SurveyType, title: 'Add survey' });
                    }
                    TileTools('#ListTools', tools);

                    //#region SurveyType Grid

                    SurveyTypesSource = {
                        datatype: 'json',
                        url: '/api/surveys',
                        datafields: [
                            { name: 'ID', type: 'number' },
                            { name: 'Name', type: 'string' },
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
                                filterable: false,
                                sortable: false,
                                width: 80,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditSurveyType?id={0}' },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteSurveyType?id={0}' }
                                    ];
                                    return renderToolsHtml(value, tools, contextList.SurveyType);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region QuestionType Grid

                    QuestionTypesSource = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID', type: 'number' },
                            { name: 'Name', type: 'string' },
                            { name: 'OptionCount', type: 'number' },
                            { name: 'DisplayStyle', type: 'string' }
                        ]
                    };

                    QuestionTypesAdapter = new $.jqx.dataAdapter(QuestionTypesSource);

                    $("#QuestionsTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        pageable: true,
                        //columnresize: true,
                        source: QuestionTypesAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            { datafield: "OptionCount", text: "# Options", width: '10%' },
                            { datafield: "DisplayStyle", text: "Display", width: '10%' },
                            {
                                datafield: "ID",
                                text: "",
                                width: 80,
                                filterable: false,
                                sortable: false,
                                cellsrenderer: function (row, column, value) {
                                    var tools = [
                                        { icon: 'pencil', urlprefix: '/form/EditQuestionType?id={0}' },
                                        { icon: 'trash-o', urlprefix: '/form/DeleteQuestionType?id={0}' }
                                    ];
                                    return renderToolsHtml(value, tools, contextList.QuestionType);
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

                };

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}