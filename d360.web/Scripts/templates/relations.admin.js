function relations_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/relations/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'IntersectType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var IntersectTypeSource;
        var IntersectTypeAdapter;

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
            var data = $("#List").jqxGrid('getrowdata', row);
            amplify.publish(AmplifyActions.TileUnsubscribe, {});
            $('#SideIcons').PageTools("reload", type, data.ID);
            //DetailTile('DetailTile', contextList, permissions, type, data.ID);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.IntersectType:
                        //DetailTile('DetailTile', contextList, permissions, type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            IntersectTypeAdapter = null;
            IntersectTypeSource = null;

            $("#List").off("rowselect", listRowSelect);
            $("#List").off("bindingcomplete", listBindingComplete);
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
                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddIntersectType', context: contextList.IntersectType, title: 'Add relationship type' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid
 
                    IntersectTypeSource = {
                        datatype: 'json',
                        url: '/relations/_IntersectTypes',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'SourceType' },
                            { name: 'SourceID' },
                            { name: 'SourceTypeName' },
                            { name: 'SourceName' },
                            { name: 'TargetType' },
                            { name: 'TargetID' },
                            { name: 'TargetTypeName' },
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
                        columngroups: 
                            [
                                { text: 'Relationship Side 1', align: 'center', name: 'S1' },
                                { text: 'Relationship Side 2', align: 'center', name: 'S2' }
                            ],
                        columns: [
                            { datafield: "SourceTypeName", text: "Type", columngroup: 'S1', filtertype: 'checkedlist', width: '150px' },
                            { datafield: "SourceName", text: "Name", columngroup: 'S1', filtertype: 'checkedlist' },
                            { datafield: "TargetTypeName", text: "Type", columngroup: 'S2', filtertype: 'checkedlist', width: '150px' },
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

                    //#region Event Subscriptions

                    $("#List").on("rowselect", listRowSelect);
                    $("#List").one("bindingcomplete", listBindingComplete);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}