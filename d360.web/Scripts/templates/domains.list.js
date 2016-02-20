function domains_list(app, pageViewModel, templatePath, contextList) {
    var routeDomain = function (context) {
        context.app.swap('');

        var typeID = context.params['typeid'];
        var selectedID = context.params['id'];
        var type = 'DomainType';
        var permissions = new PermissionsModel();

        $.getJSON('/api/domains/' + typeID, function (json) {

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Domains' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            context.title(pageViewModel.Title);

            //#region Event Handlers

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        var item = $('#Tree').jqxTree('getSelectedItem');
                        var node = $(item.element).find("span.node");
                        var arr = node.data("id").split('|');
                        var oID = arr[1];
                        var oType = arr[0];
                        ObjectStatisticsTile('SocialTile', oType, oID);
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools('refresh');//("reload", 'Domain', selectedID);
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.Domain:
                        case contextList.DomainGroup:
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
                            break;
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTile', 'Domain', selectedID, permissions);
                            break;
                    }
                }
                catch (e) {
                }
            }

            var selectNodeTiles = function (selectedType, id) {
                selectedID = id;    //Save the currently selected ID to variable for possible later use.
                var load = (selectedType == "Domain");

                var disabledClass = "tile-disabled";
                var fadoutTime = 500;

                if (load) {
                    var loadPermissionsDependentTiles = function () {
                        $('#AllocationsTile').fadeIn(fadoutTime);
                        DomainAllocationsTile('AllocationsTile', contextList, permissions, typeID, id);
                        $('#ItemsTile').fadeIn(fadoutTime);
                        DomainItemsTile('ItemsTile', contextList, permissions, typeID, id);
                        $('#OwnerTile').fadeIn(fadoutTime);
                        PeopleResponsibilityTile('OwnerTile', contextList, permissions, selectedType, id, '', false);

                        $('#AggregatesTile').fadeIn(fadoutTime);
                        RelationshipAggregatesTile('AggregatesTile', selectedType, id, permissions);
                    }
                    permissions.GetPermissionsForObject(selectedType, id).then(loadPermissionsDependentTiles);
                }
                else {
                    $('#AllocationsTile').fadeOut(fadoutTime).html('');
                    $('#ItemsTile').fadeOut(fadoutTime).html('');
                    $('#OwnerTile').fadeOut(fadoutTime).html('');
                    $('#AggregatesTile').fadeOut(fadoutTime).html('');
                }

                ObjectDetail('DetailTile', selectedType, id);//DetailTile('DetailTile', contextList, permissions, selectedType, id);

                ObjectStatisticsTile('SocialTile', selectedType, id);
            }

            function treeSelect(evt) {
                var args = evt.args;
                var row = args.row;

                var oID = row.ID;
                var oType = row.Type;

                amplify.publish(AmplifyActions.TileUnsubscribe, {});

                if (oType == 'Domain')
                    $('#SocialTile').show();
                else
                    $('#SocialTile').hide();

                if (oID > 0) {
                    $('#SideIcons').PageTools("reload", oType, oID, 'default');
                    selectNodeTiles(oType, oID);
                }
            }

            function unsubscribe(data) {
                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
                amplify.unsubscribe("SaveAction", saveAction);
                $("#Tree").off('rowSelect', treeSelect);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'domains.list.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: type, id: typeID, context: 'root' });

                    var DomainTreeGridSource =
                     {
                         dataType: 'json',
                         url: '/domains/hierarchy?id=' + typeID,
                         dataFields: [
                             { name: 'ID', type: 'number' },
                             { name: 'Type', type: 'string' },
                             { name: 'HierarchyID', type: 'string' },
                             { name: 'ParentHierarchyID', type: 'string' },
                             { name: 'Name', type: 'string' },
                             { name: 'expanded', type: 'bool' },
                         ],
                         hierarchy: {
                             keyDataField: { name: 'HierarchyID' },
                             parentDataField: { name: 'ParentHierarchyID' }
                         },
                         id: 'HierarchyID'
                     };

                    var DomainTreeGridAdapter = new $.jqx.dataAdapter(DomainTreeGridSource);

                    var loadAfterPermissionsRetrieved = function () {

                        var tools = [];
                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'plus', uri: '/form/AddDomainGroup?typeID=' + typeID, context: contextList.DomainGroup, title: 'Add grouping' });
                        }
                        TileTools('#TreeTools', tools);

                        //#region Tree

                        $("#Tree").jqxTreeGrid(
                        {
                            width: '100%',
                            height: $(window).innerHeight() - 250,
                            source: DomainTreeGridAdapter,
                            filterable: true,
                            theme: theme,
                            altRows: true,
                            showHeader: false,
                            columns: [
                              { text: 'Name', dataField: 'Name' },
                              {
                                  text: '', dataField: 'ID', width: '120px', filterable: false,
                                  cellsRenderer: function (row, column, value, data) {
                                      var tools = [];

                                      var addUrl = '';
                                      var editUrl = '';
                                      var deleteUrl = '';

                                      if (data.Type == 'DomainGroup') {
                                          addUrl = '/form/AddDomain?typeID=' + typeID + '&groupID=' + data.ID;
                                          editUrl = '/form/EditDomainGroup?id=' + data.ID;
                                          deleteUrl = '/form/DeleteDomainGroup?id=' + data.ID;
                                          if (permissions.HasPermission("Root", "Create")) {
                                              tools.push({ icon: 'plus', urlprefix: addUrl, title: 'Add domain to grouping' });
                                          }
                                          if (permissions.HasPermission("Root", "Update")) {
                                              tools.push({ icon: 'pencil', urlprefix: editUrl, title: 'Edit grouping' });
                                          }
                                          if (permissions.HasPermission("Root", "Delete")) {
                                              tools.push({ icon: 'trash-o', urlprefix: deleteUrl, title: 'Remove grouping' });
                                          }
                                      }
                                      else {
                                          editUrl = '/form/EditDomain?id=' + data.ID;
                                          deleteUrl = '/form/DeleteDomain?id=' + data.ID;
                                          if (permissions.HasPermission("Root", "Update")) {
                                              tools.push({ icon: 'pencil', urlprefix: editUrl, title: 'Edit domain' });
                                          }
                                          if (permissions.HasPermission("Root", "Delete")) {
                                              tools.push({ icon: 'trash-o', urlprefix: deleteUrl, title: 'Remove domain' });
                                          }
                                      }

                                      return renderToolsHtml(value, tools, contextList.ArtifactType, data);
                                  }
                              }
                            ],
                            ready: function () {
                                try {
                                    if (selectedID) {
                                        $("#Tree").jqxTreeGrid('selectRow', 'Domain|' + selectedID);
                                    }
                                    else {
                                        var rows = $("#Tree").jqxTreeGrid('getRows');
                                        if (rows.length > 0) {
                                            var firstRow = $("#Tree").jqxTreeGrid('getRows')[0];
                                            $("#Tree").jqxTreeGrid('selectRow', firstRow.uid);
                                        }
                                    }
                                } catch (e) {
                                    console.log(e);
                                }
                            }
                        });

                        //#endregion

                        //#region Event Subscriptions

                        amplify.subscribe("CommandExecuted", commandExecuted);
                        amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                        amplify.subscribe("SaveAction", saveAction);
                        $(document).on('resize', function () {
                            $('#Tree').jqxTreeGrid({ height: $(window).innerHeight() - 250 });
                        });
                        $("#Tree").on("rowSelect", treeSelect);
                        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                        //#endregion
                    }

                    permissions.GetPermissionsForObject(type, typeID).then(loadAfterPermissionsRetrieved);
                });
        });
    }

    app.get('#/domains/:typeid/:id', routeDomain);
    app.get('#/domains/:typeid', routeDomain);
}