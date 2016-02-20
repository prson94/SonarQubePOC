function workflow_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/workflow/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
        
        var type = 'WorkflowTypeRelation';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var WorkflowSource;
        var WorkflowAdapter;

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
            amplify.publish(AmplifyActions.TileUnsubscribe, {});
            
            var data = args.row;
            $('#SideIcons').PageTools("reload", type, data.ID);
            ObjectDetail('DetailTile', type, data.ID);
            //$('#AllocationsTile').load('/fields/Allocations?id=' + data.ID);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.WorkflowTypeRelation:
                        ObjectDetail('DetailTile', type, data.id);
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            FieldTypesAdapter = null;
            FieldTypesSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $("#List").off("rowselect", listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'workflow.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                permissions.GetPermissionsForObject(type, 0);

                //#region Grid

                WorkflowSource = {
                    datatype: 'json',
                    url: '/api/workflows/relations',
                    datafields:
                    [
                        { name: 'ID' },
                        { name: 'Object' },
                        { name: 'ObjectID' },
                        { name: 'ObjectName' },
                        { name: 'Parent' },
                        { name: 'ParentID' },
                        { name: 'ParentName' },
                        { name: 'WorkflowType' },
                        { name: 'Enabled' },
                        { name: 'ResponsibilityTypeID' },
                        { name: 'ResponsibilityType' },
                        { name: 'WorkflowTypeName' },
                        { name: 'WorkflowTypeDisplayName' },
                        { name: 'Properties' }
                    ]
                };

                WorkflowAdapter = new $.jqx.dataAdapter(WorkflowSource);

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
                    source: WorkflowAdapter,
                    theme: theme,
                    columns: [
                        { datafield: "WorkflowTypeDisplayName", text: "Workflow", width: '30%' },
                        { datafield: "ObjectName", text: "Type", width: '25%' },
                        { datafield: "ParentName", text: "Subject Area" },
                        {
                            text: '',
                            dataField: 'ID',
                            width: 80,
                            filterable: false,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                                var tools = [
                                    { icon: 'pencil', urlprefix: '/form/EditWorkflowAllocation?id={0}' },
                                    { icon: 'trash-o', urlprefix: '/form/DeleteWorkflowAllocation?id={0}' }
                                ];

                                return renderToolsHtml(value, tools, contextList.WorkflowTypeRelation);
                            }
                        }
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $("#List").one("bindingcomplete", listBindingComplete);
                $("#List").on("rowselect", listRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}