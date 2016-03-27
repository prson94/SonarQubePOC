function HierarchyTile(controlID, contextList, permissions, type, id, mapType) {
    var source = $("#hierarchyTileTmpl").html();
    var template = Handlebars.compile(source);
    var newRowID = null;
    var newRowCounter = -1;
    var editorDropDownInfo = [];
    var mode = '';
    var isAddingParent = false;

    $('#' + controlID).html(template({ control: controlID }));

    controlID = '#' + controlID;
    var controlID_hierarchy = controlID + '_hierarchy';
    //var controlID_title = controlID + '_title';
    //$(controlID_title).text(title);

    //$(controlID).css('padding-bottom', '0px');

    var getAdapter = function (mapType, selector) {
        return {

            dataType: "json",
            dataFields: [
            { name: 'ID', type: 'number' },
            { name: 'UID', type: 'string' },
            { name: 'Subject', type: 'string' },
            { name: 'SubjectID', type: 'number' },
            { name: 'Object', type: 'string' },
            { name: 'ObjectID', type: 'number' },
            { name: 'ObjectType', type: 'string' },
            { name: 'ObjectTypeID', type: 'number' },
            { name: 'ParentID', type: 'string' },
            { name: 'Name', type: 'string' },
            { name: 'Path', type: 'string' },
            { name: 'Url', type: 'string' },
            { name: 'ObjectTypeName', type: 'string' },
            { name: 'Level', type: 'number' },
            { name: 'PredicateID', type: 'number' },
            { name: 'PredicatePhrase', type: 'string' },
            { name: 'Type', type: 'number' },
            { name: 'GroupNumber', type: 'number' }
            ],
            hierarchy:
            {
                keyDataField: { name: 'UID' },
                parentDataField: { name: 'ParentID' }
            },
            id: 'UID',
            url: '/relations/hierarchy/' + mapType + '/' + type + '/' + id,
            addRow: function (rowID, rowData, position, parentID, commit) {
                rowData.parentID = parentID;

                newRowID = rowID;
                commit(true);
            }
        };
    }
    var getTreeGrid = function (mapType, adapter, selector, ctrlID) {
        return {
            width: '100%',
            source: adapter,
            sortable: false,
            showHeader: false,
            showToolbar: true,
            theme: 'metro',
            toolbarHeight: 40,
            renderToolbar: function (toolBar) {

                if (permissions.HasPermission("Relationship", "Update")) {

                    var rowKey = null;
                    var isUpdating = false;

                    var html = $("<div style='overflow: hidden; position: relative; height: 100%; width: 100%; margin-bottom: 4px'></div>");
                    var spinner = $("<span style='display:none' id='" + ctrlID + "_toolbar_spinner'><i class='fa fa-spinner fa-2x fa-spin'></i></span>");
                    var buttonTemplate = "<div style='float: left; padding: 3px; margin: 2px;'><div style='margin: 4px; width: 16px; height: 16px;'><i class='fa {fa-icon}'></i></div></div>";
                    var addButton = $(buttonTemplate.replace("{fa-icon}", "fa-level-down"));
                    var addParentButton = $(buttonTemplate.replace("{fa-icon}", "fa-level-up fa-flip-horizontal"));
                    var saveButton = $(buttonTemplate.replace("{fa-icon}", "fa-save"));
                    var editButton = $(buttonTemplate.replace("{fa-icon}", "fa-pencil"));
                    var deleteButton = $(buttonTemplate.replace("{fa-icon}", "fa-trash"));
                    var cancelButton = $(buttonTemplate.replace("{fa-icon}", "fa-remove"));
                    html.append(addParentButton);
                    html.append(addButton);
                    html.append(saveButton);
                    html.append(editButton);
                    html.append(deleteButton);
                    html.append(cancelButton);
                    html.append(spinner);
                    toolBar.append(html);
                    addButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "add a child", position: 'top', theme: 'metro', opacity: 1 });
                    addParentButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "add a parent", position: 'top', theme: 'metro', opacity: 1 });;
                    saveButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "save changes", position: 'top', theme: 'metro', opacity: 1 });;
                    editButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "edit selected artifact", position: 'top', theme: 'metro', opacity: 1 });;
                    deleteButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "delete selected artifact", position: 'top', theme: 'metro', opacity: 1 });;
                    cancelButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "cancel", position: 'top', theme: 'metro', opacity: 1 });;

                    var setButtonState = function (state) {
                        if (isUpdating)
                            state = 'disable';
                        switch (state) {
                            case 'select':
                            case 'save':
                                addButton.jqxButton({ disabled: false });
                                addParentButton.jqxButton({ disabled: false });
                                saveButton.jqxButton({ disabled: true });
                                editButton.jqxButton({ disabled: false });
                                deleteButton.jqxButton({ disabled: false });
                                cancelButton.jqxButton({ disabled: true });
                                break;
                            case 'unselect':
                                addButton.jqxButton({ disabled: true });
                                addParentButton.jqxButton({ disabled: true });
                                saveButton.jqxButton({ disabled: true });
                                editButton.jqxButton({ disabled: true });
                                deleteButton.jqxButton({ disabled: true });
                                cancelButton.jqxButton({ disabled: true });
                                break;
                            case 'edit':
                                addButton.jqxButton({ disabled: true });
                                addParentButton.jqxButton({ disabled: true });
                                saveButton.jqxButton({ disabled: false });
                                editButton.jqxButton({ disabled: true });
                                deleteButton.jqxButton({ disabled: true });
                                cancelButton.jqxButton({ disabled: false });
                                break;
                            case 'disable':
                                addButton.jqxButton({ disabled: true });
                                addParentButton.jqxButton({ disabled: true });
                                saveButton.jqxButton({ disabled: true });
                                editButton.jqxButton({ disabled: true });
                                deleteButton.jqxButton({ disabled: true });
                                cancelButton.jqxButton({ disabled: true });
                                break;
                        }

                        if (!permissions.HasPermission("Relationship", "Create"))
                            addButton.jqxButton({ disabled: true });
                        if (!permissions.HasPermission("Relationship", "Update"))
                            editButton.jqxButton({ disabled: true });
                        if (!permissions.HasPermission("Relationship", "Delete"))
                            deleteButton.jqxButton({ disabled: true });
                    }

                    setButtonState('unselect');

                    $(selector).on('rowSelect', function (event) {
                        setButtonState('select');

                        if (mode == 'edit') {
                            $(selector).jqxTreeGrid('endRowEdit', rowKey, true);
                        }
                        //console.log(mode);
                        if (mode != 'saving') {
                            mode = '';
                        }
                        rowKey = event.args.key;
                        var row = $(selector).jqxTreeGrid('getRow', rowKey);


                        //console.log(mode);
                        //if (mode != '') {
                        //    console.log(mode + ',' + rowKey);
                        //    $(selector).jqxTreeGrid('endRowEdit', rowKey, true);
                        //    if (mode == 'add')
                        //        $(selector).jqxTreeGrid('deleteRow', rowKey);
                        //}

                        if (row != null)
                            if (row.ParentID == null || row.ParentID == 0)
                                if (mapType == 4)
                                    addParentButton.jqxButton({ disabled: true });
                    });
                    $(selector).on('rowDoubleClick', function (event) {
                        window.location.href = event.args.row.Url;
                    });
                    $(selector).on('bindingComplete', function (event) {
                        showFocalRow($(selector), event);
                    });
                    $(selector).on('rowUnselect', function () { setButtonState('unselect') });
                    $(selector).on('rowEndEdit', function (event) {
                        setButtonState('save');
                        if (mode == 'add') {
                            $(selector).jqxTreeGrid('deleteRow', rowKey);

                        }

                        //console.log(mode);
                        //console.log(event);
                    });
                    $(selector).on('rowBeginEdit', function () { setButtonState('edit') });

                    addButton.click(function (event) {
                        if ($(selector).jqxTreeGrid('getSelection').length < 1)
                            return;
                        if (addButton.jqxButton('disabled'))
                            return;
                        if (mode != '')
                            return;

                        //console.log(mode);
                        isAddingParent = false;
                        var row = $(selector).jqxTreeGrid('getRow', rowKey);
                        //console.log(rowKey);
                        $(selector).jqxTreeGrid('expandRow', rowKey);
                        $(selector).jqxTreeGrid('addRow', newRowCounter--, { ID: row.ID, Level: row.Level }, 'first', rowKey);
                        $(selector).jqxTreeGrid('clearSelection');
                        $(selector).jqxTreeGrid('selectRow', newRowID);
                        $(selector).jqxTreeGrid('beginRowEdit', newRowID);
                        mode = 'add';
                    });
                    addParentButton.click(function (event) {
                        if (addParentButton.jqxButton('disabled'))
                            return;
                        if (mode != '')
                            return;
                        if ($(selector).jqxTreeGrid('getSelection').length < 1)
                            return;

                        var intersectMapID = -1;

                        var row = $(selector).jqxTreeGrid('getRow', rowKey);
                        var parent = null;
                        if (row != null)
                            parent = $(selector).jqxTreeGrid('getRow', row.ParentID);
                        if (parent != null)
                            rowKey = parent.UID;
                        else {
                            intersectMapId: row.ID;
                        }
                        //console.log($(selector).jqxTreeGrid('getRow', rowKey));

                        isAddingParent = true;
                        $(selector).jqxTreeGrid('expandRow', rowKey);
                        $(selector).jqxTreeGrid('addRow', newRowCounter--, { ID: row.ID, Level: row.Level }, 'first', rowKey);
                        $(selector).jqxTreeGrid('clearSelection');
                        $(selector).jqxTreeGrid('selectRow', newRowID);
                        $(selector).jqxTreeGrid('beginRowEdit', newRowID);
                        mode = 'add';
                    });
                    saveButton.click(function (event) {
                        var oldMode = mode;
                        if (saveButton.jqxButton('disabled'))
                            return;
                        mode = 'saving';
                        $(selector).jqxTreeGrid('endRowEdit', rowKey, false);
                        var rowData = null;
                        var parentData = null;
                        rowData = $(selector).jqxTreeGrid('getRow', rowKey);

                        if (rowData != null)
                            parentData = getRowDataItem(rowData.parent);


                        var sub = null;
                        var subid = null;
                        var obj = null;
                        var objid = null;
                        var intersectMapId = null;

                        //if (isAddingParent) {
                        //    intersectMapId = rowData.ID;
                        //} else {

                        intersectMapId = (rowData.ID || 0);
                        //console.log(rowData.ID);
                        //}
                        //console.log(rowData);
                        //console.log(parentData);
                        //if (isAddingParent) {
                        sub = parentData.type;
                        subid = parentData.id;
                        obj = rowData.Object;
                        objid = rowData.ObjectID;
                        // intersectMapId = rowData.ID;
                        //} else {
                        //    sub = parentData.type;
                        //    subid = parentData.id;
                        //    obj = rowData.Object;
                        //    objid = rowData.ObjectID;
                        //    //intersectMapId = (rowData.parent.ID || 0);
                        //}

                        if (rowData != null) {

                            var hierarchyPostModel = {
                                Subject: sub,
                                SubjectID: subid,
                                'Object': obj,
                                'ObjectID': objid,
                                //PredicateID: rowData.PredicateID,
                                IsAddingParent: isAddingParent,
                                IntersectMapID: intersectMapId,
                                HierarchyType: mapType,
                                GroupNumber: rowData.parent.GroupNumber
                            };
                            //console.log(hierarchyPostModel);
                            var url = '/relations/hierarchy/save';
                            if (oldMode == 'edit') {
                                url = '/relations/hierarchy/edit';
                                hierarchyPostModel.IntersectMapId = rowData.ID;

                            }

                            //mode = 'saving';

                            isUpdating = true;
                            setButtonState('disable');

                            $('#' + ctrlID + '_toolbar_spinner').show();
                            $.ajax({
                                url: url,
                                data: hierarchyPostModel,
                                method: 'POST'
                            }).success(function (data, status, xhr) {
                                amplify.publish("ShowMessage", data);
                            }).fail(function (data, status, xhr) {
                                amplify.publish("ShowMessage", data);
                            }).always(function () {
                                $(selector).jqxTreeGrid('updateBoundData');
                                $('#' + ctrlID + '_toolbar_spinner').hide();
                                isUpdating = false;
                                setButtonState('unselect');
                                mode = '';
                            });
                            $(selector).jqxTreeGrid('updateBoundData');
                        }
                    });
                    cancelButton.click(function (event) {
                        if (cancelButton.jqxButton('disabled'))
                            return;
                        $(selector).jqxTreeGrid('endRowEdit', rowKey, true);
                        if (mode == 'add')
                            $(selector).jqxTreeGrid('deleteRow', rowKey);
                    });
                    deleteButton.click(function (event) {
                        var oldMode = mode;
                        if (deleteButton.jqxButton('disabled'))
                            return;
                        if (oldMode != '')
                            return;
                        var selection = $(selector).jqxTreeGrid('getSelection');
                        if (selection == null || selection == [] || selection[0] == null)
                            return;
                        if (selection[0].ID == null || selection[0].ID < 1)
                            return;

                        isUpdating = true;
                        setButtonState('disable');
                        $('#' + ctrlID + '_toolbar_spinner').show();
                        $.ajax({
                            url: '/relations/hierarchy/delete/' + selection[0].ID,
                            method: 'DELETE',
                            success: function (d) {
                                $(selector).jqxTreeGrid('updateBoundData');
                                $('#' + ctrlID + '_toolbar_spinner').hide();
                                isUpdating = false;
                                setButtonState('unselect');
                                mode = '';
                            },
                            failure: function (d) {
                                $(selector).jqxTreeGrid('updateBoundData');
                                $('#' + ctrlID + '_toolbar_spinner').hide();
                                isUpdating = false;
                                setButtonState('unselect');
                                mode = '';
                            }
                        });

                    });
                    editButton.click(function (event) {
                        if (editButton.jqxButton('disabled'))
                            return;
                        if (mode != '')
                            return;
                        mode = 'edit';
                        $(selector).jqxTreeGrid('beginRowEdit', rowKey);
                    });

                } //Permissions check
            },
            columns: [
                {
                    text: 'Artifact', dataField: 'Name', align: "center", columnType: "custom", //width: '80%'
                    cellsRenderer: function (rowKey, dataField, value, data) {
                        var item = getRowDataItem(data);
                        if (item.type == type && item.id == id) {
                            return "<div style='margin-left: 4px; display:inline-block;color:#33A'><div style='font-weight:600;'>" + value + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"
                        }
                        return "<div style='margin-left: 4px; display:inline-block;'><div style='font-weight:600;'>" + value + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"
                    },
                    createEditor: function (row, cellvalue, editor, cellText, width, height) {
                        if (mode == 'edit') {
                            var data = $(selector).jqxTreeGrid('getRow', row);
                            var item = getRowDataItem(data);
                            if (item.type == type && item.id == id) {
                                editor.append($("<div style='margin-left: 4px; display:inline-block;color:#33A'><div style='font-weight:600;'>" + data.Name + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"));
                            } else {
                                editor.append($("<div style='margin-left: 4px; display:inline-block;'><div style='font-weight:600;'>" + data.Name + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"));
                            }

                            return;
                        }

                        var rowData = $(selector).jqxTreeGrid('getRow', row);

                        var mapId = 0;
                        var groupNumber = 0;

                        if (rowData != null) {
                            if (rowData.parent != null)
                                mapId = rowData.parent.ID;
                            if (rowData.parent != null)
                                groupNumber = rowData.parent.GroupNumber;
                        }


                        var hierarchyArtifactsModel = {
                            IntersectMapID: mapId,
                            MapType: mapType,
                            Type: type,
                            ID: id,
                            GroupNumber: groupNumber,
                            IsAddingParent: isAddingParent
                        }
                        var hierarchySubjectSource = {
                            datafields: [
                                { name: 'Object' },
                                { name: 'ObjectID' },
                                { name: 'Name' },
                                { name: 'ObjectTypeName' },
                                { name: 'DisplayName' }
                            ],
                            datatype: "json",
                            url: '/relations/hierarchy/artifacts/',
                            data: hierarchyArtifactsModel
                        };


                        var hierarchySubjectAdapter = new $.jqx.dataAdapter(
                            hierarchySubjectSource,
                            {
                                beforeLoadComplete: function (records) {
                                    for (var i = 0; i < records.length; i++) {
                                        var record = records[i];
                                        record.Value = record.Object + '|' + record.ObjectID;
                                    }
                                    return records;
                                }
                            }
                        );

                        editor.jqxDropDownList({
                            theme: theme,
                            source: hierarchySubjectAdapter,
                            width: field_width,
                            height: field_height,
                            valueMember: 'Value',
                            displayMember: 'DisplayName',
                            filterable: true,
                            dropDownWidth: 350,
                            searchMode: 'containsignorecase'
                        });


                    },
                    getEditorValue: function (row, cellvalue, editor) {
                        var rowData = $(selector).jqxTreeGrid('getRow', row);
                        var selectedItem = editor.jqxDropDownList('getSelectedItem');
                        if (selectedItem == null)
                            return "";
                        if (selectedItem.originalItem == null)
                            return "";
                        var originalItem = selectedItem.originalItem;

                        rowData.ObjectTypeName = originalItem.ObjectTypeName;
                        rowData.Object = originalItem.Object;
                        rowData.ObjectID = originalItem.ObjectID;
                        return originalItem.Name;
                    }
                }//,
                //{
                //    text: "Predicate", dataField: 'PredicatePhrase', width: '20%', align: "center", columnType: "custom",
                //    createEditor: function (row, cellvalue, editor, cellText, width, height) {

                //        //console.log(isAddingParent);
                //        var rowData = $(selector).jqxTreeGrid('getRow', row);
                //        var intersectMapId = 0;
                //        //console.log(rowData);
                //        var parentRow = rowData.parent;
                //        if (parentRow != null)
                //            intersectMapId = parent.ID;


                //        var url = '/diagrams/GetPredicateInfo';

                //        if (intersectMapId > 0)
                //            url += 'ByAllocation?id=' + intersectMapId;
                //        else {
                //            rowData = $(selector).jqxTreeGrid('getRows')[0];
                //            url += 'ByTypes?type1=' + rowData.ObjectType + '&type2=' + rowData.ObjectType + '&id1=' + rowData.ObjectTypeID + '&id2=' + rowData.ObjectTypeID + '&mapType=' + mapType;
                //        }

                //        var hierarchyPredicateSource = {
                //            datafields: [
                //                { name: 'id' },
                //                { name: 'name' }
                //            ],
                //            datatype: "json",
                //            url: url
                //        }

                //        var hierarchyPredicateAdapter = new $.jqx.dataAdapter(hierarchyPredicateSource);

                //        editor.jqxDropDownList({
                //            theme: theme,
                //            source: hierarchyPredicateAdapter,
                //            width: field_width,
                //            height: field_height,
                //            valueMember: 'id',
                //            displayMember: 'name',
                //            filterable: true,
                //            dropDownWidth: 100,
                //            searchMode: 'containsignorecase'
                //        });
                //    },
                //    getEditorValue: function (row, cellvalue, editor) {
                //        var rowData = $(selector).jqxTreeGrid('getRow', row);
                //        var selectedItem = editor.jqxDropDownList('getSelectedItem');
                //        if (selectedItem == null)
                //            return "";
                //        if (selectedItem.originalItem == null)
                //            return "";
                //        var originalItem = selectedItem.originalItem;

                //        rowData.PredicateID = originalItem.id;
                //        return selectedItem.name;
                //    }
                //}
            ]
        }
    }

    function initTreeGrid(selector, mapType, ctrlID) {
        $(selector).jqxTreeGrid(getTreeGrid(mapType, new $.jqx.dataAdapter(getAdapter(mapType)), selector, ctrlID));
    }

    function getRowDataItem(data) {

        if (data.Level > 0) {
            return {
                type: data.Object,
                id: data.ObjectID
            }
        } else {
            return {
                type: data.Subject,
                id: data.SubjectID
            }
        }
    }

    var c = controlID.substring(1);
    initTreeGrid(controlID_hierarchy, mapType, c);

    function showFocalRow(treeGrid, event) {
        if (event == null || event.args == null)
            return;
        var data = event.args.owner.source.loadedData;
        var focal = null;

        for (var i = 0; i < data.length; i++) {
            $(treeGrid).jqxTreeGrid('expandRow', data[i].UID);
            var item = getRowDataItem(data[i]);
            if (item.id == id && item.type == type) {
                focal = data[i];
                break;
            }
        }

        if (focal == null)
            return;

        $(treeGrid).jqxTreeGrid('ensureRowVisible', focal.UID);
    }
}