function fusion_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/fusion/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();
        var FusionAttributeTypesSource;
        var FusionAttributeTypesAdapter;

        //#region Event Handlers

        function fusionAttributesTypeBindingComplete(event) {
            try {
                var firstRow = $('#FusionAttributeTypes').jqxTreeGrid('getRows')[0];
                if (firstRow) {
                    var key = $("#FusionAttributeTypes").jqxTreeGrid('getKey', firstRow);
                    if (key) {
                        $('#FusionAttributeTypes').jqxTreeGrid('selectRow', key);
                    }
                }
            } catch (e) {
                console.log(e);
            }
        }

        function fusionAttributeTypesRowSelect(event) {
            var args = event.args;  // event args.
            var row = args.row;     // row data.
            var key = args.key;     // row key.

            //$('#FusionAttributeTypeFieldsTitle').text('Fields for ' + row.Name);
            FieldsGrid("FusionAttributeTypeFields", contextList, permissions, 'FusionAttributeType', args.key, 'Fields for ' + row.Name);
        }

        function fusionTypeSelected(data) {
            amplify.publish(AmplifyActions.TileUnsubscribe, {});

            $('#SideIcons').PageTools("reload", 'FusionType', data.ID);
            $('.FusionAttributeTypesTitle').text(data.Name);

            var loadPermissionsDependentTiles = function () {
                DetailTile('DetailTile', contextList, permissions, 'FusionType', data.ID);
                FieldsGrid("FieldsTile", contextList, permissions, 'FusionType', data.ID);
                FusionConfigurationsGrid('ConfigurationsTile', contextList, permissions, 'FusionType', data.ID);
                PeopleResponsibilityTile('SecurityTile', contextList, permissions, 'FusionType', data.ID, 'Default Responsibilities', true);

                $('#FusionAttributeTypeFieldsTitle').text('');
                $("#FusionAttributeTypeFields").html('');

                if (permissions.HasPermission('Root', 'Update')) {
                    TileTools('#FusionAttributeTypesTools', [
                        { icon: 'plus', uri: '/form/AddFusionAttributeType?typeID=' + data.ID, context: contextList.FusionAttributeType, title: 'Add top-level attribute type' }
                    ]);
                }

                FusionAttributeTypesSource.url = '/services/fusion/' + data.ID + '/attributetypes';
                $('#FusionAttributeTypes').jqxTreeGrid('updateBoundData');
            }
            permissions.GetPermissionsForObject('FusionType', data.ID).then(loadPermissionsDependentTiles);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.FusionAttributeType:
                        $('#FusionAttributeTypes').jqxTreeGrid('updateBoundData');
                        $('#FusionAttributeTypeFieldsTitle').text('');
                        $('#FusionAttributeTypeFields').html('');
                        break;
                }
            } catch (e) {
                logError("Fusion Administration : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            FusionAttributeTypesSource = null;
            FusionAttributeTypesAdapter = null;

            $('#FusionAttributeTypes').off('bindingComplete', fusionAttributesTypeBindingComplete);
            $('#FusionAttributeTypes').off('rowSelect', fusionAttributeTypesRowSelect);
            amplify.unsubscribe('FusionTypeSelected', fusionTypeSelected);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'fusion.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'FusionType', id: 0 });

                var loadAfterPermissionsRetrieved = function () {
                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddFusionType', context: contextList.FusionType, title: 'Add fusion type' });
                    }
                    TileTools('#ListTools', tools);

                    FusionTypesGrid('List', contextList, permissions);

                    //#region Fusion Attribute Type

                    FusionAttributeTypesSource = {
                        dataType: "json",
                        dataFields: [
                            { name: 'ID', type: 'number' },
                            { name: 'FusionTypeID', type: 'number' },
                            { name: 'ParentID', type: 'number' },
                            { name: 'Name', type: 'string' }//,
                            //{ name: 'expanded', type: 'bool' }
                        ],
                        hierarchy:
                        {
                            keyDataField: { name: 'ID' },
                            parentDataField: { name: 'ParentID' }
                        },
                        id: 'ID',
                        root: 'value',
                        url: null
                    };

                    FusionAttributeTypesAdapter = new $.jqx.dataAdapter(FusionAttributeTypesSource, {
                        beforeLoadComplete: function (records) {
                            var data = new Array();
                            for (var i = 0; i < records.length; i++) {
                                var item = records[i];
                                item.expanded = true;
                                data.push(item);
                            }
                            return data;
                        }
                    });

                    $('#FusionAttributeTypes').jqxTreeGrid(
                    {
                        altRows: true,
                        showHeader: false,
                        theme: theme,
                        width: grid_width,
                        filterable: false,
                        selectionMode: "singleRow",
                        pageable: false,
                        sortable: false,
                        source: FusionAttributeTypesAdapter,
                        columns: [
                            { text: 'Name', dataField: 'Name' },
                            { text: 'ID', dataField: 'ID', width: '10%' },
                          {
                              text: '',
                              dataField: 'FusionTypeID',
                              width: '200px',
                              cellsRenderer: function (row, column, value, rowData) {
                                  var tools = [];
                                  if (value != 0) {
                                      var tools = [
                                          { icon: 'pencil', urlprefix: '/form/EditFusionAttributeType?id=' + rowData.ID, title: 'Edit this attribute type' },
                                          { icon: 'trash-o', urlprefix: '/form/DeleteFusionAttributeType?id=' + rowData.ID, title: 'Remove this attribute type' },
                                          { icon: 'plus', urlprefix: '/form/AddFusionAttributeType?typeID=' + rowData.FusionTypeID + '&parentID=' + rowData.ID, title: 'Add attribute sub-type', text: 'Sub-type' }//,
                                          //{ icon: 'plus', urlprefix: '/form/fields/FusionAttributeType/' + rowData.ID + '/add', title: 'Add field', text: 'Field' }
                                      ];
                                  }
                                  return renderToolsHtml(value, tools, contextList.FusionAttributeType);
                              }
                          }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $('#FusionAttributeTypes').on('bindingComplete', fusionAttributesTypeBindingComplete);
                    $('#FusionAttributeTypes').on('rowSelect', fusionAttributeTypesRowSelect);
                    amplify.subscribe('FusionTypeSelected', fusionTypeSelected);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject('FusionType', 0).then(loadAfterPermissionsRetrieved);
            });
    });
}