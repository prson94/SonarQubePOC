function artifacts_list(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts/:typeid', function (context) {
        context.app.swap('');

        var typeID = context.params['typeid'];
        var type = 'ArtifactType';
        var id = context.params['id'];
        var filters = [];

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
                //$('#SideIcons').PageTools('reload', type: type, id: id, context: 'list');
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
                            //$('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            $('#List').jqxGrid('updatebounddata');
                            break;
                        case contextList.ArtifactType:
                            //$('#SideIcons').PageTools("reload", data.custom.ObjectType, data.custom.ObjectID, "default");
                            break;
                    }
                } catch (e) { }
            }

            function clearFilter() {
                $.each(filters, function () {
                    switch (this.type) {
                        case 'number':
                            $('#' + this.id).jqxNumberInput('val', '');
                            break;
                        case 'bool':
                            $('#' + this.id).jqxCheckBox('val', false);
                            break;
                        case 'list':
                            $('#' + this.id).jqxDropDownList('clearSelection');
                            break;
                        case 'date':
                            $('#' + this.id).val(null);
                            break;
                        default: //string
                            $('#' + this.id).jqxInput('val', '');
                            break;
                    }
                });
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
                $('#List').jqxGrid('updatebounddata');
            }

            function showFilterAdvanced() {
                var adv = $('#ShowFilterAdvanced');
                if (adv.data('visible')) {
                    adv.html('<i class="fa fa-gear brown-text lighten-4"></i> Show Advanced')
                    //adv.text('Show Advanced');
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
                        //data = createFilterModel(data);
                        $.fileDownload('/artifacts/' + typeID + '.xls', {
                            httpMethod: "POST",
                            data: {}//{ Name: data.Name, Description: data.Description, Statuses: data.Statuses, InformationModels: data.InformationModels, OwnerDomains: data.OwnerDomains }
                        });
                        break;
                }
            }

            function unsubscribe(data) {
                ArtifactListAdapter = null;
                ArtifactListSource = null;

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

                        $.each(gridinfo.Columns, function () {
                            var filterType = 'string';
                            switch (this.columntype) {
                                case 'number':
                                case 'numberinput':
                                    filterType = 'number';
                                    break;
                                case 'checkbox':
                                    filterType = 'bool';
                                    break;
                                case 'combobox':
                                case 'dropdownlist':
                                    filterType = 'list';
                                    break;
                                case 'datetimeinput':
                                    filterType = 'date';
                                    break;
                            }

                            filters.push({ field: this.datafield, type: filterType, id: 'Filter_' + this.datafield, name: this.text, items: this.filteritems });
                        });

                        var filterColClass = 'col ';
                        switch (filters.length) {
                            case 5:
                            case 6:
                            case 7:
                                filterColClass += 's12 m2 l2';
                                break;
                            case 4:
                                filterColClass += 's12 m3 l3';
                                break;
                            default: //3
                                filterColClass += 's12 m4 l4';
                                break;
                        }

                        var filterHtml = '';
                        filterHtml += '<div class="row">';

                        $.each(filters, function () {

                            filterHtml += '<div class="' + filterColClass + '">';
                            filterHtml += '<div class="FieldFilter">' + this.name + '</div>';
                            switch (this.type) {
                                case 'number':
                                    filterHtml += '<div id="' + this.id + '"></div>';
                                    break;
                                case 'bool':
                                    filterHtml += '<div id="' + this.id + '"></div>';
                                    break;
                                case 'list':
                                    filterHtml += '<div id="' + this.id + '"></div>';
                                    break;
                                case 'date':
                                    filterHtml += '<div id="' + this.id + '"></div>';
                                    break;
                                default: //string
                                    filterHtml += '<input id="' + this.id + '" type="text" />';
                                    break;
                            }
                            filterHtml += '</div>';
                        });
                        filterHtml += '</div>';

                        $('#FilterBasic').append(filterHtml);

                        $.each(filters, function () {
                            switch (this.type) {
                                case 'number':
                                    $('#' + this.id).jqxNumberInput({ theme: theme, height: field_height, width: field_width });
                                    $('#' + this.id).keypress(function (e) {
                                        var code = (e.keyCode ? e.keyCode : e.which);
                                        if (code == 13) { //Enter key
                                            runFilter()
                                        }
                                    });
                                    break;
                                case 'bool':
                                    $('#' + this.id).jqxCheckBox({ theme: theme, height: field_height, width: field_width });
                                    break;
                                case 'list':
                                    $('#' + this.id).jqxDropDownList({ theme: theme, height: field_height, width: field_width, source: this.items, placeHolder: 'Choose filter', filterable: (this.items.length > 15), searchMode: 'containsignorecase' });
                                    break;
                                case 'date':
                                    $('#' + this.id).jqxDateTimeInput({ theme: theme, height: field_height, width: field_width, value: null });
                                    $('#' + this.id).keypress(function (e) {
                                        var code = (e.keyCode ? e.keyCode : e.which);
                                        if (code == 13) { //Enter key
                                            runFilter()
                                        }
                                    });
                                    break;
                                default: //string
                                    $('#' + this.id).jqxInput({ theme: theme, height: field_height, width: field_width });
                                    $('#' + this.id).keypress(function (e) {
                                        var code = (e.keyCode ? e.keyCode : e.which);
                                        if (code == 13) { //Enter key
                                            runFilter()
                                        }
                                    });
                                    break;
                            }
                        });

                        //#endregion

                        ArtifactListSource = {
                            datatype: 'json',
                            type: 'post',
                            url: '/artifacts/ByType?id=' + typeID,
                            datafields: gridinfo.Fields,
                            beforeprocessing: function (data) {
                                ArtifactListSource.totalrecords = data.total;
                            },
                            filter: function () {
                                $("#List").jqxGrid('updatebounddata');
                            },
                            sort: function () {
                                $("#List").jqxGrid('updatebounddata');
                            }
                        };

                        ArtifactListAdapter = new $.jqx.dataAdapter(ArtifactListSource, {
                            formatData: function (data) {
                                var i = 0;
                                $.each(filters, function (ix, item) {
                                    // type, field, id, name, items
                                    var filtertype = 'stringfilter';
                                    var filtercondition = 'EQUAL';
                                    var value = '';
                                    switch (item.type) {
                                        case 'number':
                                            filtertype = 'numericfilter';
                                            value = $('#' + item.id).jqxNumberInput('val');
                                            break;
                                        case 'bool':
                                            value = $('#' + item.id).jqxCheckBox('val');
                                            break;
                                        case 'list':
                                            value = $('#' + item.id).jqxDropDownList('val');
                                            break;
                                        case 'date':
                                            filtertype = 'datefilter';
                                            value = $('#' + item.id).jqxDateTimeInput('val');
                                            break;
                                        default: //string
                                            filtercondition = 'CONTAINS';
                                            value = $('#' + item.id).jqxInput('val');
                                            break;
                                    }
                                    if (value != '') {
                                        data.filterscount++;

                                        data['filterdatafield' + i] = item.field;
                                        data['filtercondition' + i] = filtercondition;
                                        data['filtervalue' + i] = value;
                                        i++;
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
                            if (this.text == "Parent") {
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
                                var detailUri = '/artifacts/' + typeID + '/{0}';
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
                                var title = (data.DateLastCertified) ? 'Last certified on ' + moment(data.DateLastCertified).format('MMM Do YYYY') : 'Not yet certified';

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
                            autoshowfiltericon: false,
                            showfilterrow: false,
                            virtualmode: true,
                            rendergridrows: function () {
                                return ArtifactListAdapter.records;
                            },
                            pageable: true,
                            columnsresize: true,
                            source: ArtifactListAdapter,
                            theme: list_theme,
                            columns: gridinfo.Columns
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