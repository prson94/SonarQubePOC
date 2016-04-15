function CollapsibleSynonymsTile(controlID, contextList, permissions, type, id) {
    //var newRowID = null;
    //var newRowCounter = -1;
    //var editorDropDownInfo = [];
    //var mode = '';

    //var controlID_count = controlID + '_count';
    //var controlID_hierarchy = controlID + '_hierarchy';

    //$('#' + controlID).css('margin', '10px');
    //$('#' + controlID).html('<div>Synonyms<span id="' + controlID_count + '"></span></div><div><div id="' + controlID_hierarchy + '"></div></div>');
    //$('#' + controlID).jqxExpander({ theme: theme, expanded: false });


    //controlID = '#' + controlID;
    //controlID_count = '#' + controlID_count;
    //controlID_hierarchy = '#' + controlID_hierarchy;


    ////#region Event Handlers

    ////function expanded() {
    ////    $('#' + controlID_sub).jqxTreeGrid('updateBoundData');
    ////}

    ////function unsubscribe(data) {
    ////    $('#' + controlID_sub).off('bindingcomplete', bindingComplete);
    ////    amplify.unsubscribe("SaveAction", saveAction);
    ////    amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    ////    amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    ////    $('#' + controlID).off('expanded', expanded);
    ////}

    ////#endregion

    //var getAdapter = function (selector) {
    //    return {
    //        dataType: "json",
    //        dataFields: [
    //        { name: 'IntersectID', type: 'number' },
    //        { name: 'Object', type: 'string' },
    //        { name: 'ObjectID', type: 'number' },
    //        { name: 'ObjectType', type: 'string' },
    //        { name: 'ObjectTypeID', type: 'number' },
    //        { name: 'Name', type: 'string' },
    //        { name: 'Path', type: 'string' },
    //        { name: 'Url', type: 'string' },
    //        { name: 'ObjectTypeName', type: 'string' }
    //        ],
    //        id: 'IntersectID',
    //        url: '/relations/' + type + '/' + id + '/synonyms',
    //        addRow: function (rowID, rowData, position, parentID, commit) {
    //            newRowID = rowID;
    //            commit(true);
    //        }
    //    };
    //};

    //var getTreeGrid = function (adapter, selector, ctrlID) {
    //    return {
    //        width: '100%',
    //        source: adapter,
    //        sortable: false,
    //        showHeader: false,
    //        showToolbar: true,
    //        theme: 'metro',
    //        toolbarHeight: 40,
    //        renderToolbar: function (toolBar) {

    //            if (permissions.HasPermission("Relationship", "Update")) {

    //                var rowKey = null;
    //                var isUpdating = false;

    //                var html = $("<div style='overflow: hidden; position: relative; height: 100%; width: 100%; margin-bottom: 4px'></div>");
    //                var spinner = $("<span style='display:none' id='" + ctrlID + "_toolbar_spinner'><i class='fa fa-spinner fa-2x fa-spin'></i></span>");
    //                var buttonTemplate = "<div style='float: left; padding: 3px; margin: 2px;'><div style='margin: 4px; width: 16px; height: 16px;'><i class='fa {fa-icon}'></i></div></div>";
    //                var addButton = $(buttonTemplate.replace("{fa-icon}", "fa-plus"));
    //                var saveButton = $(buttonTemplate.replace("{fa-icon}", "fa-save"));
    //                var editButton = $(buttonTemplate.replace("{fa-icon}", "fa-pencil"));
    //                var deleteButton = $(buttonTemplate.replace("{fa-icon}", "fa-trash"));
    //                var cancelButton = $(buttonTemplate.replace("{fa-icon}", "fa-remove"));
    //                html.append(addButton);
    //                html.append(saveButton);
    //                html.append(editButton);
    //                html.append(deleteButton);
    //                html.append(cancelButton);
    //                html.append(spinner);
    //                toolBar.append(html);
    //                addButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "add a synonym", position: 'top', theme: 'metro', opacity: 1 });
    //                saveButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "save changes", position: 'top', theme: 'metro', opacity: 1 });;
    //                editButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "edit selected artifact", position: 'top', theme: 'metro', opacity: 1 });;
    //                deleteButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "delete selected artifact", position: 'top', theme: 'metro', opacity: 1 });;
    //                cancelButton.jqxButton({ cursor: "pointer", height: 25, width: 25, theme: 'metro' }).jqxTooltip({ content: "cancel", position: 'top', theme: 'metro', opacity: 1 });;

    //                var setButtonState = function (state) {
    //                    if (isUpdating)
    //                        state = 'disable';
    //                    switch (state) {
    //                        case 'select':
    //                        case 'save':
    //                            //addButton.jqxButton({ disabled: false });
    //                            saveButton.jqxButton({ disabled: true });
    //                            editButton.jqxButton({ disabled: false });
    //                            deleteButton.jqxButton({ disabled: false });
    //                            cancelButton.jqxButton({ disabled: true });
    //                            break;
    //                        case 'unselect':
    //                            //addButton.jqxButton({ disabled: true });
    //                            saveButton.jqxButton({ disabled: true });
    //                            editButton.jqxButton({ disabled: true });
    //                            deleteButton.jqxButton({ disabled: true });
    //                            cancelButton.jqxButton({ disabled: true });
    //                            break;
    //                        case 'edit':
    //                            addButton.jqxButton({ disabled: true });
    //                            saveButton.jqxButton({ disabled: false });
    //                            editButton.jqxButton({ disabled: true });
    //                            deleteButton.jqxButton({ disabled: true });
    //                            cancelButton.jqxButton({ disabled: false });
    //                            break;
    //                        case 'disable':
    //                            addButton.jqxButton({ disabled: true });
    //                            saveButton.jqxButton({ disabled: true });
    //                            editButton.jqxButton({ disabled: true });
    //                            deleteButton.jqxButton({ disabled: true });
    //                            cancelButton.jqxButton({ disabled: true });
    //                            break;
    //                    }

    //                    if (!permissions.HasPermission("Relationship", "Create"))
    //                        addButton.jqxButton({ disabled: true });
    //                    if (!permissions.HasPermission("Relationship", "Update"))
    //                        editButton.jqxButton({ disabled: true });
    //                    if (!permissions.HasPermission("Relationship", "Delete"))
    //                        deleteButton.jqxButton({ disabled: true });
    //                }

    //                setButtonState('unselect');

    //                $(selector).on('rowSelect', function (event) {
    //                    setButtonState('select');

    //                    if (mode == 'edit') {
    //                        $(selector).jqxTreeGrid('endRowEdit', rowKey, true);
    //                    }
    //                    if (mode != 'saving') {
    //                        mode = '';
    //                    }
    //                    rowKey = event.args.key;
    //                });
    //                $(selector).on('rowDoubleClick', function (event) {
    //                    window.location.href = event.args.row.Url;
    //                });
    //                $(selector).on('bindingComplete', function (event) {
    //                    showFocalRow($(selector), event);
    //                });
    //                $(selector).on('rowUnselect', function () { setButtonState('unselect') });
    //                $(selector).on('rowEndEdit', function (event) {
    //                    setButtonState('save');
    //                    if (mode == 'add') {
    //                        $(selector).jqxTreeGrid('deleteRow', rowKey);

    //                    }
    //                });
    //                $(selector).on('rowBeginEdit', function () { setButtonState('edit') });

    //                addButton.click(function (event) {
    //                    if ($(selector).jqxTreeGrid('getSelection').length < 1)
    //                        return;
    //                    if (addButton.jqxButton('disabled'))
    //                        return;
    //                    if (mode != '')
    //                        return;

    //                    var row = $(selector).jqxTreeGrid('getRow', rowKey);
    //                    $(selector).jqxTreeGrid('expandRow', rowKey);
    //                    $(selector).jqxTreeGrid('addRow', newRowCounter--, { ID: row.ID, Level: row.Level }, 'first', rowKey);
    //                    $(selector).jqxTreeGrid('clearSelection');
    //                    $(selector).jqxTreeGrid('selectRow', newRowID);
    //                    $(selector).jqxTreeGrid('beginRowEdit', newRowID);
    //                    mode = 'add';
    //                });
    //                saveButton.click(function (event) {
    //                    var oldMode = mode;
    //                    if (saveButton.jqxButton('disabled'))
    //                        return;
    //                    mode = 'saving';
    //                    $(selector).jqxTreeGrid('endRowEdit', rowKey, false);
    //                    var rowData = $(selector).jqxTreeGrid('getRow', rowKey);

    //                    var obj = rowData.Object;
    //                    var objid = rowData.ObjectID;
    //                    var intersectMapId = (rowData.ID || 0);

    //                    if (rowData != null) {

    //                        var hierarchyPostModel = {
    //                            'Object': obj,
    //                            'ObjectID': objid,
    //                            IntersectMapID: intersectMapId
    //                        };

    //                        var url = '/relations/synonyms/save';
    //                        if (oldMode == 'edit') {
    //                            url = '/relations/synonyms/edit';
    //                            hierarchyPostModel.IntersectMapId = rowData.ID;
    //                        }

    //                        isUpdating = true;
    //                        setButtonState('disable');

    //                        $('#' + ctrlID + '_toolbar_spinner').show();
    //                        $.ajax({
    //                            url: url,
    //                            data: hierarchyPostModel,
    //                            method: 'POST'
    //                        }).success(function (data, status, xhr) {
    //                            amplify.publish("ShowMessage", data);
    //                        }).fail(function (data, status, xhr) {
    //                            amplify.publish("ShowMessage", data);
    //                        }).always(function () {
    //                            $(selector).jqxTreeGrid('updateBoundData');
    //                            $('#' + ctrlID + '_toolbar_spinner').hide();
    //                            isUpdating = false;
    //                            setButtonState('unselect');
    //                            mode = '';
    //                        });
    //                        $(selector).jqxTreeGrid('updateBoundData');
    //                    }
    //                });
    //                cancelButton.click(function (event) {
    //                    if (cancelButton.jqxButton('disabled'))
    //                        return;
    //                    $(selector).jqxTreeGrid('endRowEdit', rowKey, true);
    //                    if (mode == 'add')
    //                        $(selector).jqxTreeGrid('deleteRow', rowKey);
    //                });
    //                deleteButton.click(function (event) {
    //                    var oldMode = mode;
    //                    if (deleteButton.jqxButton('disabled'))
    //                        return;
    //                    if (oldMode != '')
    //                        return;
    //                    var selection = $(selector).jqxTreeGrid('getSelection');
    //                    if (selection == null || selection == [] || selection[0] == null)
    //                        return;
    //                    if (selection[0].ID == null || selection[0].ID < 1)
    //                        return;

    //                    isUpdating = true;
    //                    setButtonState('disable');
    //                    $('#' + ctrlID + '_toolbar_spinner').show();
    //                    $.ajax({
    //                        url: '/relations/synonyms/delete/' + selection[0].ID,
    //                        method: 'DELETE',
    //                        success: function (d) {
    //                            $(selector).jqxTreeGrid('updateBoundData');
    //                            $('#' + ctrlID + '_toolbar_spinner').hide();
    //                            isUpdating = false;
    //                            setButtonState('unselect');
    //                            mode = '';
    //                        },
    //                        failure: function (d) {
    //                            $(selector).jqxTreeGrid('updateBoundData');
    //                            $('#' + ctrlID + '_toolbar_spinner').hide();
    //                            isUpdating = false;
    //                            setButtonState('unselect');
    //                            mode = '';
    //                        }
    //                    });

    //                });
    //                editButton.click(function (event) {
    //                    if (editButton.jqxButton('disabled'))
    //                        return;
    //                    if (mode != '')
    //                        return;
    //                    mode = 'edit';
    //                    $(selector).jqxTreeGrid('beginRowEdit', rowKey);
    //                });

    //            } //Permissions check
    //        },
    //        columns: [
    //            {
    //                text: 'Artifact', dataField: 'Name', align: "center", columnType: "custom",
    //                cellsRenderer: function (rowKey, dataField, value, data) {
    //                    var item = getRowDataItem(data);
    //                    if (item.type == type && item.id == id) {
    //                        return "<div style='margin-left: 4px; display:inline-block;color:#33A'><div style='font-weight:600;'>" + value + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"
    //                    }
    //                    return "<div style='margin-left: 4px; display:inline-block;'><div style='font-weight:600;'>" + value + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"
    //                },
    //                createEditor: function (row, cellvalue, editor, cellText, width, height) {
    //                    if (mode == 'edit') {
    //                        var data = $(selector).jqxTreeGrid('getRow', row);
    //                        var item = getRowDataItem(data);
    //                        if (item.type == type && item.id == id) {
    //                            editor.append($("<div style='margin-left: 4px; display:inline-block;color:#33A'><div style='font-weight:600;'>" + data.Name + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"));
    //                        } else {
    //                            editor.append($("<div style='margin-left: 4px; display:inline-block;'><div style='font-weight:600;'>" + data.Name + "</div><div style='font-size:0.7em;'>" + data.ObjectTypeName + "</div><div style='clear: both;'></div></div>"));
    //                        }

    //                        return;
    //                    }

    //                    var rowData = $(selector).jqxTreeGrid('getRow', row);

    //                    var hierarchySubjectSource = {
    //                        datafields: [
    //                            { name: 'ObjectID' },
    //                            { name: 'Name' },
    //                            { name: 'Type' }
    //                        ],
    //                        datatype: "json",
    //                        url: '/relations/synonyms/artifacts',
    //                        data: { type: type, id: id }
    //                    };

    //                    var hierarchySubjectAdapter = new $.jqx.dataAdapter(
    //                        hierarchySubjectSource,
    //                        {
    //                            beforeLoadComplete: function (records) {
    //                                for (var i = 0; i < records.length; i++) {
    //                                    var record = records[i];
    //                                    record.Value = record.Object + '|' + record.ObjectID;
    //                                }
    //                                return records;
    //                            }
    //                        }
    //                    );

    //                    editor.jqxDropDownList({
    //                        theme: theme,
    //                        source: hierarchySubjectAdapter,
    //                        width: field_width,
    //                        height: field_height,
    //                        valueMember: 'Value',
    //                        displayMember: 'DisplayName',
    //                        filterable: true,
    //                        dropDownWidth: 350,
    //                        searchMode: 'containsignorecase'
    //                    });


    //                },
    //                getEditorValue: function (row, cellvalue, editor) {
    //                    var rowData = $(selector).jqxTreeGrid('getRow', row);
    //                    var selectedItem = editor.jqxDropDownList('getSelectedItem');
    //                    if (selectedItem == null)
    //                        return "";
    //                    if (selectedItem.originalItem == null)
    //                        return "";
    //                    var originalItem = selectedItem.originalItem;

    //                    rowData.ObjectTypeName = originalItem.ObjectTypeName;
    //                    rowData.Object = originalItem.Object;
    //                    rowData.ObjectID = originalItem.ObjectID;
    //                    return originalItem.Name;
    //                }
    //            }
    //        ]
    //    }
    //}

    //function initTreeGrid(selector, ctrlID) {
    //    $(selector).jqxTreeGrid(getTreeGrid(new $.jqx.dataAdapter(getAdapter()), selector, ctrlID));
    //}

    //function getRowDataItem(data) {

    //    if (data.Level > 0) {
    //        return {
    //            type: data.Object,
    //            id: data.ObjectID
    //        }
    //    } else {
    //        return {
    //            type: data.Subject,
    //            id: data.SubjectID
    //        }
    //    }
    //}

    //var c = controlID.substring(1);
    //initTreeGrid(controlID_hierarchy, c);

    //function showFocalRow(treeGrid, event) {
    //    if (event == null || event.args == null)
    //        return;
    //    var data = event.args.owner.source.loadedData;
    //    var focal = null;

    //    for (var i = 0; i < data.length; i++) {
    //        $(treeGrid).jqxTreeGrid('expandRow', data[i].UID);
    //        var item = getRowDataItem(data[i]);
    //        if (item.id == id && item.type == type) {
    //            focal = data[i];
    //            break;
    //        }
    //    }

    //    if (focal == null)
    //        return;

    //    $(treeGrid).jqxTreeGrid('ensureRowVisible', focal.UID);
    //}








    var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';
    var toolsControlID = controlID + "_tools";
    var source;
    var adapter;

    //#region Event Handlers

    function bindingComplete(event) {
        var count = 0;
        try {
            count = $('#' + controlID_sub).jqxGrid('getrows').length;
        } catch (e) {
            count = 0;
        }
        $('#' + controlID_count).html("&#160;(<b>" + count + "</b>)");
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    if (data.custom) {
                        if (data.custom.AttributeTypeID === 1) {
                            $('#' + controlID_sub).jqxGrid('updatebounddata');
                        }
                    }
                    break;
                case contextList.Synonym:
                    $('#' + controlID_sub).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : SynonymsTile : SaveAction", e);
        }
    }

    function expanded() {
        $('#' + controlID_sub).jqxGrid('updatebounddata');
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $('#' + controlID_sub).off('bindingcomplete', bindingComplete);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        $('#' + controlID).off('expanded', expanded);
    }

    //#endregion

    //#region Clean up previous control logic before re-creating

    var exists = false;
    try {
        var exp = $('#' + controlID).jqxExpander('animationType');
        if (exp) {
            exists = true;
        }
    } catch (e) { }

    //#endregion

    if (!exists) {
        $('#' + controlID).css('margin', '10px');
        $('#' + controlID).html('<div>Synonyms<span id="' + controlID_count + '"></span></div><div><div id="' + toolsControlID + '"></div><div id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });

        //if (permissions.HasPermission("Relationship", "Create")) {
        //    toolsControlID = '#' + toolsControlID;
        //    TileTools(toolsControlID, [
        //        { icon: 'plus', uri: '/form/AddSynonym?type=' + type + '&id=' + id, context: contextList.Synonym, title: 'Add synonym' }
        //    ]);
        //}
    }

    //#region Grid

    try {
        source = {
            datatype: 'json',
            url: '/api/' + type + '/' + id + '/synonyms',
            datafields:
            [
                { name: 'ID' },
                { name: 'Name' },
                { name: 'Description' },
                { name: 'Source' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        $('#' + controlID_sub).jqxGrid({
            source: adapter,
            width: overlay_grid_width,
            pagesizeoptions: ['5', '10', '20'],
            pagesize: 5,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            altrows: true,
            showfilterrow: true,
            filterable: true,
            pageable: false,
            theme: list_theme,
            columns: [
                { datafield: "Source", text: "Source", width: '250px', filtertype: 'checkedlist' },
                { datafield: "Name", text: "Name", width: '250px' },
                { datafield: "Description", text: "Description" }
            ]
        });
    }
    catch (e) {

    }

    //#endregion

    //#region Register Events

    $('#' + controlID_sub).on('bindingcomplete', bindingComplete);
    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
}