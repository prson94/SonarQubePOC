function taxonomies_list(app, pageViewModel, templatePath, contextList) {
    var routeTaxonomy = function (context) {
        context.app.swap('');

        var type = 'Taxonomy';
        var typeID = context.params['typeid'];
        var selectedID = context.params['id'];
        var permissions = new PermissionsModel();

        $.getJSON('/api/catalogs/' + typeID, function (json) {

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Information Models' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            context.title(pageViewModel.Title);

            var selectTreeRowByID = function (selectedID) {
                var row = $("#Tree").jqxTreeGrid('getRow', selectedID);
                var rowParent = row.parent;
                var rowsToExpand = [];

                while (rowParent) {
                    rowsToExpand.push(rowParent.uid);
                    rowParent = rowParent.parent;
                }

                rowsToExpand.reverse();
                $.each(rowsToExpand, function () {
                    $("#Tree").jqxTreeGrid('expandRow', this);
                });

                $("#Tree").jqxTreeGrid('ensureRowVisible', selectedID);
                $("#Tree").jqxTreeGrid('selectRow', selectedID);
            }

            //#region Event Handlers

            function bindingComplete(event) {
                try {
                    var rows = $("#Tree").jqxTreeGrid('getRows');
                    if (rows.length > 0) {
                        if (selectedID > 0) {
                            selectTreeRowByID(selectedID);
                        }
                        else {
                            var firstRow = $("#Tree").jqxTreeGrid('getRows')[0];
                            $("#Tree").jqxTreeGrid('selectRow', firstRow.uid);
                        }
                    }
                    else {
                        $('#SideIcons').PageTools({ type: type, id: typeID, context: 'root' });
                    }
                } catch (e) {
                    console.log(e);
                }
            }

            function commandExecuted(commandName) {
                switch (commandName) {
                    case 'follow':
                        var item = $('#Tree').jqxTreeGrid('getSelection')[0];
                        ObjectStatisticsTile('StatisticsTile', type, item.ID);
                        break;
                }
            }

            function refreshActionMenu(data) {
                $('#SideIcons').PageTools("reload", 'Taxonomy', selectedID);
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.Comment:
                            var item = $('#Tree').jqxTreeGrid('getSelection')[0];
                            ObjectStatisticsTile('StatisticsTile', type, item.ID);
                            break;
                        case contextList.Intersect:
                            RelationshipAggregatesTile('AggregatesTileContainer', type, selectedID, permissions);
                            break;
                        case contextList.SourcingResponsibility:
                            environment_diagram('SourcingTile', permissions, type, selectedID);
                            break;
                        case contextList.Synonym:
                            $('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                        case contextList.Taxonomy:
                            switch (data.action) {
                                case 'add':
                                case 'edit':
                                    selectedID = data.id;
                                    //selectTreeRowByID(data.id);
                                    break;
                                case 'delete':
                                    selectedID = data.custom.ParentID;
                                    //selectTreeRowByID(data.custom.ParentID);
                                    break;
                            }
                            $("#Tree").jqxTreeGrid('updateBoundData');
                            break;
                    }
                } catch (e) { }
            }

            function treeSelect(evt) {

                // event args.
                var args = evt.args;
                // row data.
                var row = args.row;
                // row key.         var key = args.key;

                //var node = $(evt.args.element).find("span[data-i]:first");

                //var item = $('#Tree').jqxTree('getItem', evt.args.element);

                selectedID = row.ID;//node.data("i");
                var parentid = row.ParentID;//node.data("p");
                var ctx = contextList.Taxonomy;//node.data("c");

                if (selectedID > 0) {
                    $('#SideIcons').PageTools("reload", type, selectedID, ctx);

                    if (ctx == contextList.Taxonomy) {

                        $.getJSON('/api/Taxonomy/' + selectedID + '/flags', function (flagdata) {
                            pageViewModel.RedFlagged = flagdata.RedFlagged;
                            context.contentHeader(pageViewModel);
                        });

                        var loadPermissionsDependentTiles = function () {
                            amplify.publish(AmplifyActions.TileUnsubscribe, {});

                            DetailsTile('DetailTile', contextList, permissions, type, selectedID, contextList.Taxonomy);
                            ObjectStatisticsTile('StatisticsTile', type, selectedID);
                            PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, selectedID, '', false);
                            environment_diagram('SourcingTile', permissions, type, selectedID);
                            RelationshipAggregatesTile('AggregatesTileContainer', type, selectedID, permissions);
                        }

                        permissions.GetPermissionsForObject(type, selectedID).then(loadPermissionsDependentTiles);
                    }
                }
            }

            function unsubscribe(data) {
                $('#Tree').off('bindingComplete', bindingComplete);
                amplify.unsubscribe("CommandExecuted", commandExecuted);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
                amplify.unsubscribe("SaveAction", saveAction);
                $('#Tree').off('rowSelect', treeSelect);//$("#Tree").off("select", treeSelect);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'taxonomies.list.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {

                    context.contentHeader(pageViewModel);

                    $('#SideIcons').PageTools({ type: 'Taxonomy', id: typeID, context: 'root' });

                    var updateNodeTitleIndicators = function (name) {
                        var dropDownContent = '<div style="position: relative; margin-left: 3px; margin-top: 5px;">' + name + '</div>';
                        $('#BreadcrumbActiveName').text(name);
                    }

                    //#region Event Subscriptions
                        
                    $('#Tree').on('bindingComplete', bindingComplete);
                    amplify.subscribe("CommandExecuted", commandExecuted);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    amplify.subscribe("SaveAction", saveAction);
                    $('#Tree').on('rowSelect', treeSelect);//$("#Tree").on("select", treeSelect);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion

                    //#region TreeGrid

                    var TreeGridSource =
                    {
                        dataType: 'json',
                        url: '/taxonomy/ModelHierarchy?id=' + typeID,
                        dataFields: [
                            { name: 'HasChildren', type: 'bool' },
                            { name: 'ID', type: 'number' },
                            { name: 'ParentID', type: 'number' },
                            { name: 'Name', type: 'string' }
                        ],
                        hierarchy: {
                            keyDataField: { name: 'ID' },
                            parentDataField: { name: 'ParentID' }
                        },
                        id: 'ID'
                    };

                    var TreeGridAdapter = new $.jqx.dataAdapter(TreeGridSource);

                    $("#Tree").jqxTreeGrid(
                    {
                        width: '100%',
                        height: $(window).innerHeight() - 250,
                        source: TreeGridAdapter,
                        filterable: true,
                        theme: theme,
                        showHeader: false,
                        columns: [
                          {
                              text: 'Name', dataField: 'Name',
                              cellsRenderer: function (row, column, value, data) {
                                  return (data.HasChildren ? '<b>' : '') + value + (data.HasChildren ? '</b>' : '');
                              }
                          }
                        ]
                    });

                    //#endregion
                });
        });
    };

    app.get('#/catalogs/:typeid/:id', routeTaxonomy);
    app.get('#/catalogs/:typeid', routeTaxonomy);
}