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

        var RelationTypeSource;
        var RelationTypeAdapter;
        var IntersectTypeSource;
        var IntersectTypeAdapter;
        var PredicateSource;
        var PredicateAdapter;

        //#region Event Handlers

        function listRowSelect(event) {

            var args = event.args;
            var row = args.rowindex;

            var data = $('#List').jqxGrid('getrowdata', row);

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
                        $('#List').jqxGrid('updatebounddata');
                        break;
                    case contextList.Predicate:
                        $('#Predicates').jqxGrid('updatebounddata');
                        break;
                    case contextList.RelationType:
                        if (CompanySettings.UseNewRelationships) {
                            $('#NewRelationTypes').jqxGrid('updatebounddata');
                        }
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            RelationTypeAdapter = null;
            RelationTypeSource = null;
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
                        source: IntersectTypeAdapter,
                        theme: theme,
                        columnsresize: true,
                        columngroups: 
                            [
                                { text: 'Relationship Side 1', align: 'center', name: 'S1' },
                                { text: 'Relationship Side 2', align: 'center', name: 'S2' }
                            ],
                        columns: [
                            { datafield: "Source", text: "Type", columngroup: 'S1', filtertype: 'checkedlist', width: '125px' },
                            { datafield: "SourceName", text: "Name", columngroup: 'S1', filtertype: 'checkedlist' },
                            { datafield: "Target", text: "Type", columngroup: 'S2', filtertype: 'checkedlist', width: '125px' },
                            { datafield: "TargetName", text: "Name", columngroup: 'S2', filtertype: 'checkedlist' },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {

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

                    $("#Predicates").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: PredicateAdapter,
                        theme: theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            { datafield: "Inverse", text: "Inverse" },
                            { datafield: "Type", text: "Type" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {

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

                    if (CompanySettings.UseNewRelationships) {

                        $('#NewRelationTypesWrapper').show();

                        //#region Grid

                        var newRelationTypesTools = [];
                        if (permissions.HasPermission("Root", "Create")) {
                            newRelationTypesTools.push({ icon: 'plus', uri: '/form/AddRelationType', context: contextList.IntersectType, title: 'Add relation type' });
                        }
                        TileTools('#NewRelationTypesTools', newRelationTypesTools);

                        RelationTypeSource = {
                            datatype: 'json',
                            url: '/services/relationships/types',
                            datafields:
                            [
                                { name: 'ID' },
                                { name: 'Subject' },
                                { name: 'SubjectID' },
                                { name: 'SubjectName' },
                                { name: 'Object' },
                                { name: 'ObjectID' },
                                { name: 'ObjectName' },
                                { name: 'PredicateID' },
                                { name: 'Predicate' },
                                { name: 'Inverse' }
                            ]
                        };

                        var RelationTypeAdapter = new $.jqx.dataAdapter(RelationTypeSource);

                        $("#NewRelationTypes").jqxGrid({
                            altrows: true,
                            width: grid_width,
                            pagesizeoptions: ['10', '20', '50'],
                            pagesize: 20,
                            autoheight: true,
                            sortable: true,
                            filterable: true,
                            showfilterrow: true,
                            pageable: true,
                            source: RelationTypeAdapter,
                            theme: theme,
                            columnsresize: true,
                            columns: [
                                { datafield: "Subject", text: "Type", filtertype: 'checkedlist', width: '125px' },
                                { datafield: "SubjectName", text: "Name", filtertype: 'checkedlist' },
                                { datafield: "Predicate", text: "Predicate", filtertype: 'checkedlist', width: '125px' },
                                { datafield: "Object", text: "Type", filtertype: 'checkedlist', width: '125px' },
                                { datafield: "ObjectName", text: "Name", filtertype: 'checkedlist' },
                                {
                                    text: '',
                                    dataField: 'ID',
                                    width: 80,
                                    filterable: false,
                                    cellsrenderer: function (row, column, value) {

                                        var tools = [];
                                        if (permissions.HasPermission('Root', 'Update')) {
                                            tools = [
                                                { icon: 'pencil', urlprefix: '/form/EditRelationType?id={0}' },
                                                { icon: 'trash-o', urlprefix: '/form/DeleteRelationType?id={0}' }
                                            ];
                                        }

                                        return renderToolsHtml(value, tools, contextList.IntersectType);
                                    }
                                }
                            ]
                        });

                        //#endregion

                    }

                    //#region Event Subscriptions

                    $('#List').on('rowselect', listRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}