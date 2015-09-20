function artifacts_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/administration', function (context) {
        context.app.swap('');

        var type = 'ArtifactType';
        var permissions = new PermissionsModel();

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        context.title(pageViewModel.Title);

        //#region Event Handlers

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.ArtifactType:
                        $('#Tree').one('bindingComplete', function (event) {
                            var selectActiveRow = false;

                            if (data) {
                                if (data.id) {
                                    selectActiveRow = true;
                                }
                            }

                            if (selectActiveRow) {
                                try {
                                    $("#Tree").jqxTreeGrid('selectRow', data.id);
                                } catch (e) { }
                            }
                            else {
                                var rows = $("#Tree").jqxTreeGrid('getRows');
                                if (rows.length > 0) {
                                    var firstRow = $("#Tree").jqxTreeGrid('getRows')[0];
                                    $("#Tree").jqxTreeGrid('selectRow', firstRow.uid);
                                }
                            }
                        });
                        $("#Tree").jqxTreeGrid('updateBoundData');
                        amplify.publish("RefreshNavigation");
                        break;
                }
            }
            catch (e) {
            }
        }

        function treeSelect(evt) {
            try {
                var args = evt.args;
                var row = args.row;

                amplify.publish(AmplifyActions.TileUnsubscribe, {});

                if (row.ID > 0) {
                    $('#ClaimsTile').load('/parts/ResponsibilityTypeObjectClaimGrid?type=' + type + '&id=' + row.ID);
                    $('#SideIcons').PageTools("reload", type, row.ID);

                    var loadPermissionsDependentTiles = function () {
                        //DetailTile('DetailTile', contextList, permissions, type, row.ID);
                        FieldsGrid("FieldsTile", contextList, permissions, type, row.ID, 'Artifact Definition');
                        PeopleResponsibilityTile('SecurityTile', contextList, permissions, type, row.ID, 'Default Responsibilities', true);
                    }
                    permissions.GetPermissionsForObject(type, row.ID).then(loadPermissionsDependentTiles);
                }
                else {
                    $('#SideIcons').PageTools("reload", type, 0);
                    //$('#DetailTile').html('');
                }
            }
            catch (e) {
                console.log(e);
            }
        }

        function unsubscribe(data) {
            $("#Tree").off("rowSelect", treeSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'artifacts.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var ArtifactTreeGridSource =
                {
                    dataType: 'json',
                    url: '/artifacts/types',
                    dataFields: [
                        { name: 'ID', type: 'number' },
                        { name: 'ParentID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'expanded', type: 'bool' },
                    ],
                    hierarchy: {
                        keyDataField: { name: 'ID' },
                        parentDataField: { name: 'ParentID' }
                    },
                    id: 'ID'
                };

                var ArtifactTreeGridAdapter = new $.jqx.dataAdapter(ArtifactTreeGridSource);

                var loadAfterPermissionsRetrieved = function () {

                    //$('#DetailTile').Detail({ context: contextList.ArtifactType, id: null, type: null });

                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddArtifactType?parentID=0', context: contextList.ArtifactType, title: 'Add artifact type' });
                    }
                    TileTools('#TreeTools', tools);

                    //#region TreeGrid

                    $("#Tree").jqxTreeGrid(
                    {
                        width: '100%',
                        height: $(window).innerHeight() - 250,
                        source: ArtifactTreeGridAdapter,
                        filterable: true,
                        theme: theme,
                        altRows: true,
                        showHeader: false,
                        columns: [
                          { text: 'Name', dataField: 'Name' },
                          {
                              text: '', dataField: 'ID', width: '160px', filterable: false,
                              cellsRenderer: function (row, column, value, data) {
                                  var tools = [];

                                  if (permissions.HasPermission("Root", "Create")) {
                                      tools.push({ icon: 'plus', urlprefix: '/form/AddArtifactType?parentID={0}', title: 'Add child artifact type' });
                                  }
                                  if (permissions.HasPermission("Root", "Update")) {
                                      tools.push({ icon: 'pencil', urlprefix: '/form/EditArtifactType?id={0}', title: 'Edit artifact type' });
                                  }
                                  if (permissions.HasPermission("Root", "Delete")) {
                                      tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteArtifactType?id={0}', title: 'Remove artifact type' });
                                  }
                                  tools.push({ isitemlink: true, urlprefix: '#/artifacts/{0}', type: 'ArtifactType', context: 'Preview' });

                                  return renderToolsHtml(value, tools, contextList.ArtifactType, data);
                              }
                          }
                        ],
                        ready: function () {
                            try {
                                var rows = $("#Tree").jqxTreeGrid('getRows');
                                if (rows.length > 0) {
                                    var firstRow = $("#Tree").jqxTreeGrid('getRows')[0];
                                    $("#Tree").jqxTreeGrid('selectRow', firstRow.uid);
                                }
                            } catch (e) {
                                console.log(e);
                            }
                        }
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $(document).on('resize', function () {
                        $('#Tree').jqxTreeGrid({ height: $(window).innerHeight() - 250 });
                    });
                    $("#Tree").on("rowSelect", treeSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}