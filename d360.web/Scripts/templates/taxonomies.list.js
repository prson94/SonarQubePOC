function taxonomies_list(app, pageViewModel, templatePath, contextList) {
    var routeTaxonomy = function (context) {
        context.app.swap('');

        var type = 'Taxonomy';
        var typeID = context.params['typeid'];
        var selectedID = context.params['id'];
        var permissions = new PermissionsModel();
        var survey;

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

            function showLineage() {
                $('#main').hide();
                $('#Panel').fadeIn();
                $('#PanelContent')
                    .html(progressIndicatorHtml)
                    .load('/relations/Lineage?type=Taxonomy&id=' + selectedID);
            }

            function showImpact() {
                $('#main').hide();
                $('#Panel').fadeIn();
                $('#PanelContent')
                    .html(progressIndicatorHtml)
                    .load('/relations/Impact?type=Taxonomy&id=' + selectedID);
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

                        context.contentHeader(pageViewModel);
                        ObjectDetail('DetailTile', type, selectedID);

                        var loadPermissionsDependentTiles = function () {
                            amplify.publish(AmplifyActions.TileUnsubscribe, {});


                            if (json.AllowAttributes) {
                                $('#AttributesTile').Attributes('reload', type, selectedID, permissions.HasPermission("Attributes", "Update"));
                            }
                            else {
                                $('#AttributesTile').hide();
                            }

                            if (json.AllowSynonyms) {
                                $('#SynonymsTile').Synonyms('reload', type, selectedID, permissions.HasPermission("Relationship", "Update"), permissions.HasPermission("Relationship", "Delete"));
                            }
                            else {
                                $('#SynonymsTile').hide();
                            }

                            ObjectStatisticsTile('StatisticsTile', type, selectedID);
                            PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, selectedID, '', false);
                            RelationshipAggregatesTile('AggregatesTileContainer', type, selectedID, permissions);

                            
                            if (survey) survey.ChangeObject(type, selectedID, 'TaxonomyType', typeID);
                            else survey = new Survey('Survey', type, selectedID, 'TaxonomyType', typeID);
                            
                        }

                        permissions.GetPermissionsForObject(type, selectedID).then(loadPermissionsDependentTiles);
                    }
                }
            }

            function unsubscribe(data) {
                survey = null;
                $('#AttributesTile').Attributes('destroy');
                $('#Tree').off('bindingComplete', bindingComplete);
                $('#ShowImpact').off('click', showImpact);
                $('#ShowLineage').off('click', showLineage);
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

                    $('#AttributesTile').Attributes({ object: 'Taxonomy', objectID: typeID, readOnly: false });
                    $('#SynonymsTile').Synonyms({ object: 'Taxonomy', objectID: 0 });
                    $('#ShowImpact').jqxButton({ theme: theme, height: 50 });
                    $('#ShowLineage').jqxButton({ theme: theme, height: 50 });


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
                    $('#ShowImpact').on('click', showImpact);
                    $('#ShowLineage').on('click', showLineage);

                    //#endregion

                    //#region TreeGrid

                    var TreeGridSource =
                    {
                        dataType: 'json',
                        url: '/internal/taxonomy/ModelHierarchy?id=' + typeID,
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

                    var TreeGridAdapter = new $.jqx.dataAdapter(TreeGridSource, {
                        beforeLoadComplete: function (records) {
                            $.each(records, function () {
                                this.expanded = "true";
                            });
                            return records;
                        }
                    });

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