function artifacts_list(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/:typeid', function (context) {
        context.app.swap('');

        var typeID = context.params['typeid'];
        var type = 'ArtifactType';
        var id = context.params['id'];       
        var filterVM;        

        $.getJSON('/api/artifacts/' + typeID, function (json) {

            pageViewModel.Title = json.Name;
            pageViewModel.Directions = json.Description;
            pageViewModel.breadcrumbs = [];
            pageViewModel.breadcrumbs.push({ Name: 'Business Glossary' });
            pageViewModel.breadcrumbs.push({ Name: json.Name, Active: true });
            var permissions = new PermissionsModel();

            context.title(pageViewModel.Title);
            
            var ArtifactListSource;
            var ArtifactListAdapter;

            //#region Event Handlers

            function refreshActionMenu(data) {                
                $('#SideIcons').PageTools({ type: 'ArtifactType', id: typeID, context: 'list' });
            }

            function artifactListPageResized() {
                $("#List").jqxGrid('refresh');
            }

            function listRowDoubleClick(event) {
                var args = event.args;
                var row = args.rowindex;
                var data = $("#List").jqxGrid('getrowdata', row);
                location.assign(data.Url);
            }

            function saveAction(data) {
                try {
                    switch (data.context) {
                        case contextList.Artifact:                            
                            $('#List').jqxGrid('updatebounddata');
                            break;
                        case contextList.ArtifactType:                            
                            break;
                    }
                } catch (e) { }
            }

            function clearFilter() {
                filterVM.clearFilters();                
                try {
                    $('#RelationshipTypeFilter').jqxDropDownList('clearSelection');
                    var disabled = $('#RelationshipFilter').jqxDropDownList('disabled');
                    if (!disabled) {
                        $('#RelationshipFilter').jqxDropDownList('uncheckAll');
                    }
                } catch (e) {}

                try {
                    $('#AttributeTypeFilter').jqxDropDownList('clearSelection');
                    $('#AttributeFilter').val('');
                    var disabled = $('#AttributeFilter').jqxInput({disabled: true});
                } catch (e) { }

                $('#List').jqxGrid('updatebounddata');
            }

            function runFilter() {                
                $("#List").jqxGrid('gotopage', 0); //if user is paging around send them back to begining in case search results change number of pages.
                $('#List').jqxGrid('updatebounddata');
            }

            function showFilterAdvanced() {
                var adv = $('#ShowFilterAdvanced');
                if (adv.data('visible')) {
                    adv.html('<i class="fa fa-gear brown-text lighten-4"></i> Show Advanced')                    
                    adv.removeData('visible');
                    $('#FilterAdvanced').fadeOut(200);
                }
                else {
                    adv.html('<i class="fa fa-gear brown-text lighten-4"></i> Hide Advanced')
                    adv.data('visible', true);
                    $('#FilterAdvanced').fadeIn(200);
                }
            }

            function toolAction(data) {
                switch (data.context) {
                    case contextList.ActionExport:
                        var data = [];                        
                        $.fileDownload('/internal/artifacts/' + typeID + '.xls', {
                            httpMethod: "POST",
                            data: {}//{ Name: data.Name, Description: data.Description, Statuses: data.Statuses, InformationModels: data.InformationModels, OwnerDomains: data.OwnerDomains }
                        });
                        break;
                }
            }

            function unsubscribe(data) {
                ArtifactListAdapter = null;
                ArtifactListSource = null;
                filterVM = null;

                amplify.unsubscribe("PageResized", artifactListPageResized);
                $('#List').off('rowdoubleclick', listRowDoubleClick);
                $('#RunFilter').off('click', runFilter);
                $('#ClearFilter').off('click', clearFilter);
                amplify.unsubscribe("SaveAction", saveAction);
                $('#ShowFilterAdvanced').off('click', showFilterAdvanced);
                amplify.unsubscribe("ToolAction", toolAction);
                amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
                amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
            }

            //#endregion

            context
                .render(templatePath + 'artifacts.list.html', pageViewModel)
                .appendTo(context.$element())
                .then(function (content) {
                    context.contentHeader(pageViewModel);

                    permissions.GetPermissionsForObject('ArtifactType', typeID);

                    $('#SideIcons').PageTools({ type: 'ArtifactType', id: typeID, context: 'list' });
                    
                    //#region Grid

                    $.getJSON('/api/ArtifactType/' + typeID + '/grid/definition', function (gridinfo) {

                        //#region Build Filters
                                          
                        filterVM = new ArtifactFiltersViewModel(gridinfo.FilterColumns);
                        filterVM.FilterCallback = runFilter;
                        try {                        
                            ko.applyBindings(filterVM, document.getElementById('Filters'));
                        }
                        catch (e) {
                            console.log(e);
                        }
                        
                        //#endregion

                        ArtifactListSource = {
                            datatype: 'json',
                            type: 'post',
                            url: '/internal/artifacts/ByType?id=' + typeID,
                            datafields: gridinfo.Fields,
                            beforeprocessing: function (data) {
                                ArtifactListSource.totalrecords = data.total;
                            },
                            filter: function () {
                                $("#List").jqxGrid('updatebounddata');
                            },
                            sort: function () {                                
                                $("#List").jqxGrid('gotopage', 0); //if user is paging around send them back to begining
                                $("#List").jqxGrid('updatebounddata');
                            }
                        };

                        ArtifactListAdapter = new $.jqx.dataAdapter(ArtifactListSource, {
                            formatData: function (data) {
                                data.filterscount = 0;
                                data.relfilterscount = 0;
                                data.hidfilterscount = 0;
                                //normal filters
                                $.each(filterVM.filterData('normal'), function (ix, item) {                                    
                                    if (item.value != '' && item.value != null) {                                        
                                        data['filterdatafield' + data.filterscount] = item.field;
                                        data['filtercondition' + data.filterscount] = item.condition;
                                        data['filtervalue' + (data.filterscount++)] = item.value;                                                                                
                                    }
                                });

                                //relation filters
                                $.each(filterVM.filterData('relation'), function (ix, item) {
                                    if (item.value != '' && item.value != null) {
                                        data['relfilterdatafield' + data.relfilterscount] = item.field;
                                        data['relfiltercondition' + data.relfilterscount] = item.condition;
                                        data['relfiltervalue' + (data.relfilterscount++)] = item.value;
                                    }
                                });

                                //hidden field filters
                                $.each(filterVM.filterData('hidden'), function (ix, item) {
                                    if (item.value != '' && item.value != null) {
                                        data['hidfilterdatafield' + data.hidfilterscount] = item.field;
                                        data['hidfiltercondition' + data.hidfilterscount] = item.condition;
                                        data['hidfiltervalue' + (data.hidfilterscount++)] = item.value;
                                    }
                                });

                                //#region Relationship filter logic

                                data.RelationshipIncludeType = "";
                                data.RelationshipObjectType = "";
                                data.RelationshipObjectIDs = "";

                                var disabled = $('#RelationshipFilter').jqxDropDownList('disabled');
                                if (!disabled) {
                                    var checkedItems = $('#RelationshipFilter').jqxDropDownList('getCheckedItems');
                                    var relationshipObjectType = "";
                                    var relationshipObjectIDs = [];
                                    $.each(checkedItems, function () {
                                        var checkedItemValue = this.value.split('|');
                                        relationshipObjectType = checkedItemValue[0];
                                        relationshipObjectIDs.push(checkedItemValue[1]);
                                    });

                                    if (relationshipObjectIDs.length > 0) {
                                        data.RelationshipObjectIDs = relationshipObjectIDs.join(',');
                                        data.RelationshipObjectType = relationshipObjectType;
                                        switch ($('#FilterInclusion').jqxButtonGroup('getSelection')) {
                                            case 1:
                                                data.RelationshipIncludeType = 'All';
                                                break;
                                            default:
                                                data.RelationshipIncludeType = 'Any';
                                                break;
                                        }
                                        
                                    }
                                }

                                //#endregion

                                //#region Attribute filter logic

                                data.AttributeType = "";
                                data.AttributeSearchValue = "";
                                var disabled = $('#AttributeTypeFilter').jqxDropDownList('disabled');
                                if (!disabled) {
                                    data.AttributeType = $('#AttributeTypeFilter').val();
                                    data.AttributeSearchValue = $('#AttributeFilter').val();
                                }

                                //#endregion

                                return data;
                            }                        
                        });

                        $.each(gridinfo.Columns, function () {
                            if (this.text === "Parent") {
                                this.cellsrenderer = function (index, datafield, value, defaultvalue, column, data) {
                                    return previewLinkRenderer('Artifact', data.ParentID, data.ParentUrl, data.Parent);
                                }
                            }
                        });

                        gridinfo.Columns.push({
                            datafield: "ID",
                            text: "",
                            sortable: false,
                            filterable: false,
                            width: '150px',
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                var detailUri = '/internal/artifacts/' + typeID + '/{0}';
                                var tools = [];

                                var foreColor = '#fff';
                                switch (data.Status) {
                                    case 'Certified':
                                        foreColor = '#3f9d40';
                                        break;
                                    case 'Under Review':
                                        foreColor = '#e2792a';
                                        break;
                                    default:
                                        foreColor = '#ebebeb';
                                        break;
                                }
                                var title = '';
                                if (data.DateLastCertified)
                                    title = 'Last certified on ' + moment(data.DateLastCertified).format('MMM Do YYYY');
                                else if(data.Status == 'Certified')
                                    title = 'Manually certified';
                                else
                                    title = 'Not yet certified';

                                

                                tools.push({ isitemlink: true, urlprefix: '#' + detailUri, type: 'Artifact', context: 'Certificate', iconBackColor: 'transparent', iconForeColor: foreColor, iconText: 'certificate', title: title });
                                tools.push({ isitemlink: true, urlprefix: '#' + detailUri, type: 'Artifact', context: 'Preview' });
                                tools.push({ icon: 'pencil', urlprefix: 'form' + detailUri + '/edit' });
                                tools.push({ icon: 'trash-o', urlprefix: 'form' + detailUri + '/delete' });

                                return renderToolsHtml(value, tools, contextList.Artifact, data);
                            }
                        });

                        $("#List").jqxGrid({
                            width: grid_width,
                            pagesizeoptions: ['5', '10', '20', '50'],
                            pagesize: 20,
                            autoheight: true,
                            sortable: true,
                            altrows: true,
                            filterable: false,                         
                            showfilterrow: false,
                            virtualmode: true,
                            rendergridrows: function () {
                                return ArtifactListAdapter.records;
                            },                            
                            pageable: true,
                            columnsresize: true,
                            source: ArtifactListAdapter,
                            theme: list_theme,
                            columns: gridinfo.Columns,
                            columngroups: (gridinfo.ColumnGroups.length > 0) ? gridinfo.ColumnGroups : null
                        });

                    });

                    $('#RelationshipTypeFilter').jqxDropDownList({ theme: theme, width: field_width, height: field_height });
                    $('#RelationshipFilter').jqxDropDownList({
                        theme: theme, width: field_width, height: field_height, checkboxes: true, disabled: true,
                        selectionRenderer: function (htmlString) {
                            try {
                                var length = $('#RelationshipFilter').jqxDropDownList('getCheckedItems').length;
                                if (length) {
                                    var ending = (length > 1) ? 's' : '';
                                    return length + ' item' + ending + ' selected';
                                }
                                else
                                    return htmlString;
                            } catch (e) {
                                return htmlString;
                            }
                        }
                    });
                    $("#FilterInclusion").jqxButtonGroup({ theme: theme, mode: 'radio', enableHover: true });
                    $('#FilterInclusion').jqxButtonGroup('setSelection', 0);

                    $('#AttributeTypeFilter').jqxDropDownList({ theme: theme, width: field_width, height: field_height });
                    $('#AttributeFilter').jqxComboBox({theme: theme, width: field_width, height: field_height, disabled: true});

                    $.getJSON('/api/ArtifactType/' + typeID + '/relationshiptypes', function (relateData) {
                        $('#RelationshipTypeFilter').jqxDropDownList({ disabled: (relateData.length == 0) });

                        if (relateData.length > 0) {
                            $.each(relateData, function () {
                                $('#RelationshipTypeFilter').jqxDropDownList('addItem', { value: this.TargetType + '|' + this.TargetTypeID, label: this.TargetName });
                            });

                            $('#RelationshipTypeFilter').on('change', function (event) {
                                var item = event.args.item;
                                var values = item.value.split('|');

                                $('#RelationshipTypeFilter').jqxDropDownList({ disabled: true });

                                $.getJSON('/api/RelationshipObjectsByType?type=' + values[0] + '&id=' + values[1], function (innerRelateData) {

                                    try {
                                        $('#RelationshipFilter').jqxDropDownList('clear');
                                    } catch (e) { }

                                    if (innerRelateData.length > 0) {
                                        $('#RelationshipFilter').jqxDropDownList({ disabled: false }); //filterable: true, searchMode: 'containsignorecase', 
                                        $.each(innerRelateData, function () {
                                            $('#RelationshipFilter').jqxDropDownList('addItem', { value: this.Type + '|' + this.ID, label: this.Name });
                                        });
                                    }
                                    else {
                                        $('#RelationshipFilter').jqxDropDownList({ disabled: true }); //filterable: false, searchMode: 'containsignorecase', 
                                    }

                                    $('#RelationshipTypeFilter').jqxDropDownList({ disabled: false });

                                });
                            });
                        }
                    });

                    $.getJSON('/api/ArtifactType/' + typeID + '/attributetypefilters', function (relateData) {
                        $('#AttributeTypeFilter').jqxDropDownList({ disabled: (relateData.length == 0) });

                        if (relateData.length > 0) {
                            $.each(relateData, function () {
                                $('#AttributeTypeFilter').jqxDropDownList('addItem', { value: this.ID, label: this.Name });
                            });

                            $('#AttributeTypeFilter').on('change', function (event) {
                                var item = event.args.item;
                                var value = item.value;

                                $('#AttributeFilter').val('');

                                $('#AttributeTypeFilter').jqxDropDownList({ disabled: true });
                                var attributeValueSource = {
                                    datatype: 'json',
                                    datafields: [
                                        { name: 'Name' }
                                    ],
                                    url: '/api/ArtifactType/' + typeID + '/' + value + '/attributefiltervalues'
                                };
                                var attributeValueAdapter = new $.jqx.dataAdapter(attributeValueSource, {
                                    formatData: function (data) {
                                        //if ($("#AttributeFilter").jqxComboBox('searchString') != undefined) {
                                        //    data.name_startsWith = $("#AttributeFilter").jqxComboBox('searchString');
                                            return data;
                                        //}
                                    }
                                });
                                $('#AttributeFilter').jqxComboBox({
                                    disabled: false, displayMember: 'Name', valueMember: 'Name', source: attributeValueAdapter, search: function (searchString) {
                                        attributeValueAdapter.dataBind();
                                    }
                                });
                                //$('#AttributeFilter').jqxInput({ disabled: false, displayMember: 'Name', valueMember: 'Name', source: attributeValueAdapter });
                                
                                $('#AttributeTypeFilter').jqxDropDownList({ disabled: false });
                            });
                        }
                    });

                    //#endregion

                    //#region Event Subscriptions

                    amplify.subscribe("PageResized", artifactListPageResized);
                    $('#List').on('rowdoubleclick', listRowDoubleClick);
                    $('#RunFilter').on('click', runFilter);
                    $('#ClearFilter').on('click', clearFilter);
                    amplify.subscribe("SaveAction", saveAction);
                    $('#ShowFilterAdvanced').on('click', showFilterAdvanced);
                    amplify.subscribe("ToolAction", toolAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                    amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                    //#endregion
                });
        });
    });
}