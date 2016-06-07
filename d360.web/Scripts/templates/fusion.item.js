function fusion_item(app, pageViewModel, templatePath, contextList) {

    var fi = function (context) {
        context.app.swap('');

        var type = 'Fusion';
        var typeID = context.params['typeid'];
        var id = context.params['id'];
        var executionID = context.params['executionid'];
        //var tab = context.params['tab'];
        var fusionAttributeID = context.params['fusionattributeid'];
        var fusionAttributeTypeID = context.params['fusionattributetypeid'];

        var FusionAttributeSource;
        var FusionAttributeAdapter;
        var filterVM;

        var permissions = new PermissionsModel();

        var url = '/api/fusion/' + typeID + '/configurations/' + id;

        if (fusionAttributeID != null)
            url = '/api/fusion/' + fusionAttributeID + '/configurations/fromFusionAttribute';

        $.getJSON(url, function (json) {

            if (Array.isArray(json)) {                
                json = json[0];
            }

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;

            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Fusion' });
            pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

            context.title(pageViewModel.Title);

            if (typeID == null || id == null)
            {
                typeID = json.FusionTypeID;
                id = json.ID;
            }

            
            //#region Event Handlers

            function exportFusionAttributes() {
                var selectedFusionAttributeTypeID = 0;

                var selection = $("#FusionAttributeTypes").jqxTreeGrid('getSelection');
                for (var i = 0; i < selection.length; i++) {
                    var rowData = selection[i];
                    selectedFusionAttributeTypeID = rowData.ID;
                }

                var exportUrl = '/fusion/ExportItemsByAttributeType?fusionID=' + id + '&fusionAttributeTypeID=' + selectedFusionAttributeTypeID;

                var filterscount = 0;
                $.each(filterVM.filterData('normal'), function (ix, item) {
                    if (item.value != '' && item.value != null) {
                        exportUrl += '&filterdatafield' + ix + '=' + item.field;
                        exportUrl += '&filtercondition' + ix + '=' + item.condition;
                        exportUrl += '&filtervalue' + ix + '=' + item.value;
                        filterscount++;
                    }
                });
                exportUrl += '&filterscount=' + filterscount;

                location.assign(exportUrl);
            }

            function fusionAttributesTypeBindingComplete(event) {
                try {
                    if (fusionAttributeTypeID > 0) {
                        $('#FusionAttributeTypes').jqxTreeGrid('selectRow', fusionAttributeTypeID);
                    }
                    else {
                        var firstRow = $('#FusionAttributeTypes').jqxTreeGrid('getRows')[0];
                        if (firstRow) {
                            var key = $("#FusionAttributeTypes").jqxTreeGrid('getKey', firstRow);
                            if (key) {
                                $('#FusionAttributeTypes').jqxTreeGrid('selectRow', key);
                            }
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
                //NewFusionItemsGrid('ItemsTile', id, row.ID);

                $.ajax({
                    type: "GET",
                    url: "/api/FusionAttributeType/" + row.ID + "/grid/definition?fusionID=" + id,
                    async: false,
                    contentType: 'application/json',
                    dataType: 'json'
                }).done(function (definition) {

                    definition.Fields.push({ name: 'FusionAttributeTypeID', type: 'number' });

                    //Refresh Filters
                    if (fusionAttributeID > 0) {
                        filterVM.setColumns(definition.FilterColumns, "ID", fusionAttributeID);
                    }
                    else {
                        filterVM.setColumns(definition.FilterColumns);
                    }
                    
                    definition.Columns.forEach(function (item) {

                    try {
                        if (item.filteritems) {
                            if (item.filteritems.length == 0)
                                delete item.filteritems;
                        }
                    } catch (e) {

                    }

                    //modify type column
                    if (item.datafield && item.datafield.toUpperCase() === 'TYPE') item.datafield = '_type';
                });

                    definition.Fields.forEach(function (item) {
                        if (item.name && item.name.toUpperCase() === 'TYPE') item.name = '_type';
                    });

                    //add internal type
                    definition.Fields.push({ name: 'Type', type: 'string' });

                    FusionAttributeSource.datafields = definition.Fields;
                    FusionAttributeSource.url = '/fusion/ItemsByAttributeType?fusionID=' + id + '&fusionAttributeTypeID=' + row.ID;

                    $('#ItemsTile').jqxGrid('destroy');

                    $('#ItemsTileWrapper').html('<div id="ItemsTile"></div>');

                    //$('#ItemsTile').jqxGrid('removesort');

                    $('#ItemsTile').one('bindingcomplete', function (event) {
                        try {
                            if (definition.Columns.length > 7) {
                                $.each(definition.Columns, function () {
                                    if (this.datafield.indexOf('Parent') > -1) {
                                        $('#ItemsTile').jqxGrid('pincolumn', this.datafield);
                                    }
                                });
                                $('#ItemsTile').jqxGrid('pincolumn', 'Name');
                                $('#ItemsTile').jqxGrid('autoresizecolumns');
                            }
                        } catch (e) {
                        }
                    });

                    $('#ItemsTile').jqxGrid({
                        altrows: true,
                        width: grid_width,
                        autoheight: true,
                        sortable: true,
                        filterable: false,
                        showfilterrow: false,
                        showfiltermenuitems: false,
                        showsortmenuitems: false,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        pageable: true,
                        virtualmode: true,
                        rendergridrows: function () {
                            return FusionAttributeAdapter.records;
                        },
                        columnsresize: true,
                        source: FusionAttributeAdapter,
                        theme: theme,
                        columns: definition.Columns
                    });



                    //$('#ItemsTile').jqxGrid({ columns: definition.Columns });
                    //$('#ItemsTile').jqxGrid('updatebounddata');
                });
            }

            function itemsBindingComplete(event) {
                try {
                    var rowCount = $('#ItemsTile').jqxGrid('getdisplayrows').length;
                    if (rowCount > 0) {
                        $('#ItemsTile').jqxGrid('selectrow', 0);
                    }
                } catch (e) {
                    console.log(e);
                }
            }

            function itemsRowSelected(event) {
                var args = event.args;              // event arguments.
                var rowBoundIndex = args.rowindex;  // row's bound index.
                var data = args.row;                // row's data

                $('#AggregatesTile').fadeIn(500);
                RelationshipAggregatesTile('AggregatesTile', 'FusionAttribute', data.ID, permissions);
                //FusionRelationshipChartTile('AggregatesTile', 'FusionAttribute', data.ID);
                AttributesTile('ItemAttributesTile', contextList, permissions, 'FusionAttribute', data.ID, 'Technical Attributes for ' + data.Name)
                FusionAttributeDetailTile('FusionAttributeDetailsTile', 'FusionAttribute', data.ID);
            }

            function fusionAttributeRowSelected(data) {
                $('#AggregatesTile').fadeIn(500);
                RelationshipAggregatesTile('AggregatesTile', 'FusionAttribute', data.ID, permissions);
                //FusionRelationshipChartTile('AggregatesTile', 'FusionAttribute', data.ID);
                AttributesTile('ItemAttributesTile', contextList, permissions, 'FusionAttribute', data.ID, 'Technical Attributes for ' + data.Name)
                FusionAttributeDetailTile('FusionAttributeDetailsTile', 'FusionAttribute', data.ID);
            }

            function clearFilter() {
                filterVM.clearFilters();
                $('#ItemsTile').jqxGrid('updatebounddata');
            }

            function runFilter() {
                $('#ItemsTile').jqxGrid('gotopage', 0); //if user is paging around send them back to begining in case search results change number of pages.
                $('#ItemsTile').jqxGrid('updatebounddata');
            }

            function toolAction(data) {
                switch (data.context) {
                    case contextList.ActionExport:
                        //alert(data.uri);
                        $.fileDownload(data.uri, {
                            httpMethod: "GET"
                        });
                        break;
                }
            }

            function unsubscribe(data) {
                FusionAttributeAdapter = null;
                FusionAttributeSource = null;

                try {
                    ko.cleanNode($('#ItemsTile')[0]);
                } catch (e) {

                }

                $('#Export').off('click', exportFusionAttributes);
                $('#RunFilter').off('click', runFilter);
                $('#ClearFilter').off('click', clearFilter);
                $('#FusionAttributeTypes').off('bindingComplete', fusionAttributesTypeBindingComplete);
                $('#FusionAttributeTypes').off('rowSelect', fusionAttributeTypesRowSelect);
                $('#ItemsTile').off('rowselect', itemsRowSelected);
                $("#ItemsTile").off("bindingcomplete", itemsBindingComplete);
                amplify.unsubscribe('FusionAttributeRowSelected', fusionAttributeRowSelected);
                amplify.unsubscribe("ToolAction", toolAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            }

            //#endregion

            context
                .render(templatePath + 'fusion.item.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    permissions.GetPermissionsForObject(type, id);

                    $('#SideIcons').PageTools({ type: type, id: id });

                    if (fusionAttributeID != null) json.ID = fusionAttributeID;                    
                    //FusionItemsGrid('ItemsTile', contextList, permissions, typeID, id, (fusionAttributeID != null) ? json : null);                   
                    PeopleResponsibilityTile('GovernanceTile', contextList, permissions, type, id, '', false);

                    //#region Fusion Attribute Type

                    FusionAttributeTypesSource = {
                        dataType: "json",
                        dataFields: [
                            { name: 'ID', type: 'number' },
                            { name: 'FusionTypeID', type: 'number' },
                            { name: 'ParentID', type: 'number' },
                            { name: 'Name', type: 'string' }
                        ],
                        hierarchy:
                        {
                            keyDataField: { name: 'ID' },
                            parentDataField: { name: 'ParentID' }
                        },
                        id: 'ID',
                        root: 'value',
                        url: '/services/fusion/' + typeID + '/attributetypes'
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
                            { text: 'Name', dataField: 'Name' }
                        ]
                    });

                    //#endregion

                    //#region Fusion Attribute

                    //Build Filters
                    filterVM = new ArtifactFiltersViewModel([]);
                    filterVM.FilterCallback = runFilter;
                    try {
                        ko.applyBindings(filterVM, document.getElementById('Filters'));
                    }
                    catch (e) {
                        console.log(e);
                    }

                    FusionAttributeSource = {
                        datatype: 'json',
                        type: 'get',
                        url: null,
                        datafields: null,
                        beforeprocessing: function (data) {
                            FusionAttributeSource.totalrecords = data.total;
                        },
                        filter: function () {
                            $('#ItemsTile').jqxGrid('updatebounddata');
                        },
                        sort: function () {
                            $('#ItemsTile').jqxGrid('updatebounddata');
                        }
                    };

                    FusionAttributeAdapter = new $.jqx.dataAdapter(FusionAttributeSource, {
                    formatData: function (data) {
                        data.filterscount = 0;
                        
                        //normal filters
                        $.each(filterVM.filterData('normal'), function (ix, item) {                                    
                            if (item.value != '' && item.value != null) {
                                data['filterdatafield' + data.filterscount] = item.field;
                                data['filtercondition' + data.filterscount] = item.condition;
                                data['filtervalue' + (data.filterscount++)] = item.value;
                            }
                        });

                        return data;
                    }                        
                });

                    $('#ItemsTile').jqxGrid({
                        altrows: true,
                        width: grid_width,
                        autoheight: true,
                        sortable: true,
                        filterable: false,
                        showfilterrow: false,
                        showfiltermenuitems: false,
                        showsortmenuitems: false,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        pageable: true,
                        virtualmode: true,
                        rendergridrows: function () {
                            return FusionAttributeAdapter.records;
                        },
                        columnsresize: true,
                        source: FusionAttributeAdapter,
                        theme: theme,
                        columns: []
                    });

                    //#endregion

                    //#region Events

                    $('#Export').on('click', exportFusionAttributes);
                    $('#RunFilter').on('click', runFilter);
                    $('#ClearFilter').on('click', clearFilter);
                    $('#FusionAttributeTypes').on('bindingComplete', fusionAttributesTypeBindingComplete);
                    $('#FusionAttributeTypes').on('rowSelect', fusionAttributeTypesRowSelect);
                    $('#ItemsTile').on('rowselect', itemsRowSelected);
                    $("#ItemsTile").on("bindingcomplete", itemsBindingComplete);
                    amplify.subscribe('FusionAttributeRowSelected', fusionAttributeRowSelected);
                    amplify.subscribe("ToolAction", toolAction);
                    //amplify.subscribe("PageResized", pageResized);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion

                    if (fusionAttributeID != null) {
                        var item = { ID: fusionAttributeID, Name: json.ItemName };
                        //fusionAttributeRowSelected(item);                        
                    }

                    if (executionID) {
                        amplify.publish("ToolAction", { uri: '/fusion/FusionExecution?id=' + executionID, context: null });
                    }
                });
        });
    };

    app.get('#/fusion/:typeid/:id/executions/:executionid', fi);
    //app.get('#/fusion/:typeid/:id/:tab/:fusionattributeid', fi);
    //app.get('#/fusion/:typeid/:id/:tab', fi);
    app.get('#/fusion/:typeid/:id', fi);
    app.get('#/fusion/item/:fusionattributetypeid/:fusionattributeid', fi);
}