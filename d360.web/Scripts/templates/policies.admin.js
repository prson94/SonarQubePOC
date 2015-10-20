function policies_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/policies/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var type = 'PolicyType';
        var policyTypeID = 0;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var PolicyTypeSource;
        var PolicyTypeAdapter;

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

                policyTypeID = data.ID;

                $('#SideIcons').PageTools("reload", type, policyTypeID);
                DetailTile('DetailTile', contextList, permissions, type, policyTypeID);
                PolicyTypeLevelsGrid('LevelsTile', contextList, permissions, policyTypeID);
                FieldsGrid("FieldsTile", contextList, permissions, type, policyTypeID, 'Policy Type Definition');
                $('#ClaimsTile').load('/parts/ResponsibilityTypeObjectClaimGrid?type=' + type + '&id=' + policyTypeID);
                PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, policyTypeID, 'Default Responsibilities', true);
            }
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.PolicyType:
                        $('#List').jqxGrid('updatebounddata');
                        amplify.publish("RefreshNavigation");
                        break;
                    case contextList.PolicyTypeLevel:
                        PolicyTypeLevelsGrid('LevelsTile', contextList, permissions, policyTypeID);
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            PolicyTypeAdapter = null;
            PolicyTypeSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $('#List').off('rowselect', listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'policies.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: "/form/AddPolicyType", context: contextList.PolicyType, title: 'Add policy type' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    PolicyTypeSource = {
                        datatype: 'json',
                        url: '/api/policytypes',
                        datafields: [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'Description' }
                        ]
                    };

                    PolicyTypeAdapter = new $.jqx.dataAdapter(PolicyTypeSource);

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
                        source: PolicyTypeAdapter,
                        theme: list_theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                cellsrenderer: function (row, column, value) {

                                    var tools = [];
                                    if (permissions.HasPermission("Root", "Update")) {
                                        tools = [
                                            { isitemlink: true, urlprefix: '#/policies/{0}' },
                                            { icon: 'pencil', urlprefix: '/form/EditPolicyType?id={0}' },
                                            { icon: 'trash-o', urlprefix: '/form/DeletePolicyType?id={0}' }
                                        ];
                                    }

                                    return renderToolsHtml(value, tools, contextList.PolicyType);
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
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}