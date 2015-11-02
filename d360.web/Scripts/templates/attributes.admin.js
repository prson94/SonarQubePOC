function attributes_admin(app, pageViewModel, templatePath, contextList) {
	app.get('#/attributes/administration', function (context) {
		context.app.swap('');
		context.title(pageViewModel.Title);
		var type = 'AttributeType';

		pageViewModel.breadcrumbs = [];
		pageViewModel.breadcrumbs.push({ Name: 'Administration' });
		pageViewModel.breadcrumbs.push({ Name: 'Type Management' });
		pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

		var permissions = new PermissionsModel();

	    //#region Event Handlers

		function saveAction(data) {
		    try {
		        switch (data.context) {
		            case contextList.AttributeType:
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
		                        } catch (e) {}
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
		        }
		    } catch (e) { }
		}

		function treeSelect(evt) {
		    var args = evt.args;
		    var row = args.row;

		    amplify.publish(AmplifyActions.TileUnsubscribe, {});

		    var o = row.ID;
		    var p = row.ParentID;

		    if (o > 0) {
		        $('#SideIcons').PageTools("reload", type, o);

		        //DetailTile('DetailTile', contextList, permissions, type, o);
		        FieldsGrid("FieldsTile", contextList, permissions, type, o, 'Attribute Definition');

		        if (p) {
		            $('#AllocationsTile').addClass('tile-disabled');
		            $('#AllocationsTile').html('');
		        }
		        else {
		            $('#AllocationsTile').removeClass('tile-disabled');
		            AttributeTypeAllocationGrid('AllocationsTile', contextList, permissions, o);
		            //$('#AllocationsTile').load('/parts/' + type + '/' + o + '/allocations');
		        }
		    }
		    else {
		        $('#SideIcons').PageTools("reload", type, 0);
		        $('#DetailTile').html('');
		        $('#AllocationsTile').html('');
		        $('#FieldsTile').html('');
		    }

		}

		function unsubscribe(data) {
		    $("#Tree").off("rowSelect", treeSelect);
		    amplify.unsubscribe("SaveAction", saveAction);
		    amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
		}

	    //#endregion

		context
			.render(templatePath + 'attributes.admin.html', pageViewModel)
			.appendTo(context.$element())
			.then(function (content) {
			    context.contentHeader(pageViewModel);

				$('#SideIcons').PageTools({ type: type, id: 0 });

				var loadAfterPermissionsRetrieved = function () {
				    var tools = [];
				    if (permissions.HasPermission("Root", "Create")) {
				        tools.push({ icon: 'plus', uri: '/form/AddAttributeType', context: contextList.AttributeType, title: 'Add attribute group' });
				    }
				    TileTools('#TreeTools', tools);

			        //#region TreeGrid

				    var AttributeTreeGridSource =
                    {
                        dataType: 'json',
                        url: '/attributes/types',
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

				    var AttributeTreeGridAdapter = new $.jqx.dataAdapter(AttributeTreeGridSource);

				    $("#Tree").jqxTreeGrid(
                    {
                        width: '100%',
                        height: $(window).innerHeight()-250,
                        source: AttributeTreeGridAdapter,
                        filterable: true,
                        theme: theme,
                        altRows: true,
                        showHeader: false,
                        columns: [
                            { text: 'ID', dataField: 'ID', width: '100px', filterable: false },
                            { text: 'Name', dataField: 'Name' },
                            {
                                text: '', dataField: 'ParentID', width: '120px',
                                cellsRenderer: function (row, column, value, data) {
                                    var tools = [];

                                    if (data.ID >= 50000) {
                                        if (permissions.HasPermission("Root", "Create")) {
                                            tools.push({ icon: 'plus', urlprefix: '/form/AddAttributeType?parentID=' + data.ID, title: 'Add sub-attribute group' });
                                        }
                                        if (permissions.HasPermission("Root", "Update")) {
                                            tools.push({ icon: 'pencil', urlprefix: '/form/EditAttributeType?id=' + data.ID, title: 'Edit attribute group' });
                                        }
                                        if (permissions.HasPermission("Root", "Delete")) {
                                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteAttributeType?id=' + data.ID, title: 'Remove attribute group' });
                                        }
                                    }

                                    return renderToolsHtml(value, tools, contextList.AttributeType, data);
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
				}

				permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);

			    //#region Event Subscriptions

				$(document).on('resize', function () {
				    $('#Tree').jqxTreeGrid({ height: $(window).innerHeight() - 250 });
				});
				$('#Tree').on('rowSelect', treeSelect);
				amplify.subscribe("SaveAction", saveAction);
				amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

			    //#endregion
			});
	});
}