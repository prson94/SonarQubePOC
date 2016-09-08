function relations_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/relations/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'IntersectType';
        var intersectTypeID = null;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var IntersectTypeSource;
        var IntersectTypeAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxDataTable('getRows').length;
            if (rowCount > 0) {
                $('#List').jqxDataTable('selectRow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;  // event args.
            var data = args.row;    // row data.
            if (data) {
                amplify.publish(AmplifyActions.TileUnsubscribe, {});

                $('#SideIcons').PageTools("reload", type, data.ID);
                FieldsGrid("FieldsTile", contextList, permissions, type, data.ID);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.IntersectType:
                        $('#List').jqxDataTable('updateBoundData');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            IntersectTypeAdapter = null;
            IntersectTypeSource = null;

            //$('#List').off('bindingComplete', listBindingComplete);
            $('#List').off('rowSelect', listRowSelect);
            amplify.unsubscribe('SaveAction', saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'relations.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    //#region Grid

                    var listTools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        listTools.push({ icon: 'plus', uri: '/form/AddIntersectType', context: contextList.IntersectType, title: 'Add relationship type' });
                    }
                    TileTools('#ListTools', listTools);
 
                    IntersectTypeSource = {
                        datatype: 'json',
                        url: '/relations/_IntersectTypes',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Subject' },
                            { name: 'SubjectID' },
                            { name: 'SubjectName' },
                            { name: 'PredicateID' },
                            { name: 'PredicateName' },
                            { name: 'Object' },
                            { name: 'ObjectID' },
                            { name: 'ObjectName' }
                        ]
                    };

                    var IntersectTypeAdapter = new $.jqx.dataAdapter(IntersectTypeSource);

                    $("#List").jqxDataTable({
                        pageable: true,
                        pagerButtonsCount: 10,
                        altRows: true,
                        filterable: true,
                        pagerMode: 'advanced',
                        width: '100%',
                        filterMode: 'simple',
                        source: IntersectTypeAdapter,
                        theme: theme,
                        columnsResize: true,
                        columns: [
                            {
                                dataField: "SubjectName",
                                text: "Subject",
                                cellsRenderer: function (row, column, value, rowData) {
                                    return rowData.SubjectName + ' <span style="font-size: 75%; color: #999">(' + rowData.Subject.replace("Type", "") + ')</span>';
                                }
                            },
                            { dataField: "PredicateName", text: "Predicate", width: '15%' },
                            {
                                dataField: "ObjectName",
                                text: "Object",
                                cellsRenderer: function (row, column, value, rowData) {
                                    return rowData.ObjectName + ' <span style="font-size: 75%; color: #999">(' + rowData.Object.replace("Type", "") + ')</span>';
                                }
                            },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 100,
                                filterable: false,
                                cellsRenderer: function (row, column, value, rowData) {

                                    var tools = [];
                                    if (permissions.HasPermission('Root', 'Update')) {
                                        tools = [
                                            { icon: 'pencil', urlprefix: '/form/EditIntersectType?id={0}' },
                                            { icon: 'trash-o', urlprefix: '/form/DeleteIntersectType?id={0}' }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.IntersectType);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $('#List').one('bindingComplete', listBindingComplete);//$('#List').on('bindingComplete', listBindingComplete);
                    $('#List').on('rowSelect', listRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}