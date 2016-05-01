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

        //var RelationTypeSource;
        //var RelationTypeAdapter;
        var IntersectTypeSource;
        var IntersectTypeAdapter;
        var PredicateSource;
        var PredicateAdapter;

        //#region Event Handlers

        function listRowSelect(event) {

            // event args.
            var args = event.args;
            // row data.
            var data = args.row;

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
                    case contextList.Predicate:
                        $('#Predicates').jqxDataTable('updateBoundData');
                        break;
                    //case contextList.RelationType:
                    //    if (CompanySettings.UseNewRelationships == "true") {
                    //        $('#NewRelationTypes').jqxDataTable('updateBoundData');
                    //    }
                    //    break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            //RelationTypeAdapter = null;
            //RelationTypeSource = null;
            IntersectTypeAdapter = null;
            IntersectTypeSource = null;
            PredicateAdapter = null;
            PredicateSource = null;

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
                            { name: 'Source' },
                            { name: 'SourceID' },
                            { name: 'SourceName' },
                            { name: 'Target' },
                            { name: 'TargetID' },
                            { name: 'TargetName' }
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
                        columnGroups: [
                            { text: 'Relationship Side 1', align: 'center', name: 'S1' },
                            { text: 'Relationship Side 2', align: 'center', name: 'S2' }
                        ],
                        columns: [
                            { dataField: "Source", text: "Type", columnGroup: 'S1', width: '125px' },
                            { dataField: "SourceName", text: "Name", columnGroup: 'S1' },
                            { dataField: "Target", text: "Type", columnGroup: 'S2', width: '125px' },
                            { dataField: "TargetName", text: "Name", columnGroup: 'S2' },
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

                    //#region PredicateGrid

                    var predicateTools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        predicateTools.push({ icon: 'plus', uri: '/form/AddPredicate', context: contextList.Predicate, title: 'Add predicate' });
                    }
                    TileTools('#PredicateTools', predicateTools);

                    PredicateSource = {
                        datatype: 'json',
                        url: '/relations/Predicates',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Inverse' },
                            { name: 'Type' }
                        ]
                    };

                    PredicateAdapter = new $.jqx.dataAdapter(PredicateSource);

                    $("#Predicates").jqxDataTable({
                        pageable: true,
                        pagerButtonsCount: 10,
                        altRows: true,
                        filterable: true,
                        pagerMode: 'advanced',
                        width: '100%',
                        filterMode: 'simple',
                        source: PredicateAdapter,
                        theme: theme,
                        columnsResize: true,
                        columns: [
                            { dataField: "Name", text: "Name" },
                            { dataField: "Inverse", text: "Inverse" },
                            { dataField: "Type", text: "Type" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 100,
                                cellsRenderer: function (row, column, value, rowData) {

                                    var tools = [];
                                    if (permissions.HasPermission('Root', 'Update')) {
                                        tools = [
                                            { icon: 'pencil', urlprefix: '/form/EditPredicate?id={0}' },
                                            { icon: 'trash-o', urlprefix: '/form/DeletePredicate?id={0}' }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.Predicate);
                                }
                            }
                        ]
                    });

                    //#endregion

                    //if (CompanySettings.UseNewRelationships == "true") {

                    //    $('#NewRelationTypesWrapper').show();

                    //    //#region Grid

                    //    var newRelationTypesTools = [];
                    //    if (permissions.HasPermission("Root", "Create")) {
                    //        newRelationTypesTools.push({ icon: 'plus', uri: '/form/AddRelationType', context: contextList.IntersectType, title: 'Add relation type' });
                    //    }
                    //    TileTools('#NewRelationTypesTools', newRelationTypesTools);

                    //    RelationTypeSource = {
                    //        datatype: 'json',
                    //        url: '/services/relationships/types',
                    //        datafields:
                    //        [
                    //            { name: 'ID' },
                    //            { name: 'Subject' },
                    //            { name: 'SubjectID' },
                    //            { name: 'SubjectName' },
                    //            { name: 'Object' },
                    //            { name: 'ObjectID' },
                    //            { name: 'ObjectName' },
                    //            { name: 'PredicateType' },
                    //            { name: 'PredicateTypeName' }
                    //        ]
                    //    };

                    //    var RelationTypeAdapter = new $.jqx.dataAdapter(RelationTypeSource);

                    //    $("#NewRelationTypes").jqxDataTable({
                    //        pageable: true,
                    //        pagerButtonsCount: 10,
                    //        altRows: true,
                    //        filterable: true,
                    //        pagerMode: 'advanced',
                    //        width: '100%',
                    //        filterMode: 'simple',
                    //        source: RelationTypeAdapter,
                    //        theme: theme,
                    //        columnsResize: true,
                    //        columns: [
                    //            { dataField: "Subject", text: "Type", width: '125px' },
                    //            { dataField: "SubjectName", text: "Name" },
                    //            { dataField: "Object", text: "Type", width: '125px' },
                    //            { dataField: "ObjectName", text: "Name"},
                    //            { dataField: "PredicateTypeName", text: "Predicate", width: '125px' },
                    //            {
                    //                text: '',
                    //                dataField: 'ID',
                    //                width: 100,
                    //                cellsRenderer: function (row, column, value, rowData) {

                    //                    var tools = [];
                    //                    if (permissions.HasPermission('Root', 'Update')) {
                    //                        tools = [
                    //                            { icon: 'pencil', urlprefix: '/form/EditRelationType?id={0}' },
                    //                            { icon: 'trash-o', urlprefix: '/form/DeleteRelationType?id={0}' }
                    //                        ];
                    //                    }

                    //                    return renderToolsHtml(value, tools, contextList.IntersectType);
                    //                }
                    //            }
                    //        ]
                    //    });

                    //    //#endregion

                    //}

                    //#region Event Subscriptions

                    $('#List').on('rowSelect', listRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}