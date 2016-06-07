amplify.request.define("AttributeActionRequest", "ajax", { url: '/attributes/AttributeActions?type={type}&id={id}&owner={owner}&ownerID={ownerID}&attributeID={attributeID}', type: 'GET' });

function AttributesTile(controlID, contextList, permissions, type, id, headerTitle, readOnly) {
    var headerControlID = controlID + "Header";
    var treeControlID = controlID + "AttributeTree";
    var viewerControlID = controlID + "AttributeViewer";
    var toolbarControlID = controlID + "AttributeToolbar";
    var _detailControlID = controlID + "AttributeDetail";
    var editorControlID = controlID + "AttributeEditor";

    //#region Build HTML

    var html = '';
    html += '<header>' + headerTitle + '</header>';
    html += '<div class="row">';
    html += '<div class="col l5 m5 s6">';
    html += '<div id="' + treeControlID + '"></div>';
    html += '</div>';
    html += '<div class="col l7 m7 s6">';
    html += '<div id="' + viewerControlID + '">';
    html += '<div id="' + toolbarControlID + '"></div>';
    html += '<div id="' + _detailControlID + '"></div>';
    html += '</div>';
    html += '<div id="' + editorControlID + '"></div>';
    html += '</div>'; 
    html += '</div>';

    //#endregion 

    //#region Set proper jquery prefix on controls

    controlID = '#' + controlID;
    headerControlID = '#' + headerControlID;
    treeControlID = '#' + treeControlID;
    viewerControlID = '#' + viewerControlID;
    toolbarControlID = '#' + toolbarControlID;
    var detailControlID = '#' + _detailControlID;
    editorControlID = '#' + editorControlID;

    //#endregion

    //#region Clean up previous control logic before re-creating

    try { amplify.unsubscribe('AttributeToolAction'); } catch (e) { }
    try { amplify.unsubscribe('CancelAction'); } catch (e) { }
    try { amplify.unsubscribe('SaveAction'); } catch (e) { }
    try { $(treeControlID).jqxTreeGrid('destroy'); } catch (e) { }
    try { $(editorControlID).Editor('destroy'); } catch (e) { }
    $(controlID).html('');
    $(controlID).html(html);

    //#endregion

    var loadToolbar = function (type, id, owner, ownerID, attributeID) {
        if (!readOnly) {
            amplify.request("AttributeActionRequest", { type: type, id: id, owner: owner, ownerID: ownerID, attributeID: attributeID }, function (data) {
                if (data) {
                    $(toolbarControlID).html('');
                    var menu = $("<div style='border: none !important;'></div>");
                    menu.append(loadMenuItems(data, ""));
                    $(toolbarControlID).append(menu);
                    menu.jqxMenu({ showTopLevelArrows: false, enableRoundedCorners: false, theme: theme, autoOpenPopup: true, mode: 'horizontal' });
                    //$this.jqxMenu('setItemOpenDirection', 'TopMenu', 'left', 'down');
                    menu.bind('itemclick', function (event) {
                        var li = event.args;

                        if ($(li).data("uri") === null)
                            return;

                        attributeSwitchToEditor($(li).data("uri"));
                        //amplify.publish('AttributeToolAction', { uri: $(li).data("uri") });
                    });
                }
            });
        }
    }

    var loadMenuItems = function (data, html) {
        try {
            if (data) {

                html = "<ul>";

                $.each(data, function (idx, t) {
                    html += "<li data-uri='" + t.Uri + "'><i class='fa fa-" + t.Icon + "'";
                    if (t.Title !== "" && t.Title) {
                        html += " title='" + encodeURI(t.Title) + "'></i>" + t.Title
                    }
                    else {
                        html += "></i>";
                    }

                    if (t.Items.length > 0) {
                        html += loadMenuItems(t.Items);
                    }
                    html += "</li>";
                });

                html += "</ul>";
            }
        } catch (e) {
            logError("AttributesTile : loadMenuItems", e);
        }

        return html;
    }

    var attributeSwitchToEditor = function (uri) {
        $(viewerControlID).hide();
        $(editorControlID).fadeIn();
        $(editorControlID).load(uri);
    }

    var attributeSwitchToViewer = function (t, i) {
        $(editorControlID).html('');
        if (t === "Attribute" && i) {
            ObjectDetail(_detailControlID, t, i);
        }
        else {
            $(detailControlID).html('');
        }
        $(viewerControlID).fadeIn();
        $(editorControlID).hide();
    }

    //#region TreeGrid Logic

    var source = {
        dataType: "json",
        url: '/attributes/hierarchy/' + type + '/' + id,
        dataFields: [
            { name: 'ID' },
            { name: 'TypeID', type: 'int' },
            { name: 'IsCategory', type: 'bool' },
            { name: 'IsTechnical', type: 'bool' },
            { name: 'ShowNameInTree', type: 'bool' },
            { name: 'ObjectType', type: 'string' },
            { name: 'ObjectID', type: 'int' },
            { name: 'TargetObjectType', type: 'string' },
            { name: 'TargetObjectID', type: 'int' },
            { name: 'ParentObjectType', type: 'string' },
            { name: 'ParentObjectID', type: 'int' },
            { name: 'ObjectTypeName', type: 'string' },
            { name: 'expanded', type: 'bool' },
            { name: 'Name', type: 'string' },
            { name: 'Items', type: 'array' }
        ],
        hierarchy:
        {
            root: 'Items'
        },
        id: 'ID'
    };

    var dataAdapter = new $.jqx.dataAdapter(source);

    $(treeControlID).jqxTreeGrid({
        width: '99.5%',
        theme: list_theme,
        showHeader: false,
        selectionMode: 'singleRow',
        source: dataAdapter,
        sortable: true,
        icons: true,
        columns: [
          {
              text: 'Name',
              dataField: 'Name',
              width: '100%',
              cellsRenderer: function (rowKey, dataField, value, data) {
                  if (data.IsCategory) {
                      return "<span class='Attribute-Category'>" + data.Name + "</span>";
                  }
                  else {
                      return ((data.ShowNameInTree) ? "<b>" + data.ObjectTypeName + "</b> : " : "") + data.Name
                  }
              }
          }
        ],
        ready: function () {
            try {
                var rows = $(treeControlID).jqxTreeGrid('getRows');
                if (rows.length > 0) {
                    $(treeControlID).jqxTreeGrid('selectRow', ((rows[0].Items[0]) ? rows[0].Items[0].uid : rows[0].uid));
                }
            } catch (e) {
                console.log(e);
            }
        }
    });

    //#endregion

    //#region Event Subscriptions

    function cancelAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    var row = $(treeControlID).jqxTreeGrid('getSelection')[0];
                    if (row) {
                        attributeSwitchToViewer(row.ObjectType, row.ObjectID);
                    }
                    //removeOverlay();
                    break;
            }
        } catch (e) {
            logError("Children : CancelAction", e);
        }
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Attribute:
                    $(treeControlID).jqxTreeGrid('updateBoundData');
                    switch (data.action) {
                        case "add":
                            //load child items under selected tree node.
                            if (data.id) {
                                $(treeControlID).jqxTreeGrid('selectRow', data.id);
                                attributeSwitchToViewer('Attribute', data.id);
                            }
                            break;
                        case "delete":
                            attributeSwitchToViewer(null, null);
                            break;
                        case "edit":
                            //reload selected tree node.
                            $(treeControlID).jqxTreeGrid('selectRow', data.id);
                            attributeSwitchToViewer('Attribute', data.id);
                            break;
                    }
                    break;
            }
        } catch (e) {
            logError("Children : SaveAction", e);
        }
    }

    function treeControlBindingComplete(evt) {
        var calculateCount = function (row, count) {
            if (row.records) {
                count += row.records.length;
                $.each(row.records, function () {
                    count = calculateCount(this, count);
                });
            }
            return count;
        };

        var count = 0;
        try {
            var topRows = $(treeControlID).jqxTreeGrid('getRows');
            $.each(topRows, function () {
                count = calculateCount(this, count);
            });
        } catch (e) {
            count = 0;
        }
        amplify.publish("AttributeCount", { count: count });
    }


    function treeControlRowSelect(evt) {
        try {
            // event args.
            var args = evt.args;
            // row data.
            var row = args.row;
            // row key.
            var key = args.key;

            if (row) {
                var t = row.ObjectType;//null;
                var i = row.ObjectID;//null;
                var detailtype = null;
                var detailid = null;
                var roottype = row.ParentObjectType; //null;
                var rootid = row.ParentObjectID; //null;
                var attributeID = null;
                var targetType = row.TargetObjectType;

                if (t === 'Attribute') {
                    attributeID = i;
                }

                if (targetType) {
                    detailtype = targetType;
                    detailid = row.TargetObjectID;
                }
                else {
                    detailtype = t;
                    detailid = i;
                }

                loadToolbar(t, i, roottype, rootid, attributeID);

                attributeSwitchToViewer(targetType, row.TargetObjectID);
                if (detailid && detailtype === "Attribute") {
                    ObjectDetail(_detailControlID, detailtype, detailid);
                }
                else {
                    $(detailControlID).html('');
                }
            }
        } catch (e) {
            logError("Children : AttributeTree.select", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        dataAdapter = null;

        amplify.unsubscribe("CancelAction", cancelAction);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        $(treeControlID).off('bindingComplete', treeControlBindingComplete);
        $(treeControlID).off('rowSelect', treeControlRowSelect);
    }

    amplify.subscribe("CancelAction", cancelAction);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    $(treeControlID).on("rowSelect", treeControlRowSelect);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    $(treeControlID).on('bindingComplete', treeControlBindingComplete);

    //#endregion
}