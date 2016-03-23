amplify.request.define("AttributeActionRequest", "ajax", { url: '/attributes/AttributeActions?type={type}&id={id}&owner={owner}&ownerID={ownerID}&attributeID={attributeID}', type: 'GET' });

//#region Utilities

function gridExists(controlID) {
    try {
        var state = $(controlID).jqxGrid('getstate');
        return (state !== null);
    } catch (e) {
        return false;
    }
}
function PermissionsModel() {
    var self = this;

    self.permissions = [];

    self.GetPermissionsForObject = function (type, id) {
        var pr = new $.Deferred();

        $.ajax({
            url: '/api/' + type + '/' + id + '/permissions',
            method: 'GET',
            success: function (data, status, xhr) {
                self.permissions = data;
            },
            error: function (xhr, status, error) {
                self.permissions = [];
            },
            complete: function (xhr, status) {
                amplify.publish('PermissionsLoaded', { Type: type, ID: id });
                pr.resolve();
            }
        });

        return pr.promise();
    }

    self.HasPermission = function (claimObject, claim) {
        var has = false;
        for (var i = 0; i < self.permissions.length; i++) {
            var p = self.permissions[i];
            if (p.ClaimObject === claimObject && p.Claim === claim) {
                has = true;
                break;
            }
        }
        return has;
    }
}

function drawKpi(controlID, title, total, available, isPercentage) {
    var data = [];
    data.push({ text: 'Currently', value: total }); // current
    data.push({ text: 'Available', value: available }); // remaining
    var settings = {
        title: title,
        description: '',
        enableAnimations: true,
        showLegend: false,
        showBorderLine: false,
        backgroundColor: '#ffffff',
        padding: { left: 1, top: 1, right: 1, bottom: 1 },
        titlePadding: { left: 0, top: 0, right: 0, bottom: 0 },
        source: data,
        showToolTips: false,
        seriesGroups:
        [
            {
                type: 'donut',
                useGradientColors: false,
                series:
                    [
                        {
                            showLabels: false,
                            enableSelection: false,
                            displayText: 'text',
                            dataField: 'value',
                            labelRadius: 120,
                            initialAngle: 90,
                            radius: 60,
                            innerRadius: 50,
                            centerOffset: 0
                        }
                    ]
            }
        ]
    };

    settings.drawBefore = function (renderer, rect) {
        var text = ((total === null) ? '-' : total + (isPercentage ? "%" : ""));
        sz = renderer.measureText(text, 0, { 'class': 'kpi-inner-text' });
        
        renderer.text(
                text,
                rect.x + (rect.width - sz.width) / 2,
                rect.y + rect.height / 2,
                0,
                0,
                0,
                { 'class': 'kpi-inner-text' }
            );
    }
    $(controlID).jqxChart(settings);
    $(controlID).jqxChart('addColorScheme', 'customColorScheme', ['#3f9d40', '#EDE6E7']);
    $(controlID).jqxChart({ colorScheme: 'customColorScheme' });
}

function TileTools(toolsControlID, tools) {
    $(toolsControlID).addClass('TileTools');
    $(toolsControlID).html('');

    var internalToolClick = function () {
        amplify.publish(AmplifyActions.InternalTool, { action: $(this).data("action") });
    }
    var toolClick = function () {
        amplify.publish(AmplifyActions.Tool, { uri: $(this).data("uri"), context: $(this).data("context") });
    }

    var unsubscribe = function() {
        $.each($(toolsControlID).find('i'), function () {
            $(this).off('click', toolClick);
        });
    }

    $.each(tools, function () {
        var tool = $("<a class='btn-floating waves-effect waves-light brown lighten-1'><i class='fa fa-" + this.icon + "' title='" + this.title + "'></i></a>");
        if (this.action) {
            tool.data('action', this.action);
            tool.on('click', internalToolClick);
        }
        else {
            tool.data('uri', this.uri);
            tool.data('context', this.context);
            tool.on('click', toolClick);
        }
        $(toolsControlID).append(tool);
    });

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
}

//#endregion

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
    try { $(treeControlID).jqxTreeGrid('destroy'); } catch(e){}
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

function AttributeTypeAllocationGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Allocations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/AttributeType/' + id + '/allocations',
            datafields:
            [
                { name: 'ObjectID' },
                { name: 'AttributeTypeID' },
                { name: 'ObjectName' },
                { name: 'ObjectType' },
                { name: 'AllowMultipleEntries' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddAttributeTypeRelation?id=" + id, context: contextList.AttributeTypeRelation, title: 'Add allocation' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { text: 'Object Type', dataField: 'ObjectType' },
                { text: 'Object Name', dataField: 'ObjectName' },
                { text: 'Allow Multiple Entries?', dataField: 'AllowMultipleEntries', width: 125, cellsrenderer: booleanrenderer },
                { 
                    text: '', 
                    dataField: 'AttributeTypeID', 
                    width: 80, 
                    filterable: false, 
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditAttributeTypeRelation?id=' + data.AttributeTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteAttributeTypeRelation?id=' + data.AttributeTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID });
                        }

                        return renderToolsHtml(value, tools, contextList.AttributeTypeRelation);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.AttributeTypeRelation:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : AttributeTypeAllocationsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function CertificationNotificationTile(controlID, id) {
    controlID = '#' + controlID;
    var buttonControlID = controlID + "_button";

    var getWorkflowForItem = function () {
        $.getJSON('/workflow/CertificationNotification?id=' + id, function (data) {

            $(controlID).html('');

            if (data.WorkflowID) {
                $(controlID).append('<article>');
                $(controlID).append('<header>Certification Is Due</header>');
                $(controlID).append('<div style="text-align: center; margin-bottom: 15px">You need to certify this item.</div>');
                $(controlID).append('<div style="text-align: center; margin-bottom: 15px"><button id="' + buttonControlID + '" type="button" class="btn btn-success" onclick="ClickGridTool(event)" data-context="Workflow" data-uri="/workflow/' + data.WorkflowID + '/overlay">Certify Now!</button></div>');
                $(controlID).append('</article>');
                $(controlID).fadeIn(250);
            }
            else {
                $(controlID).fadeOut(250);
            }

            //$('#' + buttonControlID).on('click', function () {
            //    //'workflow/' + data.WorkflowID + '/overlay'
            //});
        });
    }

    getWorkflowForItem();

    function saveAction(data) {
        try {
            switch (data.context) {
                case "Workflow":
                case "RequestCertification":
                    getWorkflowForItem();
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

}

function CriticalRelationshipsTile(controlID, type, id) {

    var source = {
        datatype: 'json',
        type: 'get',
        datafields: [
            { name: "IntersectID" },
            { name: "ID" },
            { name: "ObjectType" },
            { name: 'TypeName' },
            { name: 'Url' },
            { name: 'Name' },
            { name: 'Description' },
            { name: 'IconBackColor' },
            { name: 'IconForeColor' },
            { name: 'IconText' }
        ],
        url: '/api/' + type + '/' + id + '/relations/critical'
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(controlID).jqxGrid({
            source: adapter,
            width: grid_width,
            pagesizeoptions: ['5', '10', '20'],
            pagesize: 20,
            autoheight: true,
            sortable: true,
            altrows: true,
            showfilterrow: true,
            filterable: true,
            groupsrenderer: function (text, group, expanded, data) {
                return '<h2>' + group + '</h2>';
            },
            groupable: true,
            groupsexpandedbydefault: true,
            showgroupsheader: true,
            groups: ['TypeName'],
            pageable: true,
            theme: list_theme,
            columns: [
                { datafield: "TypeName", text: "Type", hidden: true },
                {
                    datafield: "Name",
                    text: "Name",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return previewLinkRenderer(data.ObjectType, data.ID, data.Url, data.Name);
                    }
                },
                { datafield: "Description", text: "Description" },
            ]
        });
    } catch (e) {
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case 'Intersect':
                    $(controlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : CriticalRelationshipsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
}

function CollapsibleAttributesTile(controlID, contextList, permissions, type, id) {
    var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';

    //#region Event Handlers

    function attributeCountNotice(data) {
        $('#' + controlID_count).html("&#160;(<b>" + data.count + "</b>)");
    }

    function expanded() {
        AttributesTile(controlID_sub, contextList, permissions, type, id, '', false);
    }

    function unsubscribe(data) {
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe("AttributeCount", attributeCountNotice);
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
    try { unsubscribe({}); } catch (e) { }

                    //#endregion

    if (!exists) {
        $('#' + controlID).css('margin', '10px');
        $('#' + controlID).html('<div>Attributes<span id="' + controlID_count + '"></span></div><div style="min-height: 150px"><div id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });
                    }
    AttributesTile(controlID_sub, contextList, permissions, type, id, '', false);

    //#region Register Events

    amplify.subscribe("AttributeCount", attributeCountNotice);
    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

                    //#endregion
                    }

function CollapsibleTypeHierarchyTile(controlID, contextList, permissions, type, id) {
    var controlID_sub = controlID + '_Sub';

    //#region Event Handlers

    function expanded() {
        HierarchyTile(controlID_sub, contextList, permissions, type, id, 3);
    }

    function unsubscribe(data) {
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        $('#' + controlID).off('expanded', expanded);
    }

    //#endregion

    //#region Clean up previous control logic before re-creating

    try { unsubscribe({}); } catch (e) { }
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
        $('#' + controlID).html('<div>Structure</div><div style="min-height: 150px"><div style="width:99%" id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });
    }

    HierarchyTile(controlID_sub, contextList, permissions, type, id, 3);

    //#region Register Events

    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
        }

function CollapsibleSynonymsTile(controlID, contextList, permissions, type, id) {
    var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';
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

//    try { unsubscribe({}); } catch (e) { }

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
        $('#' + controlID).html('<div>Synonyms<span id="' + controlID_count + '"></span></div><div><div id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });
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

function ObjectDetail(controlID, type, id) {
    var tmpl = Handlebars.getTemplate('DetailTile');

    var processFieldLabel = function (fix, f) {
        f.labelID = controlID + '_' + f.FieldName;
        f.valueID = controlID + '_val_' + f.FieldName;
        if (f.ScriptProperty) {
            f.Name = eval(f.ScriptProperty);
        }
    }

    var processFieldDetails = function (fix, f) {
        var labelID = '#' + f.labelID;
        var valueID = '#' + f.valueID;

        //#region Create tooltips where there are field descriptions
        if (f.FieldDescription && f.FieldDescription != '') {
            $(labelID).qtip({
                content: {
                    text: f.FieldDescription,
                    position: {
                        at: 'bottom center', // Position the tooltip above the link
                        my: 'top center',
                        viewport: $(window), // Keep the tooltip on-screen at all times
                        effect: false // Disable positioning animation
                    }
                },
                style: {
                    classes: 'qtip-blue qtip-rounded'
                }
            });
        }
        //#endregion

        //#region Load field values

        if (f.TooltipContext && f.TooltipID && f.TooltipType && f.TooltipUrl) {
            $(valueID).html("<a href='" + f.TooltipUrl +
                "' data-type='" + f.TooltipType +
                "' data-context='" + f.TooltipContext +
                "' data-id='" + f.TooltipID + "'>" +
                f.Value + "</a>");
        }
        else if (f.LookupGridUrl) {
            $.getJSON(f.LookupGridUrl, function (data) {

                var fields = data.Fields;
                var res = data.Values;
                var cols = data.Columns;

                var source = {
                    localdata: res,
                    datatype: 'json',
                    datafields: fields
                };

                var dataAdapter = new $.jqx.dataAdapter(source);

                var tooltiprenderer = function (element) {
                    $(element).parent().jqxTooltip({ position: 'mouse', content: v.FieldDescription });
                }

                var cn = null;
                $.each(cols, function () {
                    if (this.datafield == "Name") {
                        cn = this;
                    }
                });
                if (cn) {
                    cn.width = "30%";
                    cn.cellsRenderer = function (index, datafield, value, defaultvalue, column, data) {
                        return "<div class='d3s-cell' style='overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 4px;'><a data-context='Preview' data-type='" + data.Object + "' data-id='" + data.ID + "' href='" + data.Url + "'>" + data.Name + "</a></div>";
                    }
                }

                var cp = null;
                $.each(cols, function () {
                    if (this.datafield == "TextPath") {
                        cp = this;
                    }
                });
                if (cp) {
                    cp.width = "40%";
                    cp.cellsRenderer = function (index, datafield, value, defaultvalue, column, data) {
                        return "<div class='d3s-cell' style='overflow: hidden; text-overflow: ellipsis; padding-bottom: 2px; text-align: left; margin-right: 2px; margin-left: 4px; margin-top: 4px;'><a data-context='Preview' data-type='" + data.Object + "' data-id='" + data.ID + "' href='" + data.Url + "'>" + data.TextPath + "</a></div>";
                    }
                }

                $(valueID).jqxGrid({
                    altrows: true,
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 10,
                    showemptyrow: false,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: false,
                    showheader: !f.HideHeader,
                    pageable: !f.HideFooter,
                    columnsresize: true,
                    source: dataAdapter,
                    theme: 'flat',
                    pagermode: 'simple',
                    columns: cols//,
                    //ready: function () {
                    //    $(valueID).jqxGrid('autoresizecolumns');
                    //}
                });
            });
        }
        else {
            if (f.Value != null && f.Value.match(/(\d{4})-(\d{2})-(\d{2})T(\d{2})\:(\d{2})\:(\d{2})/)) {
                f.Value = f.Value.replace(/["]/g, "");
                var d = new Date(f.Value);
                $(valueID).html(d.toLocaleString());
            }
            else
                $(valueID).html(f.Value);
        }

        //#endregion
    }

    $.getJSON('/api/' + type + '/' + id + '/detail', function (model) {

        model.control = controlID;

        //#region Update friendly names where there are script code items
        $.each(model.rows, function (rix, r) {
            r.hasOneColumn = (r.columns == 1);
            $.each(r.FirstColumnFields, processFieldLabel);
            $.each(r.SecondColumnFields, processFieldLabel);
        });
        //#endregion

        $('#' + controlID).html(tmpl(model));

        $.each(model.rows, function (rix, r) {
            $.each(r.FirstColumnFields, processFieldDetails);
            $.each(r.SecondColumnFields, processFieldDetails);
        });

    });
}

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
            { name: 'PredicatePhrase', type: 'string'},
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

function DomainAllocationsTile(controlID, contextList, permissions, typeID, domainID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Usage</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var srcAllocationsGrid = {
        datatype: 'json',
        url: '/api/domains/' + typeID + '/' + domainID + '/allocations',
        datafields:
        [
            { name: 'AttributeTypeID', type: 'number' },
            { name: 'LocationType' },
            { name: 'Location' },
            { name: 'Type' },
            { name: 'Name' }
        ]
    };

    var adapterAllocationsGrid = new $.jqx.dataAdapter(srcAllocationsGrid);

    $(gridControlID).jqxGrid({
        altrows: true,
        width: grid_width,
        autoheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: true,
        pageable: true,
        pagesizeoptions: ['10', '20', '50'],
        pagesize: 20,
        source: adapterAllocationsGrid,
        theme: list_theme,
        columns: [
            { text: 'Object Type', dataField: 'Type', columntype: 'dropdownlist', filtertype: 'checkedlist' },
            { text: 'Object Name', dataField: 'Name' },
            { text: 'Location Type', dataField: 'LocationType', columntype: 'dropdownlist', filtertype: 'checkedlist' },
            { text: 'Location', dataField: 'Location' }
        ]
    });

    //#endregion

    //#region Event Subscriptions

    function unsubscribe(data) {
        srcAllocationsGrid = null;
        adapterAllocationsGrid = null;
    }

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function DomainItemsTile(controlID, contextList, permissions, typeID, domainID) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Items<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    var srcDomainItemsGrid = {
        datatype: 'json',
        url: '/services/domains/' + typeID + '/lists/' + domainID,
        datafields:
        [
            { name: 'ID' },
            { name: 'Name' },
            { name: 'Code' },
            { name: 'Description' }
        ]
    };

    var adapterDomainItemsGrid = new $.jqx.dataAdapter(srcDomainItemsGrid);

    var tools = [];
    if (permissions.HasPermission("Root", "Update")) {
        tools.push({ icon: 'plus', uri: '/form/AddDomainItem?typeID=' + typeID + '&listID=' + domainID, context: contextList.DomainItem, title: 'Add domain item' });
    }
    TileTools(toolsControlID, tools);

    $(gridControlID).jqxGrid({
        altrows: true,
        width: grid_width,
        autoheight: true,
        autorowheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: true,
        pageable: true,
        pagesizeoptions: ['10', '20', '50'],
        pagesize: 20,
        source: adapterDomainItemsGrid,
        theme: list_theme,
        columns: [
            { text: 'Name', dataField: 'Name' },
            { text: 'Code', dataField: 'Code' },
            { text: 'Description', dataField: 'Description' },
            {
                text: '',
                dataField: 'ID',
                width: 80,
                filterable: false,
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                    var tools = [];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditDomainItem?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteDomainItem?id={0}' });
                    }

                    return renderToolsHtml(value, tools, contextList.DomainItem);
                }
            }
        ]
    });

    //#endregion

    //#region Event Subscriptions

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.DomainItem:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("DomainItemsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        srcDomainItemsGrid = null;
        adapterDomainItemsGrid = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function EventAgeBreakdownChart(controlID, contextList, type, id, timescale) {

    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + chartControlID + '" style="width: 100%; height: 225px;"></div>')
    chartControlID = '#' + chartControlID;

    var sUrl = '/queries/' + type + '/' + id + '/EventAgeBreakdown';

    var src = {
        datatype: 'json',
        type: 'get',
        url: '/queries/' + type + '/' + id + '/EventAgeBreakdown' + ((timescale !== '' && timescale) ? "?maxHistoryDays=" + timescale : ""),
        datafields:
        [
            { name: 'Date', type: 'date' },//{ name: 'Status', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(src);

    $(chartControlID).jqxChart({
        title: "By Age",
        description: "",
        enableAnimations: true,
        showLegend: true,
        showBorderLine: false,
        //padding: { left: 0, top: 25, right: 75, bottom: 0 },
        //titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
        //legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
        source: adapter,
        colorScheme: chartDefaultTheme,
        xAxis: {
            dataField: 'Date',
            type: 'date',
            baseUnit: 'day',
            visible: false,
            valuesOnTicks: false,
            tickMarks: {
                visible: false,
                interval: 1,
                color: '#BCBCBC'
            },
            unitInterval: 1,
            gridLines: {
                visible: false,
                interval: 3,
                color: '#BCBCBC'
            },
            labels: {
                angle: -45,
                rotationPoint: 'topright',
                offset: { x: 0, y: -25 }
            }
        },
        valueAxis:
        {
            visible: true,
            minValue: 0,
            //unitInterval: 1,
            title: { text: 'Total Events By Day<br>' },
            tickMarks: { color: '#BCBCBC' }
        },
        colorScheme: 'scheme04',
        seriesGroups:
            [
                {
                    type: 'line',
                    series: [
                            { dataField: 'Count', displayText: '# Events' }
                    ]
                }
            ]

        //xAxis: {
        //    dataField: 'Status',
        //    showGridLines: true
        //},
        //seriesGroups: [{
        //    useGradientColors: false,
        //    type: 'column',
        //    columnsGapPercent: 50,
        //    valueAxis:
        //    {
        //        unitInterval: 10,
        //        displayValueAxis: true,
        //        description: '# Events'
        //    },
        //    series: [
        //            { dataField: 'Count', displayText: 'Age In Days'}
        //    ]
        //}]
    });
}

function EventCriticalityBreakdownChart(controlID, contextList, type, id, timescale) {

    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + chartControlID + '" style="width: 100%; height: 225px;"></div>')
    chartControlID = '#' + chartControlID;

    var src = {
        datatype: 'json',
        type: 'get',
        url: '/queries/' + type + '/' + id + '/EventCriticalityBreakdown' + ((timescale !== '' && timescale) ? "?maxHistoryDays=" + timescale : ""),
        datafields:
        [
            { name: 'Criticality', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(src);

    $(chartControlID).jqxChart({
        title: "By Criticality",
        description: "",
        enableAnimations: true,
        showLegend: true,
        showBorderLine: false,
        //padding: { left: 0, top: 25, right: 75, bottom: 0 },
        //titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
        //legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
        source: adapter,
        colorScheme: chartDefaultTheme,
        seriesGroups: [{
            type: 'pie',
            useGradientColors: false,
            showLabels: true,
            series: [
                {
                    useGradient: false,
                    dataField: 'Count',
                    displayText: 'Criticality',
                    labelRadius: 50,
                    initialAngle: 15,
                    radius: 75,
                    centerOffset: 0
                }
            ]
        }]
    });
}

function EventsGrid(controlID, contextList, id, selectedEventID, showCommands, hideTitle) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    var html = "";
    if (!hideTitle) {
        html += "<header>Event Details</header>";
    }
    html += '<div id="' + gridControlID + '"></div>';
    $(controlID).html(html);
    gridControlID = '#' + gridControlID;

    $.getJSON('/api/EventGroup/' + id + '/grid/definition', function (gridinfo) {

        var src = {
            datatype: 'json',
            url: '/Monitor/EventsByHeader?groupID=' + id,
            datafields: gridinfo.Fields,
            beforeprocessing: function (data) {
                src.totalrecords = data.total;
            },
            filter: function () {
                $(gridControlID).jqxGrid('updatebounddata');
            },
            sort: function () {
                $(gridControlID).jqxGrid('updatebounddata');
            },
            id: 'ID'
        };

        var adapter = new $.jqx.dataAdapter(src);

        if (showCommands) {
            gridinfo.Columns.push({
                datafield: "ID",
                text: "",
                sortable: false,
                filterable: false,
                width: '80px',
                resizable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    tools.push({ icon: 'info', urlprefix: '/overlays/Event/' + data.ID + '/detail' });
                    tools.push({ icon: 'pencil', urlprefix: '/form/EditEvent?id=' + data.ID });

                    return renderToolsHtml(value, tools, contextList.Event, data);
                }
            });
        }

        try {
            $(gridControlID).jqxGrid({
                width: grid_width,
                pagesizeoptions: ['5', '10', '20', '50'],
                pagesize: 20,
                autoheight: true,
                autorowheight: true,
                sortable: true,
                altrows: true,
                filterable: true,
                showfilterrow: true,
                virtualmode: true,
                rendergridrows: function () {
                    return adapter.records;
                },
                pageable: true,
                columnsresize: true,
                source: adapter,
                theme: list_theme,
                columns: gridinfo.Columns,
                ready: function () {
                    if (selectedEventID) {
                        var selectedEventIndex = $(gridControlID).jqxGrid('getrowboundindexbyid', selectedEventID);
                        if (selectedEventIndex > -1) {
                            $(gridControlID).jqxGrid('selectrow', selectedEventIndex);
                        }
                    }
                    $(gridControlID).jqxGrid('autoresizecolumns');
                }
            });
        } catch (e) {

        }
    });

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Event:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : EventsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function EventHeadersGrid(controlID, contextList, type, id, selectedGroupID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Event History</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var EventHeadersGridSource = {
        datatype: 'json',
        url: '/Monitor/EventHeaders?ruleID=' + id,
        datafields: [
            { name: 'ID', type: 'number' },
            { name: 'PublicID', type: 'string' },
            { name: 'Date', type: 'date' },
            { name: 'Rule', type: 'string' },
            { name: 'NumberOfEvents', type: 'number' },
            { name: 'Name', type: 'string' }
        ],
        beforeprocessing: function (data) {
            EventHeadersGridSource.totalrecords = data.total;
        },
        filter: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        sort: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        id: 'ID'
    };

    var EventHeadersGridAdapter = new $.jqx.dataAdapter(EventHeadersGridSource);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            virtualmode: true,
            pageable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            columnsresize: true,
            enabletooltips: true,
            rendergridrows: function () {
                return EventHeadersGridAdapter.records;
            },
            source: EventHeadersGridAdapter,
            theme: list_theme,
            columns: [
                { datafield: "Date", text: "Last Event Date", cellsformat: 'MM/dd/yyyy HH:mm:ss', columntype: "datetimeinput", filtertype: "range", width: 150 },
                { datafield: "Name", text: "Name" },
                { datafield: "NumberOfEvents", text: "# Events", filtertype: "number", width: 100 },
                { datafield: "PublicID", text: "Public ID", width: 250 },
                {
                    text: '',
                    dataField: 'ID',
                    width: '80px',
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { icon: 'info', urlprefix: '/overlays/EventGroup/' + data.ID + '/detail' },
                            { icon: 'pencil', urlprefix: '/form/EditEventGroup?id=' + data.ID }
                        ];
                        return renderToolsHtml(value, tools, contextList.EventGroup);
                    }
                }
            ],
            ready: function () {
                if (selectedGroupID) {
                    var selectedGroupIndex = $(gridControlID).jqxGrid('getrowboundindexbyid', selectedGroupID);
                    if (selectedGroupIndex > -1) {
                        $(gridControlID).jqxGrid('selectrow', selectedGroupIndex);
                    }
                }
            }
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function gridRowSelect(event) {
        var args = event.args;              // event arguments.
        var rowBoundIndex = args.rowindex;  // row's bound index.
        var rowData = args.row;             // row's data.

        amplify.publish('EventHeaderSelected', {
            GroupID: rowData.ID
        });
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Event:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : EventHeadersGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on('rowselect', gridRowSelect);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function EventStatusBreakdownChart(controlID, contextList, type, id, timescale) {

    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + chartControlID + '" style="width: 100%; height: 225px;"></div>')
    chartControlID = '#' + chartControlID;

    var src = {
        datatype: 'json',
        type: 'get',
        url: '/queries/' + type + '/' + id + '/EventStatusBreakdown' + ((timescale != '' && timescale) ? "?maxHistoryDays=" + timescale : ""),
        datafields:
        [
            { name: 'Status', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(src);

    $(chartControlID).jqxChart({
        title: "By Status",
        description: "",
        enableAnimations: true,
        showLegend: true,
        showBorderLine: false,
        //padding: { left: 0, top: 25, right: 75, bottom: 0 },
        //titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
        //legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
        source: adapter,
        colorScheme: chartDefaultTheme,
        seriesGroups: [{
            type: 'pie',
            useGradientColors: false,
            showLabels: true,
            series: [
                {
                    useGradient: false,
                    dataField: 'Count',
                    displayText: 'Status',
                    labelRadius: 50,
                    initialAngle: 15,
                    radius: 75,
                    centerOffset: 0
                }
            ]
        }]
    });
}

function FieldsGrid(controlID, contextList, permissions, type, id, title) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var displayTools = ((type != 'AttributeType') || (type == 'AttributeType' && id >= 50000));

    var source;
    var adapter;

    //#region Grid

    if (!title || title <= '') {
        title = 'Field Definition';
    }

    try {
        $(controlID).html('<header>' + title + '<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/fields/' + type + '/' + id + '.json',
            datafields:
            [
                { name: 'ObjectType' },
                { name: 'ObjectID' },
                { name: 'ID' },
                { name: 'FriendlyName' },
                { name: 'SortOrder' },
                { name: 'IsRequired' },
                { name: 'IsListable' },
                { name: 'DisplayDescription' },
                { name: 'FormDescription' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (displayTools && permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddFieldType?type=' + type + '&id=' + id, context: contextList.FieldType, title: 'Add definition attribute' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "FriendlyName", text: "Field" },
                { datafield: "SortOrder", text: "Order", columntype: 'numberinput', filtertype: 'number', width: 70 },
                { datafield: "IsRequired", text: "Required?", columntype: 'checkbox', filtertype: 'bool', width: 70 },
                { datafield: "IsListable", text: "Listable?", columntype: 'checkbox', filtertype: 'bool', width: 70 },
                {
                    text: '', dataField: 'ID', width: '150px', filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (displayTools) {
                            if (permissions.HasPermission("Root", "Update")) {
                                tools.push({ icon: 'caret-up', urlprefix: '/fields/' + data.ObjectType + '/' + data.ObjectID + '/' + data.ID + '/move/up', context: 'action' });
                                tools.push({ icon: 'caret-down', urlprefix: '/fields/' + data.ObjectType + '/' + data.ObjectID + '/' + data.ID + '/move/down', context: 'action' });
                                tools.push({ icon: 'pencil', urlprefix: '/form/EditFieldType?id={0}' });
                                tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFieldType?id={0}' });
                            }
                        }

                        return renderToolsHtml(value, tools, contextList.FieldType);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function commandExecuted(command) {
        try {
            if (command == "FieldMove") {
                $(gridControlID).jqxGrid('updatebounddata');
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("CommandExecuted", commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe('CommandExecuted', commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function _FusionItemsGrid(controlID, fusionTypeID, fusionID, defaultTypeDefinition, definition, dataUri,id) {
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;
    $(controlID).html('<div id="' + gridControlID + '"></div>');
    gridControlID = '#' + gridControlID;

    $('#FusionAttributeDetailsTile').hide();
        
    //#region Event Handlers

    var doubleClick = function (event) {        
        var args = event.args;
        var boundIndex = args.rowindex;     // row's bound index.
        var data = $(gridControlID).jqxGrid('getrowdata', boundIndex);

        var dataUri = '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID;

        if (data.Type == 'FusionAttribute') {
            // The next level will be the attribute types under this parent attribute.
            dataUri += "&parentType=FusionAttributeType" +
                "&parentID=" + data.FusionAttributeTypeID +
                "&parentFusionAttributeID=" + data.ID;

            amplify.publish('FusionItemSelected', {
                definition: defaultTypeDefinition,
                uri: dataUri,
                title: data.Name,
                type: data.Type,
                id: data.ID
            });
        }
        else {            
            // The next level will be the attributes under this parent attribute type.
            dataUri += "&parentType=FusionAttribute&parentID=" + data.ParentFusionAttributeID;
            dataUri += "&parentFusionAttributeTypeID=" + data.ID;
            $.getJSON("/api/FusionAttributeType/" + data.ID + "/grid/definition", function (definition) {
                definition.Fields.push({ name: 'FusionAttributeTypeID', type: 'number' });

                amplify.publish('FusionItemSelected', {
                    definition: definition,
                    uri: dataUri,
                    title: data.Name,
                    type: data.Type,
                    id: data.ID
                });
            });
        }        
    }

    var gridRowSelect = function(event) {
        var args = event.args;              // event arguments.
        var rowBoundIndex = args.rowindex;  // row's bound index.
        var rowData = args.row;             // row's data.
        if (rowData.Type === 'FusionAttribute') {
            amplify.publish('FusionAttributeRowSelected', {
                ID: rowData.ID,
                Name: rowData.Name
            });
        }
    }

    var pageResized = function() {
        $(gridControlID).jqxGrid('refresh');
    }

    var unsubscribe = function(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    // in case where we dont have the definition we need to get it this happens on goto initial selected item
    if(definition === null && id !== null){        
        $.ajax({
            type:"GET",
            url: "/api/FusionAttributeType/" + id + "/grid/definition",
            async: false,
            contentType:'application/json',
            dataType: 'json',
            success: function (data) {
                definition = data;
                definition.Fields.push({ name: 'FusionAttributeTypeID', type: 'number' });
            }
        });
     }

    //#endregion
    //modify type column
    definition.Columns.forEach(function (item) {
        if (item.datafield && item.datafield.toUpperCase() === 'TYPE') item.datafield = '_type';
    });

    definition.Fields.forEach(function(item){
        if (item.name && item.name.toUpperCase() === 'TYPE') item.name = '_type';
    });    
    //add internal type
    definition.Fields.push({ name: 'Type', type: 'string' });

    var source = {
        datatype: 'json',
        type: 'get',
        url: dataUri,
        datafields: definition.Fields,
        beforeprocessing: function (data) {
            source.totalrecords = data.total;
        },
        filter: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        },
        sort: function () {
            $(gridControlID).jqxGrid('updatebounddata');
        }
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            //showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            virtualmode: true,
            rendergridrows: function () {
                return adapter.records;
            },
            columnsresize: true,
            source: adapter,
            ready: function () {
                if (definition.Columns.length > 7) {
                    $(gridControlID).jqxGrid('pincolumn', 'Name');
                    $(gridControlID).jqxGrid('autoresizecolumns');
                }
            },
            theme: theme,
            columns: definition.Columns
        });

        //#region Event Subscriptions

        $(gridControlID).one('rowdoubleclick', doubleClick);
        $(gridControlID).on('rowselect', gridRowSelect);
        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

        //#endregion
    } catch (e) {
    }
}

function FusionItemsGrid(controlID, contextList, permissions, fusionTypeID, fusionID,initialData) {

    var _innercontrolID = controlID + '_inner'; //stores original value.
    var breadcrumbControlID = controlID + "_breadcrumb";
    controlID = '#' + controlID;

    $(controlID).html('<header>Items</header><div><ul class="hierarchy" id="' + breadcrumbControlID + '"><li id="H_FusionAttributeType_0">Root</li></ul></div><div class="form-instructions">Double-click on an item in the list below.</div><div id="' + _innercontrolID + '"></div>');

    breadcrumbControlID = '#' + breadcrumbControlID;

    var typeDefinition = {
        Fields: [
            { name: 'ID', type: 'number' },
            { name: 'Name', type: 'string' },
            { name: 'IsLeaf', type: 'bool' },
            { name: 'ParentFusionAttributeID', type: 'number' }
        ],
        Columns: [
            { text: 'Name', dataField: 'Name' }            
        ]
    };

    //#region Event Subscriptions

    function fusionItemSelected(data) {        
        var liID = 'H_' + data.type + '_' + data.id;

        $(breadcrumbControlID).append('<li class="separator">/</li><li id="' + liID + '">' + data.title + '</li>');
        $('#' + liID).data('type', data.type);
        $('#' + liID).data('id', data.id);
        $('#' + liID).data('definition', data.definition);
        $('#' + liID).data('uri', data.uri);

        $(breadcrumbControlID).children().removeClass('clickable');
        $('#' + liID).prevAll('li').not('.separator').addClass('clickable');

        $('#' + liID).on('click', function () {
            $(this).nextAll().remove();
            _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, $(this).data('definition'), $(this).data('uri'));
        });
        
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, data.definition, data.uri);
    }

    function buildCurrentItemBreadcrumb(data) {        
        //need to go from selected item to root and generate the breadcrumb
        $.getJSON('/api/fusion/selectedbreadcrumb/' + data.SelectedID, function (path) {
            path.forEach(function (item) {            
                buildBreadcrumbLink('FusionAttributeType', item.typeid, item.typename, item.typeid, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttribute&parentID=' + item.parentID + '&parentFusionAttributeTypeID=' + item.typeid,false);

                buildBreadcrumbLink('FusionAttribute', item.id, item.name, item.typeid, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType&parentID=' + item.typeid + '&parentFusionAttributeID=' + item.id, true);
            });
        });
    }

    function buildBreadcrumbLink(type, id, name,typeid,uri,passdefault) {
        var liID = 'H_' + type + '_' + id;
        $(breadcrumbControlID).append('<li class="separator">/</li><li id="' + liID + '">' + name + '</li>');
        $('#' + liID).data('type', type);
        $('#' + liID).data('id', id);
        $('#' + liID).data('uri', uri);

        if (passdefault) {
            var def = typeDefinition;
            def.id = typeid;
            def.title = name;
            def.type = type;
            $('#' + liID).data('definition', def);
        }

        $('#' + liID).prevAll('li').not('.separator').addClass('clickable');
        $('#' + liID).on('click', function () {
            $(this).nextAll().remove();
            _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, passdefault ? $(this).data('definition') : null, $(this).data('uri'), typeid);
        });
    }

    function rootLiClick() {
        $(this).nextAll().remove();
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType');
    }

    function unsubscribe(data) {
        $('#H_FusionAttributeType_0').off('click', rootLiClick);
        amplify.unsubscribe('FusionItemSelected', fusionItemSelected);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $('#H_FusionAttributeType_0').on('click', rootLiClick);
    amplify.subscribe('FusionItemSelected', fusionItemSelected);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    
    //#endregion
    if (initialData != null) {
        // build the breadcrumb for current item
        buildCurrentItemBreadcrumb(initialData);

        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType&parentID=' + initialData.FusionAttributeTypeID + '&parentFusionAttributeID=' + initialData.ID);
    }
    else
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType');
}

function FusionAttributePromotionRulesGrid(controlID, contextList, permissions, typeID, fusionID) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Promotion Rules</header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var srcPromotionRulesGrid = {
        datatype: 'json',
        url: '/services/fusion/' + typeID + '/configurations/' + fusionID + '/promotionrules',
        datafields:
        [
            { name: 'ID' },
            { name: 'ObjectName' },
            { name: 'ObjectType' },
            { name: 'ParentName' },
            { name: 'ParentObjectType' },
            { name: 'PromotionName' },
            { name: 'PromotionObjectType' },
            { name: 'PromotionParentName' },
            { name: 'PromotionParentObjectType' },
            { name: 'Enabled' }
        ]
    };

    var adapterPromotionRulesGrid = new $.jqx.dataAdapter(srcPromotionRulesGrid);

    $(gridControlID).jqxGrid({
        altrows: true,
        width: grid_width,
        autoheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: true,
        pageable: true,
        pagesizeoptions: ['10', '20', '50'],
        pagesize: 20,
        source: adapterPromotionRulesGrid,
        theme: list_theme,
        columns: [
            { text: 'Name', dataField: 'ObjectName' },
            { text: 'Parent', dataField: 'ParentName' },
            { text: 'Promote To', dataField: 'PromotionName' },
            { text: 'Parent to Promote To', dataField: 'PromotionParentName' },
            {
                text: '',
                dataField: 'ID',
                width: 80,
                filterable: false,
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var url = '/form/domains/' + typeID + '/' + fusionID + '/items/{0}/';
                    var tools = [
                        { icon: 'pencil', urlprefix: url + 'edit' },
                        { icon: 'trash-o', urlprefix: url + 'delete' }
                    ];

                    return renderToolsHtml(value, tools, contextList.DomainItem);
                }
            }
        ]
    });

    //#endregion

    //#region Event Subscriptions

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.DomainItem:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("FusionAttributePromotionRulesGrid : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        srcDomainItemsGrid = null;
        adapterDomainItemsGrid = null;

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function FusionRelationshipChartTile(controlID, type, id, parentAttributeID) {
    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<header>Relationships</header><table style="width: 100%"><tr><td><div id="' + chartControlID + '" style="margin: auto; width: 95%; height: 300px"></div></td></tr></table>')
    chartControlID = '#' + chartControlID;

    $.ajax({
        url: '/fusion/RelationshipAggregates?type=' + type + '&id=' + id + '&parentAttributeID=' + (parentAttributeID ? parentAttributeID : 0),
        method: 'GET'
    })
    .done(function (data, status, xhr) {
        if (data.length > 0) {
            var source = {
                datatype: 'json',
                localdata: data,//url: '/fusion/RelationshipAggregates?id=' + id,
                datafields:
                [
                    { name: 'ObjectType' },
                    { name: 'ObjectID' },
                    { name: 'TypeID' },
                    { name: 'Type' },
                    { name: 'TypeName' },
                    { name: 'IconBackColor' },
                    { name: 'IconForeColor' },
                    { name: 'IconText' },
                    { name: 'Count' }
                ]
            };

            var adapter = new $.jqx.dataAdapter(source);

            $(chartControlID).jqxChart({
                title: "",
                description: "",
                enableAnimations: true,
                showLegend: true,
                showBorderLine: false,
                //legendLayout: { left: (tileWidth / 2) + 10, top: 50, width: 175, height: 200, flow: 'vertical' }, //legendLayout: { left: 250, top: 75, width: 175, height: 200, flow: 'vertical' },
                //padding: { left: 10, right: (tileWidth / 2) - 10, top: 0, bottom: 10 },//padding: { left: 10, right: 150, top: 10, bottom: 10 },
                source: adapter,
                colorScheme: chartDefaultTheme,
                seriesGroups: [{
                    type: 'donut',
                    useGradientColors: false,
                    series: [
                        {
                            showLabels: true,
                            useGradient: false,
                            dataField: 'Count',
                            displayText: 'TypeName',
                            labelRadius: 80,
                            initialAngle: 15,
                            radius: 100,
                            innerRadius: 50,
                            centerOffset: 0
                        }
                    ],
                    click: function (e) {
                        var data = adapter.records[e.elementIndex];
                        var url = '/fusion/RelationshipAggregatesOverlay?type=' + type + '&id=' + id + '&targetType=' + data.Type + '&targetID=' + data.TypeID + '&parentAttributeID=' + (parentAttributeID ? parentAttributeID : 0);
                        openTileOverlay(url);
                    }
                }]
            });

            $(document).on('resize', function () {
                $(chartControlID).jqxChart('refresh');
            });
        }
        else {
            $(chartControlID).text('No information available.');
        }
    })
    .fail(function (xhr, status, error) {
        $(chartControlID).text(error);
    });
}

function FusionAttributeDetailTile(controlID, type, id) {
    var detailControlID = controlID + "_fus_det";    
    controlID = '#' + controlID;    
    $(controlID).hide();
    
    $.ajax({
        url: '/fusion/details/' + type + '/' + id,
        method: 'GET'
    })
    .done(function (data, status, xhr) {
        if (data.Fields.length > 0) {            
            $(controlID).html('<header>Details</header><div id="' + detailControlID + '" style="margin: auto; width: 95%;" class="form"></div>');
            detailControlID = '#' + detailControlID;
            var itemCnt = 0;
            var ended = false;

            var row = $("<div class='row'>");
            $(detailControlID).append(row);

            var col = $("<div class='col l6 m6'>");
            $(row).append(col);

            col.append("<div class='FieldName FieldDisplayName'>Name</div>");
            col.append("<div class='FieldContent wrapword'>" + data.Name + "</div>");

            col = $("<div class='col l6 m6'>");
            $(row).append(col);
            col.append("<div class='FieldName FieldDisplayName'>Path</div>");
            col.append("<div class='FieldContent wrapword'>" + data.TextPath + "</div>");

            row = $("<div class='row'>");
            $(detailControlID).append(row);

            data.Fields.forEach(function (item) {
                if (itemCnt % 2 == 0 && itemCnt > 0) {                    
                    row = $("<div class='row'>");
                    $(detailControlID).append(row);
                }                
                col = $("<div class='col l6 m6'>");
                $(row).append(col);
                col.append("<div class='FieldName FieldDisplayName'>" + item.Name + "</div>");
                col.append("<div class='FieldContent wrapword'>" + item.Value + "</div>");
            });

            $(controlID).show();
        }
    });
}

function FusionTypesGrid(controlID, contextList, permissions) {

    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + gridControlID + '"></div>'); //<header>Types</header>
    gridControlID = '#' + gridControlID;

    var source = {
        datatype: 'json',
        type: 'get',
        url: '/services/fusion?$orderby=Name',
        datafields: [
            { name: 'ID', type: 'number' },
            { name: 'Name', type: 'string' },
            { name: 'Description', type: 'string' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesize: 10,
            pageable: true,
            pagermode: 'simple',
            columnsresize: true,
            source: adapter,
            theme: theme,
            ready: function () {
                var rowCount = $(gridControlID).jqxGrid('getdisplayrows').length;
                if (rowCount > 0) {
                    $(gridControlID).jqxGrid('selectrow', 0);
                }
            },
            columns: [
                { text: 'Name', dataField: 'Name' },
                {
                    text: '',
                    dataField: 'ID',
                    width: 80,
                    filterable: false,
                    sortable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditFusionType?id={0}' });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFusionType?id={0}' });
                        }
                        return renderToolsHtml(value, tools, contextList.FusionType);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function gridRowSelect(event) {
        try {
            amplify.publish('FusionTypeSelected', event.args.row);
        } catch (e) {
            logError("Parts.js : FusionConfigurationsGrid", e);
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FusionType:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FusionConfigurationsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('rowselect', gridRowSelect);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on('rowselect', gridRowSelect);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function FusionConfigurationsGrid(controlID, contextList, permissions, type, id) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    $(controlID).html('<header>Configurations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    if (permissions.HasPermission("Root", "Update")) {
        TileTools(toolsControlID, [
            { icon: 'plus', uri: '/form/AddFusion?typeID=' + id, context: contextList.Fusion, title: 'Add configuration' }
        ]);
    }

    $.getJSON('/api/' + type + '/' + id + '/grid/definition', function (definition) {

        var source = {
            datatype: 'json',
            type: 'get',
            url: '/services/fusion/' + id + '/configurations',
            datafields: definition.Fields
        };

        var adapter = new $.jqx.dataAdapter(source);

        definition.Columns.push({
            text: '',
            dataField: 'ID',
            width: '160px',
            sortable: false,
            filterable: false,
            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                if (value != 0) {
                    var tools = [
                        { isitemlink: true, urlprefix: '#/fusion/' + definition.ID + '/{0}' }
                    ];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditFusion?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteFusion?id={0}' });
                    }
                    tools.push({ icon: 'filter', urlprefix: '/overlays/FusionConfigurationFilters?fusionTypeID=' + id + '&fusionID={0}', title: 'View/modify synchronization filters' });

                    return renderToolsHtml(value, tools, contextList.Fusion);
                }
                else {
                    return "";
                }
            }
        });

        try {
            $(gridControlID).jqxGrid({
                altrows: true,
                width: grid_width,
                autoheight: true,
                sortable: true,
                filterable: true,
                showfilterrow: true,
                pagesizeoptions: ['10', '20', '50'],
                pagesize: 20,
                pageable: true,
                columnsresize: true,
                source: adapter,
                theme: theme,
                columns: definition.Columns
            });
        } catch (e) {
        }

        //#region Event Subscriptions

        function gridRowDoubleClick(event) {
            try {
                var args = event.args;
                var row = args.rowindex;
                var data = $(gridControlID).jqxGrid('getrowdata', row);
                location.assign('#/fusion/' + definition.ID + '/' + data.ID);
            } catch (e) {
                logError("Parts.js : FusionConfigurationsGrid", e);
            }
        }

        function pageResized() {
            $(gridControlID).jqxGrid('refresh');
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Fusion:
                        $(gridControlID).jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) {
                logError("Parts.js : FusionConfigurationsGrid", e);
            }
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;

            $(gridControlID).off('rowdoubleclick', gridRowDoubleClick);
            amplify.unsubscribe("PageResized", pageResized);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        $(gridControlID).on('rowdoubleclick', gridRowDoubleClick);
        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe("SaveAction", saveAction);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

        //#endregion
    });
}

function GroupMembersGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var html = "";
    html += '<header>Members<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>';
    $(controlID).html(html);
    gridControlID = '#' + gridControlID;
    toolsControlID = '#' + toolsControlID;

    var source = {
        datatype: 'json',
        type: 'get',
        url: '/api/groups/' + id + '/resources',
        datafields:
        [
            { name: 'ResourceID', type: 'number' },
            { name: 'FirstName', type: 'string' },
            { name: 'LastName', type: 'string' },
            { name: 'Owner', type: 'string' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    var tools = [];
    if (permissions.HasPermission("Root", "Update")) {
        tools.push({ icon: 'plus', uri: '/form/AddGroupUser?id=' + id, context: contextList.ResourceGroup, title: 'Add member' });
    }
    TileTools(toolsControlID, tools);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            virtualmode: false,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            filterable: true,
            showfilterrow: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "LastName", text: "Last Name" },
                { datafield: "FirstName", text: "First Name" },
                { datafield: "Owner", text: "Owner?", width: 125 },
                {
                    datafield: "ResourceID",
                    text: "",
                    width: 80,
                    sortable: false,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { isitemlink: true, urlprefix: '#/resources/{0}', type: 'Group', context: 'Preview' }
                        ];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteGroupUser?groupID=' + id + '&resourceID={0}' });
                        }

                        return renderToolsHtml(value, tools, contextList.ResourceGroup, data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.ResourceGroup:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : GroupMembersGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function IntersectTypeRolesGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Available Roles<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/relationshiptypes/' + id + '/roles',
            datafields:
            [
                { name: 'ID' },
                { name: 'IntersectTypeID' },
                { name: 'Name' },
                { name: 'Side1Label' },
                { name: 'Side2Label' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddIntersectTypeRole?intersectTypeID=' + id, context: contextList.IntersectTypeRole, title: 'Add role' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "Name", text: "Name", width: '40%' },
                { datafield: "Side1Label", text: "Side 1 Label", width: '25%' },
                { datafield: "Side2Label", text: "Side 2 Label", width: '25%' },
                {
                    text: '', dataField: 'ID', width: '10%', filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditIntersectTypeRole?id={0}&intersectTypeID=' + data.IntersectTypeID });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteIntersectTypeRole?id={0}&intersectTypeID=' + data.IntersectTypeID });
                        }

                        return renderToolsHtml(value, tools, contextList.IntersectTypeRole);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function commandExecuted(command) {
        if (command == "FieldMove") {
            $(gridControlID).jqxGrid('updatebounddata');
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("CommandExecuted", commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe('CommandExecuted', commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function LookupTypeItemsGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Event Methods

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.FieldType:
                    unsubscribe();
                    $(toolsControlID).html('');
                    loadGridConfiguration();
                    break;
                case contextList.Lookup:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : LookupTypeItemsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    //#endregion

    function loadGridConfiguration() {
        $.getJSON('/api/LookupType/' + id + '/grid/definition', function (gridinfo) {

            source = {
                datatype: 'json',
                url: '/resources/lookups/' + id + '/items.json',
                datafields: gridinfo.Fields,
            };

            adapter = new $.jqx.dataAdapter(source);

            if ((gridinfo.FieldsCount > 0) && permissions.HasPermission("Root", "Update")) {
                TileTools(toolsControlID, [
                    { icon: 'plus', uri: '/form/AddLookup?id=' + id, context: contextList.Lookup, title: 'Add item' }
                ]);
            }

            gridinfo.Columns.push({
                text: '', dataField: 'ID', width: '10%', filterable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    if (permissions.HasPermission("Root", "Update")) {
                        tools.push({ icon: 'pencil', urlprefix: '/form/EditLookup?id={0}' });
                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteLookup?id={0}' });
                    }

                    return renderToolsHtml(value, tools, contextList.Lookup);
                }
            });

            try {
                $(gridControlID).jqxGrid({
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    altrows: true,
                    source: adapter,
                    theme: list_theme,
                    columns: gridinfo.Columns
                });
            } catch (e) { }

            //#region Event Subscriptions
            amplify.subscribe("PageResized", pageResized);
            amplify.subscribe("SaveAction", saveAction);
            amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            //#endregion
        });
    }

    //#region Grid

    try {
        $(controlID).html('<header>Items<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        loadGridConfiguration();

    } catch (e) {
        console.log(e);
    }

    //#endregion
}

function ObjectStatisticsTile(controlID, type, id) {
    var source = $("#objectStatisticsTile").html();
    var template = Handlebars.compile(source);

    controlID = '#' + controlID;

    $.getJSON(
        '/api/' + type + '/' + id + '/object/statistics',
        function (data) {
            $(controlID).html(
                template(data)
            );
            if ($(controlID).find('.ScoreKpi').length) {
                drawKpi($(controlID).find('.ScoreKpi'), 'Governance score', data.Score, 100 - data.Score, true);
            }
        }
    );
}

function ResourceStatisticsTile(controlID, type, id) {
    var source = $("#resourceStatisticsTile").html();
    var template = Handlebars.compile(source);

    controlID = '#' + controlID;

    $.getJSON(
        '/api/' + type + '/' + id + '/object/statistics',
        function (data) {
            $(controlID).html(
                template(data)
            );
            if ($(controlID).find('.ScoreKpi').length) {
                drawKpi($(controlID).find('.ScoreKpi'), 'Governance score', data.Score, 100 - data.Score, true);
            }
        }
    );
}

function PeopleResponsibilityTile(controlID, contextList, permissions, type, id, title, showHidden) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    try {
        var html = "";
        if (title == '' || !title) title = 'People Responsibilities';
        html = '<header>' + title + '<div id="' + toolsControlID + '"></div></header>';
        html += '<div id="' + gridControlID + '"></div>';
        $(controlID).html(html);
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        if (!showHidden) showHidden = false;

        if (permissions.HasPermission("Governance", "Create")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddResponsibility?type=" + type + "&id=" + id, context: contextList.Responsibility, title: 'Add responsibility' }
            ]);
        }

        source = {
            datatype: 'json',
            url: '/api/' + type + '/' + id + '/ownership?showHidden=' + showHidden,
            datafields:
            [
                { name: 'ResponsibilityID' },
                { name: 'AssigningItemType' },
                { name: 'AssigningItemID' },
                { name: 'ResponsibleObjectType' },
                { name: 'ResponsibleObjectID' },
                { name: 'ResponsibleObjectName' },
                { name: 'PrimaryOwnerResourceID' },
                { name: 'PrimaryOwnerResourceName' },
                { name: 'PrimaryOwnerResourceUrl' },
                { name: 'ObjectType' },
                { name: 'ObjectID' },
                { name: 'Role' },
                { name: 'ResponsibleObjectUrl' },
                { name: 'ContextItems' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            pageable: true,
            selectionmode: 'none',
            autorowheight: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "Role", text: "Role", width: '20%' },
                { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "ResponsibleObjectName", text: "Resource", width: '20%',
                  cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return previewLinkRenderer(data.ResponsibleObjectType, data.ResponsibleObjectID, data.ResponsibleObjectUrl, data.ResponsibleObjectName);
                    }
                },
                { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "PrimaryOwnerResourceName", text: "Group Owner", width: '20%',
                  cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        if (data.PrimaryOwnerResourceName && data.PrimaryOwnerResourceName != '')
                            return previewLinkRenderer('Resource', data.PrimaryOwnerResourceID, data.PrimaryOwnerResourceUrl, data.PrimaryOwnerResourceName);
                        else
                            return '';
                    }
                },
                { datafield: "ContextItems", text: "Context" },
                { datafield: "ResponsibilityID", text: "", width: '80px', filterable: false, sortable: false, 
                  cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID) {
                            if (permissions.HasPermission("Governance", "Update")) {
                                tools.push({ icon: 'pencil', urlprefix: '/form/EditResponsibility?id={0}' });
                            }
                            if (permissions.HasPermission("Governance", "Delete")) {
                                tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteResponsibility?id={0}' });
                            }
                        }

                        return renderToolsHtml(value, tools, contextList.Responsibility, data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#endregion

    //#region Event Subscriptions

    function gridBindingComplete(event) {
        try {
            $(gridControlID).jqxGrid('sortby', 'Role', 'asc');
        } catch (e) { }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Artifact:
                case contextList.DomainList:
                case contextList.Responsibility:
                case contextList.PeopleResponsibility:
                case contextList.Taxonomy:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        $(gridControlID).off('bindingcomplete', gridBindingComplete)
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(gridControlID).on("bindingcomplete", gridBindingComplete);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function PolicyStatusKpi(controlID, contextList, permissions, id) {

    var calendarControlID = controlID + "_calendar";
    var graphicControlID = controlID + "_graphic";
    controlID = '#' + controlID;

    var date = '10-20-2015';

    //#region Grid

    var calendarChange = function (event) {
        var dt = moment(event.args.date);
        date = dt.toISOString();
        loadStatus();
    }

    var loadStatus = function () {
        $.getJSON('/monitor/PolicyStatusForDate', { id: id, date: date }, function (data) {
            var html = '';

            if (data.status) {
                html = "<i class='fa fa-thumbs-o-up' style='font-size: 40px; color: green' title='All good'></i>";
            }
            else {
                html = "<i class='fa fa-thumbs-o-down' style='font-size: 40px; color: red' title='Not so good'></i>";
            }

            $(graphicControlID).html(html);
        });
    }

    try {
        $(controlID).html('<header>Health Status</header><div style="padding: 10px"><table><tr style="vertical-align: middle"><td><div id="' + calendarControlID + '"></div></td><td><div id="' + graphicControlID + '"></div></td></tr></table></div>');
        calendarControlID = '#' + calendarControlID;
        graphicControlID = '#' + graphicControlID;

        $(calendarControlID).jqxDateTimeInput({ width: '220px', height: '25px', theme: theme, formatString: "MM-dd-yy" });
        loadStatus();
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function unsubscribe(data) {
        $(calendarControlID).off('change', calendarChange);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(calendarControlID).on('change', calendarChange);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function PolicyTypeLevelsGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Levels<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/PolicyType/' + id + '/levels',
            datafields:
            [
                { name: 'PolicyTypeID' },
                { name: 'Name' },
                { name: 'Level' },
                { name: 'Description' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddPolicyTypeLevel?id=" + id, context: contextList.PolicyTypeLevel, title: 'Add level' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "Level", text: "Level", width: '10%' },
                { datafield: "Name", text: "Name", width: '30%' },
                { datafield: "Description", text: "Description" },
                {
                    text: '',
                    dataField: 'PolicyTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditPolicyTypeLevel?id=' + data.PolicyTypeID + '&level=' + data.Level });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeletePolicyTypeLevel?id=' + data.PolicyTypeID + '&level=' + data.Level });
                        }

                        return renderToolsHtml(value, tools, contextList.PolicyTypeLevel);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.PolicyTypeLevel:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : PolicyTypeLevelsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function RelatedArtifactsGrid(controlID, permissions, typeName, typeID, id) {

    controlID = '#' + controlID;
    var gridID = '#RelatedArtifactsTileGrid';
    var textID = '#RelatedArtifactsTileText';

    var source = $("#relatedArtifactsTileTmpl").html();
    var template = Handlebars.compile(source);
    $(controlID).html(template({ Title: typeName }));

    //#region Grid

    var source = {
        datatype: 'json',
        type: 'get',
        datafields: [
            { name: "ID" },
            { name: 'Name' },
            { name: 'Url' }
        ],
        url: '/queries/RelatedArtifacts?artifactID=' + id
    };

    var adapter = new $.jqx.dataAdapter(source);

    $(gridID).jqxGrid({
        source: adapter,
        width: grid_width,
        pagesizeoptions: ['5', '10', '20'],
        pagesize: 5,
        autoheight: true,
        sortable: true,
        altrows: true,
        showheader: false,
        showfilterrow: false,
        filterable: false,
        pageable: false,
        theme: list_theme,
        columns: [
            {
                datafield: "Name",
                text: "Name",
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return previewLinkRenderer('Artifact', data.ID, data.Url, data.Name);
                }
            },
            {
                datafield: "ID",
                text: "",
                width: '40px',
                sortable: false,
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    var tools = [];

                    if (permissions.HasPermission("Relationship", "Delete")) {
                        tools.push({ icon: 'trash-o', urlprefix: '/form/RelatedArtifact/' + id + '/' + data.ID, context: 'action', method: 'DELETE' });
                        tools.push();
                        tools.push();
                        tools.push();
                    }
                    return renderToolsHtml(value, tools, 'action');
                }
            }
        ]
    });

    //#endregion

    //#region Textbox

    var relatedArtifactsDataSource = {
        datatype: "json",
        datafields: [
            { name: 'ID' },
            { name: 'Name' }
        ],
        url: '/queries/RelatedArtifactOptions?typeID=' + typeID + '&artifactID=' + id
    };
    var relatedArtifactsDataAdapter = new $.jqx.dataAdapter(relatedArtifactsDataSource);

    $(textID).jqxInput({ disabled: !permissions.HasPermission("Relationship", "Create"), source: relatedArtifactsDataAdapter, placeHolder: "Search for a " + typeName.toLowerCase() + " to relate...", displayMember: "Name", valueMember: "ID", width: '100%', height: 25 });

    //#endregion

    //#region Events

    function commandExecuted(command) {
        if (command == "RelatedArtifactAdded") {
            $(gridID).jqxGrid('updatebounddata');
            $(textID).jqxInput('val', '');
        }
        if (command == "RelatedArtifactDeleted") {
            $(gridID).jqxGrid('updatebounddata');
        }
    }

    function pageResized() {
        $(gridID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case 'RelatedArtifacts':
                    $(gridID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : RelatedArtifactsGrid : SaveAction", e);
        }
    }

    function textSelect(event){
        if (event.args) {
            var item = event.args.item;
            if (item) {
                amplify.publish("ToolAction", { context: 'action', customdata: { method: 'POST' }, uri: '/form/RelatedArtifact/' + id + '/' + item.value });
            }
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe('CommandExecuted', commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        $(textID).off('select', textSelect);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("CommandExecuted", commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    $(textID).on('select', textSelect);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function RelationshipAggregatesTile(controlID, type, id, permissions) {

    var chartsExist;
    var parent;
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    try {
        controlID = '#' + controlID;

        parent = $(controlID);

        var html = "";
        html += "<header>Relationships<div id='" + toolsControlID + "'></div></header>";
        html += "<div style='margin-left: 5px' id='" + gridControlID + "'></div>";
        //html += "<table style='width: 100%'>";//"<div class='row'>";
        //html += "<tr>";
        //html += "<td style='width: 50%'><div id='AggregateTileChart1' class='col s6' style='margin: auto; width: 100%'></div></td>";
        //html += "<td style='width: 50%'><div id='AggregateTileChart2' class='col s6' style='margin: auto; width: 100%'></div></td>";
        //html += "</tr>";
        //html += "<tr>";
        //html += "<td colspan='2' style='margin: auto; width: 100%'><div id='AggregateTileChart3' class='col s12' style='width: 60%'></div></td>";
        //html += "</tr>";
        //html += "</table>";//"</div>";

        parent.html(html);

        toolsControlID = '#' + toolsControlID;
        gridControlID = '#' + gridControlID;

        if (permissions.HasPermission("Relationship", "Update")) {
            TileTools(toolsControlID, [
                    { icon: 'pencil', uri: '/relations/RelationOverlay?type=' + type + '&id=' + id, context: contextList.Intersect, title: 'Manage Relationships' }
            ]);
        }

        var clickRelationshipKpiTitle = function () {
            var kpi = $(this);
            var critical = kpi.data("critical");
            var clickBaseUri = '/Relations/AggregateRelationOverlay?criticalOnly=' + (critical ? 'true' : 'false') + '&';
            var url = clickBaseUri + 'type=' + type + '&id=' + id + '&targetType=' + kpi.data("t") + '&targetID=' + kpi.data("i") + '&intersectTypeID=' + kpi.data("intersecttypeid");
            openTileOverlay(url);
        }

        $.ajax({
            url: '/tiles/RelationshipAggregates',
            method: 'GET',
            data: {
                type: type,
                id: id
            },
            dataType: 'json'
        }).fail(function (xhr, status, error) {

        }).done(function (data, status, xhr) {

            var groups = [];
            // Load unique group names
            $.each(data, function () {
                var groupName = this.Group;
                if ($.inArray(groupName, groups) == -1) {
                    groups.push(groupName);
                }
            });
            var collectionHtml = '';
            $.each(groups, function () {
                var selectedGroupName = this;
                var nodes = [];
                var colors = [];
                $.each(data, function () {
                    if (this.Group == selectedGroupName) {
                        nodes.push(this);
                        colors.push(this.IconBackColor);
                    }
                });
                var gridHtml = '<h5>' + nodes[0].GroupName + '</h5><div class="kpi-grid">';
                $.each(nodes, function () {
                    gridHtml += '<div class="kpi-grid-item" style="background-color: ' + this.IconBackColor + '; color: ' + this.IconForeColor + '" data-critical="' + (this.Group == "2" && this.Type == "ArtifactType") + '" data-t="' + this.Type + '" data-i="' + this.TypeID + '"data-intersecttypeid="' + this.IntersectTypeID + '">' +
                        '<div class="icon">' + this.IconText + '</div>' +
                        '<div class="value">' + this.Count + '</div>' +
                        '<div class="title">' + this.TypeName + '</div>' +
                        '</div>';
                });
                gridHtml += '</div>';
                collectionHtml += gridHtml;
                


                //    var cht = $('#AggregateTileChart' + this);
            //    if (nodes.length <= 0) {
            //        cht.css('height', '40px');
            //        cht.html('No data to display');
            //    }
            //    else {
            //        var groupName = nodes[0].GroupName;
            //        var critical = (nodes[0].Group == "2" && nodes[0].Type == "ArtifactType");
            //        cht.css('height', '300px');
            //        cht.jqxChart({
            //            source: nodes,
            //            title: groupName,
            //            description: '',
            //            enableAnimations: false,
            //            showLegend: true,
            //            showBorderLine: false,
            //            legendLayout : { flow: 'horizontal' },
            //            //padding: { left: 5, top: 5, right: 5, bottom: 5 },
            //            //titlePadding: { left: 0, top: 0, right: 0, bottom: 10 },
            //            seriesGroups: [
            //                {
            //                    useGradientColors: false,
            //                    type: 'pie',
            //                    showLegend: true,
            //                    enableSeriesToggle: true,
            //                    series: [
            //                        {
            //                            dataField: 'Count',
            //                            displayText: 'TypeName',
            //                            showLabels: true,
            //                            //labelRadius: 125,
            //                            labelLinesEnabled: true,
            //                            labelLinesAngles: true,
            //                            labelsAutoRotate: false
            //                            //initialAngle: 0,
            //                            //radius: 100,
            //                            //minAngle: 0,
            //                            //maxAngle: 180,
            //                            //centerOffset: 0,
            //                            //offsetY: 180,
            //                            //formatFunction: function (value, itemIndex, serie, group) {
            //                            //    return value;
            //                            //}
            //                        }
            //                    ],
            //                    click: function (e) {
            //                        var clickBaseUri = '/Relations/AggregateRelationOverlay?criticalOnly=' + (critical ? 'true' : 'false') + '&';
            //                        var data = nodes[e.elementIndex];                                    
            //                        var url = clickBaseUri + 'type=' + type + '&id=' + id + '&targetType=' + data.Type + '&targetID=' + data.TypeID + '&intersectTypeID=' + data.IntersectTypeID;
            //                        openTileOverlay(url);
            //                    }
            //                }
            //            ]
            //        });
            //        cht.jqxChart('addColorScheme', 'myScheme', colors);
            //        cht.jqxChart('colorScheme', 'myScheme');
            //        cht.jqxChart('refresh');

            //        $(document).on('resize', function () {
            //            cht.jqxChart('refresh');
            //        });
            //    }
            });
            $(gridControlID).html(collectionHtml);
            $('.kpi-grid').isotope({
                // options
                itemSelector: '.kpi-grid-item',
                layoutMode: 'fitRows',
                fitRows: {
                    gutter: 10
                }
            });
            $('.kpi-grid-item').on('click', clickRelationshipKpiTitle);
        });

    } catch (e) {
        console.log(e);
    }
}

function Relationship_SimpleHierarchyTile(controlID, contextList, permissions, type, id) {
    var headerControlID = controlID + "Header";
    var treeControlID = controlID + "SimpleHierarchyTree";

    var headerTitle = "";

    //#region Build HTML

    var html = '';
    html += '<header>' + headerTitle + '</header>';
    html += '<div class="row">';
    html += '<div class="col s12">';
    html += '<div id="' + treeControlID + '"></div>';
    html += '</div>';
    html += '</div>';

    //#endregion

    //#region Set proper jquery prefix on controls

    controlID = '#' + controlID;
    headerControlID = '#' + headerControlID;
    treeControlID = '#' + treeControlID;

    //#endregion

    //#region Clean up previous control logic before re-creating

    $(controlID).html('');
    $(controlID).html(html);

    //#endregion

    var loadHierarchy = function (node, html) {
        try {
            if (node) {

                html = "<ul>";
                html += "<li item-expanded='true'>";
                
                html += (node.ObjectType == type && node.ObjectID == id) ? "" : "<a data-context='Preview' data-type='" + node.ObjectType + "' data-id='" + node.ObjectID + "' href='" + node.ObjectUrl + "'>";
                html += (node.ObjectType == type && node.ObjectID == id) ? "<b>" + node.ObjectName + "</b>" : node.ObjectName;
                html += (node.ObjectType == type && node.ObjectID == id) ? "" : "</a>";

                if (node.Items.length > 0) {
                    $.each(node.Items, function () {
                        html += loadHierarchy(this);
                    });
                }

                html += "</li>";
                html += "</ul>";
            }
        } catch (e) {
            logError("Relationship_SimpleHierarchyTile : loadHierarchy", e);
        }

        return html;
    }

    $.getJSON('/relations/SimpleHierarchies', { type: type, id: id }, function (data) {
        if (data) {
            // Loop through each top-level flow hierarchy.  There could be multiple.
            $.each(data, function () {
                $(treeControlID).append("<h4>" + this.FlowTypeName + "</h4>");

                var tree = $("<div style='border: none !important;'></div>");
                tree.append(loadHierarchy(this, ""));
                $(treeControlID).append(tree);
                //tree.jqxTree({ theme: theme });
            });
        }
    });

    //#region Event Subscriptions

    function unsubscribe(data) {

        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function RelationshipTypeTreeTile(controlID, permissions, type, id) {
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    var title = 'Relationship Types';

    try {
        $(controlID).html('<header>' + title + '<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        var source = {
            dataType: "json",
            dataFields: [
                { name: 'IntersectTypeID', type: 'number' },
                { name: 'TargetObjectType', type: 'string' },
                { name: 'TargetObjectID', type: 'number' },
                { name: 'TextPath', type: 'string' },
                { name: 'Level', type: 'number' },
                { name: 'relationships', type: 'array' },
                { name: 'predicates', type: 'array' }
                //{ name: 'expanded', type: 'bool' }
            ],
            hierarchy:
            {
                root: 'relationships'
            },
            //id: 'IntersectTypeID',
            url: '/relations/' + type + '/' + id + '/RelationshipTypeTree.json'
        };
        var dataAdapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: '/form/AddIntersectType?type=' + type + '&id=' + id, context: contextList.IntersectType, title: 'Add relationship type' }
            ]);
        }

        $(gridControlID).jqxTreeGrid({
            width: grid_width,
            pageable: true,
            pagerMode: 'advanced',
            pageSizeMode: 'root',
            pageSize: 10,
            pageSizeOptions: ['5', '10', '25'],
            theme: theme,
            source: dataAdapter,
            sortable: true,
            columns: [
                {
                    text: 'Name',
                    dataField: 'TextPath',
                    cellsRenderer: function (row, column, value, data) {
                        var html = "";
                        html += ((data.Level == 1) ? "<b>" : "") + data.TextPath + ((data.Level == 1) ? "</b>" : "");
                        return html;
                    }
                },
                {
                    text: 'Predicates', dataField: 'predicates', width: '40%', filterable: false,
                    cellsRenderer: function (row, column, value, data) {
                        var html = "";
                        if (data.predicates) {
                            html += "";//"<ul>";
                            $.each(data.predicates, function () {
                                html += ((html !== "") ? ", " : "") + this.Name;//"<li>" + this.Name + "</li>";
                            });
                            //html += "</ul>";
                        }
                        return html;
                    }
                },
                {
                    text: '', dataField: 'IntersectTypeID', width: '160px', filterable: false,
                    cellsRenderer: function (row, column, value, data) {
                        var tools = [];

                        if (permissions.HasPermission("Root", "Create")) {
                            tools.push({ icon: 'plus', urlprefix: '/form/AddIntersectType?type=IntersectType&id={0}', title: 'Add fusion relationship type' });
                        }
                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditPredicateAllocation?id={0}', title: 'Edit predicates' });
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditIntersectType?id={0}', title: 'Edit relationship type' });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteIntersectType?id={0}', title: 'Remove relationship type' });
                        }

                        return renderToolsHtml(value, tools, contextList.ArtifactType, data);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function commandExecuted(command) {
        //try {
        //    if (command == "FieldMove") {
        //        $(gridControlID).jqxTreeGrid('updateBoundData');
        //    }
        //} catch (e) {
        //    logError("Parts.js : FieldsGrid", e);
        //}
    }

    function pageResized() {
        $(gridControlID).jqxTreeGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.IntersectType:
                    $(gridControlID).jqxTreeGrid('updateBoundData');
                    break;
            }
        } catch (e) {
            logError("Parts.js : FieldsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("CommandExecuted", commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe('CommandExecuted', commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function StatisticTypeAllocationGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Allocations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/StatisticType/' + id + '/allocations',
            datafields:
            [
                { name: 'ObjectID' },
                { name: 'StatisticTypeID' },
                { name: 'ObjectName' },
                { name: 'ObjectType' },
                { name: 'Score' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        TileTools(toolsControlID, [
            { icon: 'plus', uri: "/form/AddStatisticTypeRelation?id=" + id, context: contextList.AttributeTypeRelation, title: 'Add allocation' }
        ]);

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { text: 'Object Type', dataField: 'ObjectType' },
                { text: 'Object Name', dataField: 'ObjectName' },
                { text: 'Score', dataField: 'Score', width: 75 },
                {
                    text: '',
                    dataField: 'StatisticTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [
                            { icon: 'pencil', urlprefix: '/form/EditStatisticTypeRelation?id=' + data.StatisticTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID },
                            { icon: 'trash-o', urlprefix: '/form/DeleteStatisticTypeRelation?id=' + data.StatisticTypeID + "&objectType=" + data.ObjectType + "&objectTypeID=" + data.ObjectID }
                        ];
                        return renderToolsHtml(value, tools, contextList.StatisticTypeRelation);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.StatisticTypeRelation:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : StatisticTypeAllocationsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function SynonymTypeAllocationGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Allocations<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/SynonymType/' + id + '/allocations',
            datafields:
            [
                { name: 'ObjectID' },
                { name: 'SynonymTypeID' },
                { name: 'ObjectName' },
                { name: 'ObjectType' },
                { name: 'TypeName' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddSynonymTypeRelation?id=" + id, context: contextList.SynonymTypeRelation, title: 'Add allocation' }
            ]);
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { text: 'Object Type', dataField: 'ObjectType' },
                { text: 'Object Name', dataField: 'ObjectName' },
                {
                    text: '',
                    dataField: 'SynonymTypeID',
                    width: 40,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteSynonymTypeRelation?synonymTypeID=' + data.SynonymTypeID + "&type=" + data.ObjectType + "&id=" + data.ObjectID });
                        }

                        return renderToolsHtml(value, tools, contextList.SynonymTypeRelation);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.SynonymTypeRelation:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : SynonymTypeAllocationsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function TaxonomyTypeLevelsGrid(controlID, contextList, permissions, id) {

    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;

    var source;
    var adapter;

    //#region Grid

    try {
        $(controlID).html('<header>Levels<div id="' + toolsControlID + '"></div></header><div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/api/TaxonomyType/' + id + '/levels',
            datafields:
            [
                { name: 'TaxonomyTypeID' },
                { name: 'Name' },
                { name: 'Level' },
                { name: 'Description' }
            ]
        };

        adapter = new $.jqx.dataAdapter(source);

        if (permissions.HasPermission("Root", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'plus', uri: "/form/AddTaxonomyTypeLevel?id=" + id, context: contextList.TaxonomyTypeLevel, title: 'Add level' }
            ]);        
        }

        $(gridControlID).jqxGrid({
            width: grid_width,
            autoheight: true,
            autorowheight: true,
            sortable: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 20,
            filterable: true,
            showfilterrow: true,
            pageable: true,
            altrows: true,
            source: adapter,
            theme: list_theme,
            columns: [
                { datafield: "Level", text: "Level", width: '10%' },
                { datafield: "Name", text: "Name", width: '30%' },
                { datafield: "Description", text: "Description" },
                {
                    text: '',
                    dataField: 'TaxonomyTypeID',
                    width: 80,
                    filterable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                        var tools = [];

                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditTaxonomyTypeLevel?id=' + data.TaxonomyTypeID + '&level=' + data.Level });
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteTaxonomyTypeLevel?id=' + data.TaxonomyTypeID + '&level=' + data.Level });
                        }

                        return renderToolsHtml(value, tools, contextList.TaxonomyTypeLevel);
                    }
                }
            ]
        });
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.TaxonomyTypeLevel:
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) {
            logError("Parts.js : TaxonomyTypeLevelsGrid", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function WorkflowTypeRelationsGrid(controlID, contextList, permissions, type, id, headerTitle) {
    var gridControlID = controlID + "_grid";

    controlID = '#' + controlID;
    var html = "";
    var showHeader = (headerTitle != '');
    if (!headerTitle) headerTitle = 'Allocated Workflows';
    $(controlID).html((showHeader ? '<header>' + headerTitle + '</header>' : '') + '<div id="' + gridControlID + '"></div>')
    gridControlID = '#' + gridControlID;

    var initrowdetails = function (index, parentElement, gridElement, datarecord) {
        
        var i = 1;
        var col = 0;
        for (var name in datarecord.Properties) {

            col = (i % 2) + 1;

            if (name == 'Responsibilities') {
                col = 1;
            }

            var div = $($(parentElement).children()[col]);

            var value = datarecord.Properties[name];
            div.append('<label for="lbl' + name + '">' + name + '</label>');
            div.append('<div id="lbl' + name + '">' + ((value == '') ? 'No value' : value) + '</div>');

            i++;
        }
    }

    var source = {
        datatype: 'json',
        url: '/api/' + type + '/' + id + '/workflowtypes',
        datafields:
        [
            { name: 'WorkflowType' },
            { name: 'WorkflowTypeName' },
            { name: 'WorkflowTypeDisplayName'},
            { name: 'Enabled' },
            { name: 'Required' },
            { name: 'Properties' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: false,
            pageable: false,
            selectionmode: 'none',
            autorowheight: true,
            source: adapter,
            theme: list_theme,
            rowdetails: true,
            rowdetailstemplate: {
                rowdetails: "<h4>Workflow Settings</h4><div class='pull-left' style='width: 49%'></div><div class='pull-right' style='width: 49%'></div><div class='clearfix'></div>"
            },
            initrowdetails: initrowdetails,
            columns: [
                {
                    columntype: 'dropdownlist',
                    filtertype: 'checkedlist',
                    datafield: "WorkflowTypeDisplayName",
                    text: "Workflow Type"
                },
                //{ datafield: "Required", text: "Required?", columntype: 'checkbox', filtertype: 'bool', width: '15%' },
                { datafield: "Enabled", text: "Enabled?", columntype: 'checkbox', filtertype: 'bool', width: '15%' },
                {
                    datafield: "WorkflowType",
                    text: "",
                    width: '80px',
                    filterable: false,
                    sortable: false,
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        var tools = [];

                        //if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID) {
                        if (permissions.HasPermission("Root", "Update")) {
                            tools.push({ icon: 'pencil', urlprefix: '/form/EditWorkflowAllocation?workflowType={0}&type=' + type + '&id=' + id });
                        }
                        if (permissions.HasPermission("Root", "Delete")) {
                            tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteWorkflowAllocation?workflowType={0}&type=' + type + '&id=' + id });
                        }
                        //}

                        return renderToolsHtml(value, tools, "WorkflowTypeRelation", data);
                    }
                }
            ]
        });
    } catch (e) {
    }

    //#endregion

    //#region Event Subscriptions

    function pageResized() {
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case "WorkflowTypeRelation":
                    $(gridControlID).jqxGrid('updatebounddata');
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}

function YourFollowedItemsTile(controlID, resourceID, title) {
    try {
        var chart = $(controlID);
        var parentWidth = chart.parent().innerWidth();

        chart.css('width', '100%');
        chart.css('height', '400px');

        try {
            chart.jqxChart('destroy');
        } catch (e) { }

        try {
            var source = {
                datatype: 'json',
                url: '/queries/FollowingBreakdownByResource?id=' + resourceID,
                datafields:
                [
                    { name: 'Type' },
                    { name: 'TypeID' },
                    { name: 'TypeName' },
                    { name: 'Count' }
                ]
            };

            var adapter = new $.jqx.dataAdapter(source);

            chart.jqxChart({
                title: title,
                description: "",
                enableAnimations: true,
                showLegend: true,
                showBorderLine: false,
                legendLayout: { left: 0, top: 250, width: parentWidth - 25, height: 150, flow: 'vertical' },
                padding: { left: 0, right: 0, top: 0, bottom: 150 },
                source: adapter,
                colorScheme: chartDefaultTheme,
                seriesGroups: [{
                    useGradientColors: false,
                    type: 'pie',
                    series: [
                        {
                            showLabels: true,
                            useGradient: false,
                            dataField: 'Count',
                            displayText: 'TypeName',
                            labelRadius: 50,
                            initialAngle: 15,
                            radius: 100,
                            centerOffset: 0
                        }
                    ],
                    click: function (e) {
                        var data = adapter.records[e.elementIndex];
                        var url = '/parts/Following?resourceID=' + resourceID + '&type=' + data.Type + '&id=' + data.TypeID;
                        openTileOverlay(url);
                    }
                }]
            });
        } catch (e) { }

        function pageResized() {
            chart.jqxChart('refresh');
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;

            amplify.unsubscribe("PageResized", pageResized);
            amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    }
    catch (e) {
    }
}

function YourOwnedItemsTile(controlID, resourceID, title) {
    try {

        var chart = $(controlID);
        var parentWidth = chart.parent().innerWidth();

        chart.css('width', '100%');
        chart.css('height', '400px');

        try {
            chart.jqxChart('destroy');
        } catch (e) { }

        try {
            var source = {
                datatype: 'json',
                url: '/queries/ResponsibilityBreakdownByResource?id=' + resourceID,
                datafields:
                [
                    { name: 'ObjectType' },
                    { name: 'ObjectTypeID' },
                    { name: 'ObjectTypeName' },
                    { name: 'Count' }
                ]
            };

            var adapter = new $.jqx.dataAdapter(source);

            chart.jqxChart({
                title: title,
                description: "",
                enableAnimations: true,
                showLegend: true,
                showBorderLine: false,
                legendLayout: { left: 0, top: 250, width: parentWidth-25, height: 150, flow: 'vertical' },
                padding: { left: 0, right: 0, top: 0, bottom: 150 },
                source: adapter,
                colorScheme: chartDefaultTheme,
                seriesGroups: [{
                    useGradientColors: false,
                    type: 'pie',
                    series: [
                        {
                            showLabels: true,
                            useGradient: false,
                            dataField: 'Count',
                            displayText: 'ObjectTypeName',
                            labelRadius: 50,
                            initialAngle: 15,
                            radius: 100,
                            centerOffset: 0
                        }
                    ],
                    click: function (e) {
                        var data = adapter.records[e.elementIndex];
                        var url = '/parts/resources/' + resourceID + '/ownership/' + data.ObjectType + '/' + data.ObjectTypeID;
                        openTileOverlay(url);
                    }
                }]
            });
            //chart.jqxChart('addColorScheme', 'myScheme', colorScheme);
            //chart.jqxChart('colorScheme', 'myScheme');
            //chart.jqxChart('refresh');
        } catch (e) { }

        function pageResized() {
            chart.jqxChart('refresh');
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;

            amplify.unsubscribe("PageResized", pageResized);
            amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }
    catch (e) {
    }
}

function YourWorkflowTasks(controlID,givenWorkflowType) {    
    var gridControlID = controlID + "_grid";    
    controlID = '#' + controlID;
    var html = "";
    html += '<div class="row">';    
    html += '<div class="col s12"><div id="' + gridControlID + '"></div></div>';
    html += '</div>';
    $(controlID).html(html);    
    gridControlID = '#' + gridControlID;
    
            
    var gridSource;
    var gridAdapter;
    var inputWorkflowID = givenWorkflowType;
    
    //#region Event Subscriptions

    

    //function saveAction(data) {
        /*var reloadControlData = function () {
            var reloadChartData = function () {
                var pr = new $.Deferred();
                chartAdapter.dataBind();
                return pr.promise();
            }
            reloadChartData().then(function () {
                chart.jqxGrid('updatebounddata');
                $(gridControlID).jqxGrid('updatebounddata');
            });
        }
        try {
            switch (data.context) {
                case "Workflow":
                case "OwnerApprovalWorkflow":
                case "OwnerCertificationWorkflow":
                case "IssueWorkflow":
                    reloadControlData();
                    break;
                case "commentform":
                    if (data.custom.CommentTypeID == 5) {
                        reloadControlData();
                    }
                    break;
            }
        } catch (e) { }*/
    //}

    function saveAction(data) {
        //console.log(data);
        try {
            switch (data.context) {
                case "workflowform":
                case "artifactform":
                    switchToViewer();
                   // $(gridControlID).jqxGrid('updatebounddata');
            }
        } catch (e) {
            logError("YourWorkflowTasks : SaveAction", e);
        }
    }

    function pageResized() {
        $(gridControlID).jqxGrid('autoresizecolumns');
    }

   /* function cancelAction(data) {
        console.log(data);
        try {
            switch (data.context) {
                case "workflowform":
                case "artifactform":
                    switchToViewer();
                    break;
            }
        } catch (e) {
            logError("YourWorkflowTasks : CancelAction", e);
        }
    }*/

    function localAction(data) {
        //console.log(data.context);
        try {
            switch (data.context) {
                case "workflowform":
                case "artifactform":                
                    switchToEditor(data.uri);
                    break;
            }
        } catch (e) {
            logError("YourWorkflowTasks : LocalAction", e);
        }
    };

    function unsubscribe(data) {
        gridSource = null;
        gridAdapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe('ToolAction', localAction);
   //     amplify.unsubscribe('CancelAction', cancelAction);
    }

    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe('ToolAction', localAction);
   // amplify.subscribe('CancelAction', cancelAction);

    //#endregion
    
    //#region Helper Functions

    var switchToViewer = function () {
        $('#assignmentoverlay').show();
    }

    var switchToEditor = function (uri) {
        try {
            $('#assignmentoverlay').fadeOut(10);
          /*  $('#PromotionEditor').fadeIn(10);
            $('#PromotionEditor').html(progressIndicatorHtml);
            $('#PromotionEditor').load(uri, function (response, status, xhr) {
                if (status == "error") {
                    amplify.publish("ShowMessage", { title: "Something unexpected happened!", message: xhr.status + ' ' + xhr.statusText, type: 'error' });
                    switchToViewer();
                }
            });*/
        } catch (e) {

        }
    }

    var gridDataSource = function (workflowTypeID) {
        var gridSource;
        switch (workflowTypeID) {
            case 1:
                // Suggest
                gridSource = {
                    datatype: 'json',
                    url: '/services/workflow/tasks/types/' + workflowTypeID + '?$orderby=DateStarted%20asc',
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'ID', type: 'number' },
                        { name: 'StartDate', type: 'date' },
                        { name: 'Name', type: 'string' },
                        { name: 'Url', type: 'string' },
                        { name: 'ProposedName', type: 'string' },
                        { name: 'ProposedDescription', type: 'string' },
                        { name: 'RequestingResourceID', type: 'number' },
                        { name: 'RequestingResourceName', type: 'string' },
                        { name: 'TaxonomyTypeID', type: 'number' },
                        { name: 'TaxonomyTypeName', type: 'string' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                        ]
                };
                
                break;
            case 2:
                // Certify
                gridSource = {
                    datatype: 'json',
                    url: '/services/workflow/tasks/types/' + workflowTypeID + '?$orderby=DateStarted%20asc',
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'ID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'TypeName', type: 'string' },
                        { name: 'Url', type: 'string' },
                        { name: 'StartDate', type: 'date' },
                        { name: 'DueDate', type: 'date' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };                
                break;
            case 3:
                // WorkIssue
                gridSource = {
                    datatype: 'json',
                    url: '/services/workflow/tasks/types/' + workflowTypeID + '?$orderby=DateStarted%20asc',
                    datafields: [
                        { name: 'WorkflowID' },
                        { name: 'Issue', type: 'string' },
                        { name: 'ResourceID', type: 'number' },
                        { name: 'ResourceName', type: 'string' },
                        { name: 'ResourceUrl', type: 'string' },
                        { name: 'DateStarted', type: 'date' },
                        { name: 'Activity', type: 'string' },
                        { name: 'ActivityDescription', type: 'string' },
                        { name: 'ActivityName', type: 'string' }
                    ]
                };                
                break;
        }
        return gridSource;
    }

    var gridColumns = function (workflowTypeID) {
        var cols = null;
        switch (workflowTypeID) {
            case 1:
                //#region Suggest                
                cols = [
                    {
                        datafield: "Name", text: "Type", filtertype: 'checkedlist',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('ArtifactType', data.ID, data.Url, data.Name);
                        }
                    },
                    {
                        filtertype: 'checkedlist', datafield: "RequestingResourceName", text: "Requestor",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Resource', data.RequestingResourceID, '#/resources/' + data.RequestingResourceID, data.RequestingResourceName);
                        }
                    },
                    { datafield: "StartDate", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ProposedName", text: "Proposed Name" },                    
                    { datafield: "TaxonomyTypeName", text: "Subject Area", filtertype: 'checkedlist' },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Artifact, data);
                        }
                    }
                ];
                //#endregion
                break;
            case 2:
                
                cols = [
                    { datafield: "TypeName", text: "Type", filtertype: 'checkedlist' },
                    {
                        filtertype: 'checkedlist', datafield: "Name", text: "Name",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Artifact', data.ID, data.Url, data.Name);
                        }
                    },
                    { datafield: "StartDate", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "DueDate", text: "Date Due", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Artifact, data);
                        }
                    }
                ];
                //#endregion
                break;
            case 3:                
                cols = [
                    { datafield: "Issue", text: "Issue" },
                    { filtertype: 'checkedlist', datafield: "ResourceName", text: "Reporting User",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return previewLinkRenderer('Resource', data.ResourceID, data.ResourceUrl, data.ResourceName);
                        }
                    },
                    { datafield: "DateStarted", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy" }, // hh:mm:ss tt },
                    { datafield: "ActivityName", text: "Activity", filtertype: 'checkedlist' },
                    {
                        datafield: "WorkflowID",
                        text: "",
                        sortable: false,
                        filterable: false,
                        width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Workflow, data);
                        }
                    }
                ];
                //#endregion
                break;            
        }
        return cols;

    };

    //#endregion

    //#region Item Grid

    var gridAdapter = new $.jqx.dataAdapter(gridDataSource(inputWorkflowID));
    
    try {
        $(gridControlID).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 10,
            pageable: true,
            selectionmode: 'none',
            autorowheight: true,
            source: gridAdapter,
            theme: list_theme,
            columns: gridColumns(inputWorkflowID)            
        });
    } catch (e) {
    }

    //#endregion
        
    //#endregion
}

function LineageDiagram(controlID, type, id, permissions, readonly) {
    var originalObject = type;
    var originalObjectID = id;
    var fullscreen = false;

    var tmpl = Handlebars.getTemplate('LineageDiagram');
    $('#' + controlID).html(tmpl({ control: controlID }));

    //#region Control constants

    var controlID_header = controlID + "_header";
    var controlID_wrapper = controlID + '_wrapper';
    var controlID_diagram = controlID + '_dgm';
    var controlID_palette = controlID + '_palette';
    var controlID_overview = controlID + '_overview';
    var controlID_sidebar = controlID + '_sidebar';
    var controlID_overlay = controlID + '_overlay';
    var controlID_ribbon = controlID + '_ribbon';
    var controlID_wrapper_fullscreen = controlID + '_wrapper_fullscreen';
    var controlID_message = controlID + '_message';

    var controlID_controls = controlID + '_controls';

    var controlID_info = controlID + '_info';
    var controlID_info_body = controlID + '_info_body';

    var controlID_sourcerules = controlID + '_sourcerules';
    var controlID_sourcerules_table = controlID + '_sourcerules_table';

    var controlID_add_search_text = controlID + '_add_search_text';
    var controlID_add_search = controlID + '_add_search';
    var controlID_add_artifact_type = controlID + '_add_artifact_type';
    var controlID_add_search_message = controlID + '_add_search_message';

    var controlID_overlay_existing = controlID + '_overlay_existing';
    var controlID_overlay_new = controlID + '_overlay_new';
    var controlID_overlay_relationship = controlID + '_overlay_relationship';
    var controlID_overlay_predicates = controlID + '_overlay_predicates';
    var controlID_overlay_cancel = controlID + '_overlay_cancel';
    var controlID_overlay_add = controlID + '_overlay_add';
    var controlID_overlay_roles = controlID + '_overlay_roles';

    var controlID_responsibilities = controlID + '_responsibilities';
    var controlID_responsibilities_table = controlID + '_responsibilities_table';

    var controlID_fusion = controlID + '_fusion';
    var controlID_fusion_body = controlID + '_fusion_body';

    var controlID_ribbon_spacer = controlID + '_ribbon_spacer';
    var controlID_ribbon_content = controlID + '_ribbon_content';
    var controlID_ribbon_expander = controlID + '_ribbon_expander';
    var controlID_ribbon_zoom_slider = controlID + '_ribbon_zoom_slider';
    var controlID_ribbon_zoom_out = controlID + '_ribbon_zoom_out';
    var controlID_ribbon_zoom_in = controlID + '_ribbon_zoom_in';
    var controlID_ribbon_zoom_text = controlID + '_ribbon_zoom_text';
    var controlID_ribbon_zoom_100 = controlID + '_ribbon_zoom_100';
    var controlID_ribbon_zoom_fit = controlID + '_ribbon_zoom_fit';
    var controlID_ribbon_reset = controlID + '_ribbon_reset';
    var controlID_ribbon_fullscreen = controlID + '_ribbon_fullscreen';
    
    var controlID_ribbon_save = controlID + '_ribbon_save';
    var controlID_ribbon_save_spinner = controlID + '_ribbon_save_spinner';
    var controlID_ribbon_add = controlID + '_ribbon_add';
    var controlID_ribbon_undo = controlID + '_ribbon_undo';
    var controlID_ribbon_redo = controlID + '_ribbon_redo';
    var controlID_ribbon_remove = controlID + '_ribbon_remove';
    var controlID_ribbon_sourcerule_add = controlID + '_ribbon_sourcerule_add';
    var controlID_ribbon_sourcemapping_add = controlID + '_ribbon_sourcemapping_add';

    var controlID_popover_add = controlID + '_popover_add';

    var controlID_popover_sourcerule_editor = controlID + '_popover_sourcerule_editor';
    var controlID_popover_sourcerule_editor_body = controlID_popover_sourcerule_editor + '_body';
    var controlID_popover_sourcemapping_editor = controlID + '_popover_sourcemapping_editor';
    var controlID_popover_sourcemapping_editor_body = controlID_popover_sourcemapping_editor + '_body';

    //#endregion

    //#region Control instantiation

    $("#" + controlID_ribbon_zoom_100).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_zoom_fit).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_save).jqxButton({ theme: theme, height: "100%", width: 64, disabled: true });
    $("#" + controlID_ribbon_reset).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_fullscreen).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_add).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_remove).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_undo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_redo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_zoom_out).jqxRepeatButton({ delay: 3, theme: theme });
    $("#" + controlID_ribbon_zoom_in).jqxRepeatButton({ delay: 3, theme: theme });

    $("#" + controlID_ribbon_sourcerule_add).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_sourcemapping_add).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_sourcerules).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_info).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_responsibilities).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_fusion).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_ribbon_expander).jqxExpander({ theme: theme }).jqxExpander('collapse');

    //#endregion

    $("#" + controlID_message).hide();

    $("#" + controlID_ribbon_add).on('click', function () {
        $('#' + controlID_popover_add).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 150);
    });

    $("#" + controlID_ribbon_sourcerule_add).on('click', function () {
        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;
        //console.log(selected);
        $('#' + controlID_popover_sourcerule_editor).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 150);

        //TODO: logic for nothing selected
        var data = {
            target: type,
            targetID: id,
            object: selected.type,
            objectID: selected.id,
            ID: 0,
            controlID: controlID
        };

        var model = new HierarchyPanelViewModel(data);
        ko.cleanNode($('#' + controlID_popover_sourcerule_editor_body)[0]);
        ko.applyBindings(model, $('#' + controlID_popover_sourcerule_editor_body)[0]);
        //model.ApplyJqxBindings();
    });
    
    $("#" + controlID_ribbon_sourcemapping_add).on('click', function () {
        $('#' + controlID_popover_sourcemapping_editor).toggle(200).css('left', $(this).position().left + 1).css('top', $(this).position().top + 150);
        var selected = myDiagram.selection;
        if (selected == null)
            return;
        var selected = selected.first().data;
        if (selected == null)
            return;
        
        var from = myDiagram.model.findNodeDataForKey(selected.from);
        var to = myDiagram.model.findNodeDataForKey(selected.to);

        //console.log(from);
        //console.log(to);

        var data = {
            Source: from.type,
            SourceID: from.id,
            SourceName: from.name,
            SourceTypeName: from.typeName,
            Target: to.type,
            TargetID: to.id,
            TargetName: to.name,
            TargetTypeName: to.typeName,
            Object: type,
            ObjectID: id,
        };
        var model = new SourceToTargetMappingModel(data);
        ko.cleanNode($('#' + controlID_popover_sourcemapping_editor_body)[0]);
        ko.applyBindings(model, $('#' + controlID_popover_sourcemapping_editor_body)[0]);
    });

    $('#' + controlID_ribbon).jqxRibbon({
        width: "100%",
        height: 64,
        animationType: "fade",
        selectionMode: "click",
        position: "top",
        theme: theme,
        mode: "default",
        selectedIndex: 0
    });

    $('#' + controlID_ribbon_fullscreen).on('click', function () {
        fullscreen = !fullscreen;
        if (fullscreen) {
            window.scrollTo(0, 0); //scroll to top
            $('#' + controlID_ribbon_fullscreen).html('<i class="fa fa-2x fa-sign-out"></i><br />Exit Fullscreen');
            $('#' + controlID_wrapper_fullscreen).css('position', 'fixed')
                .css('left', '0')
                .css('top', '0')
                .css('width', '100%')
                .height($(window).height())
                .css('overflow', 'hidden');
            
            var top = $('#' + controlID_wrapper).position().top;

            $('#' + controlID_wrapper).height($('#' + controlID_wrapper_fullscreen).height() - top - 20);
            $('#' + controlID_diagram).height($('#' + controlID_wrapper_fullscreen).height() - top - 20);

            $('#' + controlID_sidebar).height($('#' + controlID_wrapper_fullscreen).height() - 20);
        } else {
            $('#' + controlID_ribbon_fullscreen).html('<i class="fa fa-2x fa-arrows-alt"></i><br />Fullscreen');
            $('#' + controlID_wrapper_fullscreen).attr('style', 'z-index:1000000;background-color:white;');
            $('#' + controlID_wrapper).height(520);
            $('#' + controlID_diagram).height(520);
            $('#' + controlID_sidebar).height(520);
        }
        //force this to queue behind browser layout updates
        setTimeout(function () {
            myDiagram.requestUpdate();
            myDiagram.focus();
        }, 0);
    });

    $('#' + controlID_ribbon_save).on('click', function () {
        saveChanges();

        //var message = $('<p />', { text: 'Are you sure you want to save changes?' }),
        //     ok = $('<button />', {
        //         text: 'Save',
        //         'class': 'btn qtip-blue qtip-btn-inline',
        //     }),
        //     cancel = $('<button />', {
        //         text: 'Cancel',
        //         'class': 'btn qtip-blue qtip-btn-inline',
        //     });

        //var content = "<p>Are you sure you want to save changes?</p>"

        //confirmDialog(message.add(ok).add(cancel), 'Save Changes?', 'Save', saveChanges);
    });

    function confirmDialog(content, title, id, func) {
        $('<div />').qtip({
            content: {
                text: content,
                title: title
            },
            position: {
                my: 'center', at: 'center',
                target: $(window)
            },
            show: {
                ready: true,
                modal: {
                    on: true,
                    blur: false
                }
            },
            hide: false,
            style: {
                classes: 'qtip-blue qtip-rounded'
            },
            events: {
                render: function (event, api) {
                    $('button', api.elements.content).click(function (e) {
                        api.hide(e);
                        if ($(this).text() == id) {
                            $(this).prop('disabled', true);
                            func();
                        }
                        
                    });
                },
                hide: function (event, api) { api.destroy(); }
            }
        })
    }

    $('#' + controlID_ribbon_undo).on('click', function () {
        myDiagram.undoManager.undo();
    });
    $('#' + controlID_ribbon_redo).on('click', function () {
        myDiagram.undoManager.redo();
    });

    $('#' + controlID_ribbon_zoom_slider).on('input', function () {
        var val = $(this).val();
        $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
        myDiagram.scale = (val / 1500);
    });

    $('#' + controlID_ribbon_zoom_in).on('click', function () {
        var val = parseInt($('#' + controlID_ribbon_zoom_slider).val()) + 5;
        $('#' + controlID_ribbon_zoom_slider).val(val);
        $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
        myDiagram.scale = (val / 1500);
    });

    $('#' + controlID_ribbon_zoom_out).on('click', function () {
        var val = $('#' + controlID_ribbon_zoom_slider).val();
        $('#' + controlID_ribbon_zoom_slider).val(val - 5);
        $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
        myDiagram.scale = (val / 1500);
    });


    var oldHeight = $('#' + controlID_wrapper).height();
    var oldWidth = $('#' + controlID_wrapper).width();

    var newId = -1;
    var temp = null;
    var relationshipLabels = null;
    var pendingExclusionObjects = [];
    var pendingExclusionNodes = [];
    var deletedNodes = [];
    var initialLinks = [];
    var initialNodes = [];
    var newLink = null;
    var predicates = [];
    var overlayEditLinkKey = null;
    var selection = null;
    //#region Responsibilities

    var lineageResponsibilitySource = {
        datatype: 'json',
        url: null,
        datafields:
        [
            { name: 'ResponsibilityID' },
            { name: 'AssigningItemType' },
            { name: 'AssigningItemID' },
            { name: 'AssigningItemName' },
            { name: 'AssigningItemUrl' },
            { name: 'ResponsibleObjectType' },
            { name: 'ResponsibleObjectID' },
            { name: 'ResponsibleObjectName' },
            { name: 'PrimaryOwnerResourceID' },
            { name: 'PrimaryOwnerResourceName' },
            { name: 'PrimaryOwnerResourceUrl' },
            { name: 'ObjectType' },
            { name: 'ObjectID' },
            { name: 'Role' },
            { name: 'ResponsibleObjectUrl' }
        ]
    };

    var lineageResponsibilityAdapter = new $.jqx.dataAdapter(lineageResponsibilitySource);

    $('#' + controlID_responsibilities_table).jqxGrid({
        altrows: true,
        width: overlay_grid_width,
        autoheight: true,
        sortable: true,
        filterable: true,
        showfilterrow: false,
        pagesize: 5,
        pageable: true,
        pagermode: "simple",
        selectionmode: 'none',
        autorowheight: true,
        source: lineageResponsibilityAdapter,
        theme: list_theme,
        columns: [
            { columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "Role", text: "Role", width: '34%' },
            {
                columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "ResponsibleObjectName", text: "Resource", width: '33%',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return previewLinkRenderer(data.ResponsibleObjectType, data.ResponsibleObjectID, data.ResponsibleObjectUrl, data.ResponsibleObjectName);
                }
            },
            {
                columntype: 'dropdownlist', filtertype: 'checkedlist', datafield: "PrimaryOwnerResourceName", text: "Group Owner", width: '33%',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    if (data.PrimaryOwnerResourceName && data.PrimaryOwnerResourceName != '')
                        return previewLinkRenderer('Resource', data.PrimaryOwnerResourceID, data.PrimaryOwnerResourceUrl, data.PrimaryOwnerResourceName);
                    else
                        return '';
                }
            }
        ]
    });

    //#endregion

    //#region Technical Relations

    var technicalRelationsSource = {
        datatype: 'json',
        url: null,
        datafields: [
            { name: 'IntersectID' },
            { name: 'Description' },
            { name: 'TargetName' },
            { name: 'TargetObjectID' },
            { name: 'TargetObjectType' },
            { name: 'TargetTypeID' },
            { name: 'TargetType' },
            { name: 'TargetTypeName' },
            { name: 'Classification' },
            { name: 'TargetUrl' }
        ]
    };

    var technicalRelationsAdapter = new $.jqx.dataAdapter(technicalRelationsSource);

    $('#' + controlID_fusion_body).jqxGrid({
        width: overlay_grid_width,
        autoheight: true,
        sortable: true,
        altrows: true,
        filterable: true,
        showfilterrow: false,
        pagesize: 5,
        pageable: true,
        pagermode: "simple",
        selectionmode: 'none',
        autorowheight: true,
        columnsresize: true,
        enabletooltips: true,
        source: technicalRelationsAdapter,
        theme: list_theme,
        groupable: false,
        ready: function () {
            $('#' + controlID_fusion_body).jqxGrid('autoresizecolumns');
        },
        columns: [
            //{ text: 'Type', groupable: true, datafield: 'TargetTypeName' },
            {
                text: 'Name', groupable: false, datafield: 'TargetName',
                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                    return textrenderer("<div class='cell-value-name'>" + data.TargetName + "</div><div class='cell-value-type'>" + data.TargetTypeName + "</div>");
                }
            }
        ]
    });

    //#endregion

    //#region methods

    if (readonly) {
        $('#' + controlID_ribbon_undo).hide();
        $('#' + controlID_ribbon_redo).hide();
        $('#' + controlID_ribbon_add).hide();
        $('#' + controlID_ribbon_remove).hide();
        $('#' + controlID_ribbon_save).hide();
    } else {
        $("#" + controlID_ribbon_remove).jqxButton({ theme: theme });
        $("#" + controlID_ribbon_remove).on('click', function () {
            //console.log(selection.length);
            markForDeletion(selection);
            $("#" + controlID_ribbon_remove).hide(200);
            //populateDiagram();
        });
    }

    function createLinkModel() {
        return {
            key: null,
            id: null,
            from: null,
            fromPortId: "OUT",
            to: null,
            toPortId: "IN",
            text: null,
            predicateId: null,
            isDeletable: true,
            exclude: 'false',
            diagramObjectType: "Link"
        };
    };

    function createNodeModel() {
        return {
            key: null,
            id: null,
            parentId: null,
            name: null,
            type: null,
            typeName: null,
            backColor: null,
            foreColor: null,
            isDeletable: true,
            exclude: 'false',
            highlightColor: null,
            diagramObjectType: "Node",
            level: null,
            template: "Artifact",
            intersectMapId: null,
            intersectId: null,
            sourceRuleCount: 0,
            isVisible: false
        };
    };

    function onLayoutCompleted() {
        //console.log(myDiagram.documentBounds.height);
        //console.log(myDiagram.viewportBounds.height);
        //var height = $('#' + controlID_diagram).height($(window).innerHeight());
    }

    function makePort(name, leftside) {
        var port = g(go.Shape, "Circle", {
            fill: "white",
            stroke: "gray",
            strokeWidth: 3,
            desiredSize: new go.Size(9, 9),
            portId: name, // declare this object to be a "port"
            //toMaxLinks: 1, // don't allow more than one link into a port
            cursor: "pointer" // show a different cursor to indicate potential link point
        });

        var panel = g(go.Panel, "Horizontal", {
            margin: new go.Margin(2, 0)
        });

        if (leftside) {
            port.toSpot = go.Spot.Left;
            port.toLinkable = true;
            panel.alignment = go.Spot.TopLeft;
            panel.add(port);
        } else {
            port.fromSpot = go.Spot.Right;
            port.fromLinkable = true;
            panel.alignment = go.Spot.TopRight;
            panel.add(port);
        }
        return panel;
    }

    function makeTemplate(obj, w, h, borderColor, fontSize, inports, outports) {

        var node = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: w,
            height: h
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: borderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "backColor").makeTwoWay()
       ),
        g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.LeftCenter
            },
            g(go.Shape, "Circle",
                {
                    fill: '#DD1148',
                    toolTip: g(go.Adornment, "Auto", g(go.Shape, { fill: "lightyellow" }), g(go.Panel, "Vertical", g(go.TextBlock, { margin: 3, text: 'Source rule defined' })))
                }
            ),
            g(go.TextBlock,
                {
                    row: 0,
                    margin: 3,
                    alignment: go.Spot.LeftCenter,
                    editable: false,
                    stroke: '#ffffff',
                    font: "bold " + fontSize + "pt sans-serif"
                }//,
                //new go.Binding("text", "sourceRuleCount").makeTwoWay()
            ),
            new go.Binding("visible", "isVisible")
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(w - 20, h - 10),
                font: "bold " + fontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "foreColor").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (fontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "foreColor").makeTwoWay(),
                new go.Binding("text", "typeName").makeTwoWay()
            )//,
            //g(go.TextBlock, {
            //    row: 2,
            //    margin: 3,
            //    maxSize: new go.Size(180, NaN),
            //    font: "bold 10pt sans-serif"
            //},
            //    new go.Binding("text", "role").makeTwoWay(),
            //    new go.Binding("stroke", "fore").makeTwoWay()
            //)
        )),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Left,
            alignmentFocus: new go.Spot(0, 0.5, -8, 0)
        },
        inports),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        outports));

        myDiagram.nodeTemplateMap.add(obj, node);
    }

    function makeSearchTemplate() {
        var node = g(go.Node, "Spot",
               {
                   mouseEnter: mouseEnter,
                   mouseLeave: mouseLeave
               },
           g(go.Panel, "Auto", {
               width: 250,
               height: 22,
               name: "NodePanel"
           },
           g(go.Shape, "RoundedRectangle", {
               stroke: 'transparent',
               strokeWidth: 2,
               spot1: go.Spot.TopLeft,
               spot2: go.Spot.BottomRight,
               name: "NodeShape"
           },
               new go.Binding("fill", "backColor").makeTwoWay()
          ),
           g(go.Panel, "Table",
               g(go.TextBlock, {
                   row: 0,
                   margin: 3,
                   alignment: go.Spot.Top,
                   editable: false,
                   maxSize: new go.Size(250, 22),
                   font: "8pt sans-serif"
               },
                   new go.Binding("text", "name").makeTwoWay()
                   , new go.Binding("stroke", "foreColor").makeTwoWay()
               ))
           ));

        myPalette.nodeTemplateMap.add("Artifact", node);
    }
    
    function mouseEnter(e, node) {
        node.isShadowed = true;
    };

    function mouseLeave(e, node) {
        node.isShadowed = false;
    };

    function onDoubleClick(e) {

        var obj = e.diagram.selection.first().data;
        if (obj != null) {
            if (obj.diagramObjectType == 'Node') {


                var message = $('<p />', { text: 'You are about to navigate to a different lineage diagram. You will lose any unsaved changes. Continue?' }),
                 ok = $('<button />', {
                     text: 'Okay',
                     'class': 'btn qtip-blue qtip-btn-inline',
                 }),
                 cancel = $('<button />', {
                     text: 'Cancel',
                     'class': 'btn qtip-blue qtip-btn-inline',
                 });
                if (checkModified()) {
                    confirmDialog(message.add(ok).add(cancel), 'Confirm Navigation', 'Okay', function () {
                        type = obj.type;
                        id = obj.id;
                        populateDiagram();
                    });
                } else {
                    type = obj.type;
                    id = obj.id;

                    populateDiagram();
                    $('#' + controlID_ribbon_remove).hide(200);
                }
            }
            else if (obj.diagramObjectType == 'Link' && !readonly) {
                overlayEditLinkKey = obj.key;
                showRelationshipOverlay(obj);
            }
        }
    }

    function onSelectionChange(e) {
        selection = e.diagram.selection;

        if (selection.count < 1) {
            $("#" + controlID_ribbon_sourcerule_add).hide(200);
            $("#" + controlID_ribbon_sourcemapping_add).hide(200);
            $("#" + controlID_ribbon_remove).hide(200);
            $('#' + controlID_fusion).jqxExpander('collapse');
            $('#' + controlID_responsibilities).jqxExpander('collapse');
            $('#' + controlID_info).jqxExpander('collapse');
            return;
        } else {
            if (!readonly) {
                $("#" + controlID_ribbon_remove).show(200);
                $("#" + controlID_ribbon_sourcerule_add).show(200);
            }
        }

        //get a deep copy of the selection as an array
        var sel = $.extend(true, [], selection.toArray()); //JSON.parse(JSON.stringify(selection.toArray()));
        var firstNodePopulated = false;
        var sourceNodes = [];

        for (var i = 0; i < sel.length; i++ )
        {
            var data = sel[i].data;

            if (data.diagramObjectType == 'Node') {
                
                 for (var it = sel[i].findNodesInto(); it.next(); ) {
                    var n = it.value;  // n is now a Node.
                    var d = n.data;
                    sourceNodes.push(d);
                 }

                //#region Info
                if (!firstNodePopulated) {
                    $.ajax({
                        url: '/resources/' + data.type + '/' + data.id + '/templates/tooltip/Preview',
                        data: null,
                        success: function (data) {
                            $('#' + controlID_info_body).html(data);
                            $("#" + controlID_info).jqxExpander('expand');
                        },
                        async: true
                    });

                    try {
                        technicalRelationsSource.url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + type + '&sID=' + id + '&t=' + data.type + '&tID=' + data.id;
                        $('#' + controlID_fusion_body).jqxGrid('updatebounddata');
                    } catch (e) {
                    }
                    $("#" + controlID_fusion).jqxExpander('expand');

                    if (sourceNodes.length > 0) { //(data.sourceRuleCount > 0) {
                        //$('#' + controlID_sourcerules).show();
                        $("#" + controlID_sourcerules).jqxExpander('expand');

                        $.getJSON('/api/' + type + '/' + id + '/sources/' + data.type + '/' + data.id + '/rules', function(rules) {
                            var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                            $('#' + controlID_sourcerules_table).html(sourceTemplate(rules));
                        });
                    }
                    else {
                        $("#" + controlID_sourcerules_table).html('');
                        $("#" + controlID_sourcerules).jqxExpander('collapse');
                        //$('#' + controlID_sourcerules).hide();
                    }

                    $("#" + controlID_responsibilities).show();
                    try {
                        lineageResponsibilitySource.url = '/api/' + data.type + '/' + data.id + '/ownership?showHidden=false';
                        $('#' + controlID_responsibilities_table).jqxGrid('updatebounddata');
                    } catch (e) {

                    }
                    $("#" + controlID_responsibilities).jqxExpander('expand');
                }
                //#endregion

                firstNodePopulated = true;

            } else if (data.diagramObjectType == "Link") { //link selected

                $('#' + controlID_info_body).html('');
                $("#" + controlID_info).jqxExpander('collapse');
                $("#" + controlID_ribbon_sourcemapping_add).show(200);
                //$("#" + controlID_sourcerules).jqxExpander('collapse');

                lineageResponsibilitySource.url = null;
                $('#' + controlID_responsibilities_table).jqxGrid('updatebounddata');
                $("#" + controlID_responsibilities).jqxExpander('collapse');

                if (data.intersectId != null) {
                    technicalRelationsSource.url = '/api/Intersect/' + data.intersectId + '/relations';
                    $('#' + controlID_fusion_body).jqxGrid('updatebounddata');
                    $("#" + controlID_fusion).jqxExpander('expand');
                }
            }
        }
    }

    function getImmediateParents(key) {
        //console.log(key);
        var parents = [];
        var links = [];
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].to == key) {
                //console.log(myDiagram.model.linkDataArray[i]);
                parents.push(myDiagram.model.findNodeDataForKey(myDiagram.model.linkDataArray[i].from));
            }
        }
       // console.log(parents);
        return parents;
    }

    function onViewportBoundsChanged() {
        var s = myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        //console.log('vpchanged');
        $('#' + controlID_ribbon_zoom_text).text(Math.round(myDiagram.scale * 100) + '%');
        $('#' + controlID_ribbon_zoom_slider).val(Math.round(myDiagram.scale * 1500));

        //console.log(myDiagram.div.style.height);
    };

    function onLinkDrawn(e) {
        overlayEdit = false;
        showRelationshipOverlay(e.subject.data);
    };

    function showRelationshipOverlay(linkData) {

        newLink = linkData;

        newLink.diagramObjectType = "Link";
        var fromNode = myDiagram.model.findNodeDataForKey(linkData.from);
        var toNode = myDiagram.model.findNodeDataForKey(linkData.to);

        $('#' + controlID_overlay_relationship).html('<span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
            + (fromNode.foreColor || 'black')
            + ';background-color: '
            + (fromNode.backColor || 'white')
            + ';" >'
            + fromNode.typeName
            + '</span><span style="font-size:1.5rem;font-weight:800;color:grey">&#8594;</span><span style="padding:3px; border: 0 solid transparent; border-radius:3px;color: '
            + (toNode.foreColor || 'black')
            + ';background-color: '
            + (toNode.backColor || 'white')
            + ';">'
            + toNode.typeName + '</span>');

        $('#' + controlID_overlay_add).show();

        var data = {
            type1: fromNode.typeName,
            type2: toNode.typeName
        };
        populatePredicateList();

        $('#' + controlID_overlay).show();
    }
    
    function onChange(e) {
        checkModified();

    }

    function onDeleting(e) {
        if (readonly) {
            e.cancel = true;
            return;
        }
        markForDeletion(selection);
        $('#' + controlID_ribbon_expander).jqxExpander('expand');
        //$('#' + controlID_ribbon).jqxRibbon('selectAt', 1);
    };

    function onDeleted(e) {
        selection = null;
    }

    function markForDeletion(set) {
        //console.log(set.count);
        myDiagram.startTransaction("markSelection");

        //get a deep copy of the set as an array
        var sel = $.extend(true, [], set.toArray());//JSON.parse(JSON.stringify(set.toArray()));
        //initialLinks = JSON.parse(JSON.stringify(linkList));
        for (var i = 0; i < sel.length; i++)
        {
            var obj = sel[i].data;

            if (obj == null)
                continue;
            
            if (obj.diagramObjectType == 'Node') {
                var affectedLinks = [];
                for (var j = 0; j < myDiagram.model.linkDataArray.length; j++) {
                    var link = myDiagram.model.linkDataArray[j];
                    //console.log(link);
                    if (link.to == obj.key || link.from == obj.key) {
                        affectedLinks.push(link);
                    }
                }
               
                for (var j = 0; j < affectedLinks.length; j++) {
                    myDiagram.model.removeLinkData(affectedLinks[j]);
                }

                myDiagram.model.removeNodeData(obj);
            } else if (obj.diagramObjectType == 'Link') {
                myDiagram.model.removeLinkData(obj);
                //console.log('remove: ' + obj.id);
            }

        }
        myDiagram.commitTransaction("markSelection");
    }

    function onDrop(e) {
        $('#' + controlID_popover_add).hide();
    }

    function reOrderLayout() {
        myDiagram.layout.invalidateLayout();
        myDiagram.requestUpdate();
    }

    function rotateDiagram() {
        myDiagram.startTransaction("rotate");
        digraphDirection = (digraphDirection + 90) % 360;
        myDiagram.layout.direction = digraphDirection;
        myDiagram.layout.setsPortSpots = true;
        myDiagram.commitTransaction("rotate");
    }

    function saveChanges() {

        if (readonly) return;

        $('#' + controlID_ribbon_save).jqxButton({ disabled: true });
        // $('#' + controlID_ribbon_save_spinner).show();

        var nodeChanges = getNodeChanges();
        var linkChanges = getLinkChanges();

        var flagError = false;
        var errors = "";
        var processCount = 0;

        for (var i = 0; i < nodeChanges.deleted.length; i++) {
            var node = nodeChanges.deleted[i];
            var data = {
                target: type,
                targetID: id,
                id: node.intersectMapId
            };
            processCount++;
            $.ajax({
                async: false,
                method: 'DELETE',
                url: '/relations/' + data.target + '/' + data.targetID + '/sources/' + data.id
            }).done(function (data, status, xhr) {
                if (!data.success) {
                    flagError = true;
                    errors += data.message;
                    processCount--;
                }
            }).fail(function (xhr, status, error) {
                flagError = true;
                errors += data.message;
                processCount--;
            });

        }

        for (var i = 0; i < linkChanges.added.length; i++) {
            var link = linkChanges.added[i];
            var to = myDiagram.model.findNodeDataForKey(link.to);
            var from = myDiagram.model.findNodeDataForKey(link.from);
            var predicate = link.predicateId;

            var source = {
                target: type,
                targetID: id,
                subject: from.type,
                subjectID: from.id,
                object: to.type,
                objectID: to.id,
                predicateID: link.predicateId
            };

            processCount++;
            $.ajax({
                url: '/Relations/sources',
                async: false,
                data: JSON.stringify(source),
                processData: false,
                type: 'POST',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (data) {
                    if (!data.success) {
                        flagError = true;
                        errors += data.message;
                        processCount--;
                    }
                },
                failure: function (data) {
                    flagError = true;
                    errors += data.message;
                    processCount--;
                }
            });
        }

        for (var i = 0; i < linkChanges.modified.length; i++) {
            var data = {
                intersectMapID: linkChanges.modified[i].id,
                predicateID: linkChanges.modified[i].predicateId
            };

            if (data.intersectMapID == null || data.predicateID == null)
                continue;
            processCount++;
            $.ajax({
                url: '/relations/update/' + data.intersectMapID + '/' + data.predicateID,
                async: false,
                success: function (data) {
                    processCount--;
                },
                failure: function (data) {
                    flagError = true;
                    processCount--;
                }
            });
        }

        if (flagError) {
            amplify.publish("SourceFormStatus", { title: 'An error occurred while saving changes.', message: xhr.statusText + xhr.responseText, success: false });
        } else {
            amplify.publish("SourceSave");
            deletedNodes = [];
            populateDiagram();
            $('#' + controlID_ribbon_save_spinner).hide();
        }

    }

    function checkModified() {
        var nodes = getNodeChanges();
        var links = getLinkChanges();

        if (readonly) {
            $('#' + controlID_message).hide();
        } else if (nodes.deleted.length > 0 ||
            nodes.added.length > 0 ||
            nodes.modified.length > 0 ||
            links.added.length > 0 ||
            links.deleted.length > 0 ||
            links.modified.length > 0) {

            $('#' + controlID_message).show();
            $('#' + controlID_ribbon_save).jqxButton({ disabled: false })
            return true;
        } else {
            $('#' + controlID_message).hide();
            $('#' + controlID_ribbon_save).jqxButton({ disabled: true });
            return false;
        }
    }

    function getLinkChanges() {
        var changes = {
            added: [],
            modified: [],
            deleted: []
        };
        var links = myDiagram.model.linkDataArray;


        for (var i = 0; i < links.length; i++) {
            var found = false;
            for (var j = 0; j < initialLinks.length; j++) {
                if (initialLinks[j].to == links[i].to && initialLinks[j].from == links[i].from) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.added.push(links[i]);
            }
        }

        for (var i = 0; i < initialLinks.length; i++) {
            var found = false;
            for (var j = 0; j < links.length; j++) {
                if (initialLinks[i].to == links[j].to && initialLinks[i].from == links[j].from) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                //console.log('not found');
                changes.deleted.push(initialLinks[i]);
            }
        }

        for (var i = 0; i < initialLinks.length; i++) {
            var found = false;
            for (var j = 0; j < links.length; j++) {
                if (initialLinks[i].from == links[j].from && initialLinks[i].to == links[j].to) {

                    var l1 = (initialLinks[i].predicateId || '').toString();
                    var l2 = (links[j].predicateId || '').toString();

                    //console.log(l1 + ', ' + l2);
                    if (l1 != l2)
                    {
                        found = true;
                        //console.log(l2);
                    }
                        
                    break;
                }
            }
            if (found) {
                
                //console.log('"' + initialLinks[i].predicateId + '"');
                //console.log('"' + links[j].predicateId + '"');
                changes.modified.push(links[j]);
            }
        }

        return changes;
    }

    function getNodeChanges() {
        var changes = {
            added: [],
            modified: [],
            deleted: []
        };

        var nodes = myDiagram.model.nodeDataArray;

        //added
        for (var i = 0; i < nodes.length; i++) {
            var found = false;
            for (var j = 0; j < initialNodes.length; j++) {
                if (initialNodes[j].key == nodes[i].key) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.added.push(nodes[i]);
            }
        }

        //deleted
        for (var i = 0; i < initialNodes.length; i++) {
            var found = false;
            for (var j = 0; j < nodes.length; j++) {
                if (initialNodes[i].key == nodes[j].key) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                changes.deleted.push(initialNodes[i]);
            }
        }

        //modified
        for (var i = 0; i < nodes.length; i++) {
            for (var j = 0; j < initialNodes.length; j++) {
                if (initialNodes[j].id == nodes[i].id) {

                    if (initialNodes[j].key === nodes[i].key) {
                        //changes.modified.push(nodes[i]);
                        break;
                    }
                }
            }
        }

        //console.log(changes);
        return changes;

    }

    //#endregion

    var g = go.GraphObject.make;
    var myDiagram = initializeDiagram();

    function initializeDiagram() {
        var dg = g(go.Diagram, controlID_diagram, {
                    initialContentAlignment: go.Spot.Left,
                    //autoScale: go.Diagram.UniformToFill,
                    allowDrop: true,
                    initialAutoScale: go.Diagram.UniformToFill,
                    scrollMode: go.Diagram.DocumentScroll,
                    initialPosition: new go.Point(125, 125),
                    layout: g(go.LayeredDigraphLayout, { direction: 0, columnSpacing: 50, layerSpacing: 50 }),
                    "undoManager.isEnabled": true
                });
        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = "template";
        dg.model.linkFromPortIdProperty = "frompid";
        dg.model.linkToPortIdProperty = "topid";
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !readonly;
        dg.model.isReadOnly = readonly;
        //dg.isReadOnly = readonly;

        return dg;
    }

    $('#' + controlID_overlay_predicates).on('change', function (e) {
        var checked = true;//$('#' + controlID_overlay_radio_existing).jqxRadioButton('checked');
        if (checked) {
            if ($(this).val() == 0) {
                $('#' + controlID_overlay_add).prop('disabled', true);
            } else {
                $('#' + controlID_overlay_add).prop('disabled', false);
            }
        }
    });

    $('#' + controlID_wrapper).on('mouseup', function () {
        var height = $('#' + controlID_wrapper).height();
        if (height != oldHeight) {
            $('#' + controlID_diagram).height(height - 20);
            oldHeight = height;
            //force browser to update layout before executing
            setTimeout(function () {
                myDiagram.requestUpdate();
            }, 0);
            
        }
    });

    $('#' + controlID_ribbon_zoom_100).on('click', function () {
        $('#' + controlID_ribbon_zoom_slider).val(1500);
        myDiagram.scale = 1.0;
        $('#' + controlID_ribbon_zoom_text).text('100%');
    });

    $('#' + controlID_ribbon_zoom_fit).on('click', function () {
        myDiagram.zoomToFit();
        $('#' + controlID_ribbon_zoom_text).text(Math.round(myDiagram.scale * 100) + '%');
    });

    $('#' + controlID_ribbon_reset).on('click', function () {
        //type = originalObject;
        //id = originalObjectID;
        //populateDiagram();
        if (checkModified()) {
            var message = $('<p />', { text: 'Are you sure you want to reset the diagram? You have unsaved changes' }),
             ok = $('<button />', {
                 text: 'Reset',
                 'class': 'btn qtip-blue qtip-btn-inline'
             }),
             cancel = $('<button />', {
                 text: 'Cancel',
                 'class': 'btn qtip-blue qtip-btn-inline'
             });
            confirmDialog(message.add(ok).add(cancel), 'Reset Diagram?', 'Reset', function () {
                type = originalObject;
                id = originalObjectID;
                populateDiagram();
            });
        } else {
        type = originalObject;
        id = originalObjectID;
        populateDiagram();
        }
    });

    //#endregion

    myDiagram.addDiagramListener('ViewportBoundsChanged', onViewportBoundsChanged);
    myDiagram.addDiagramListener('ChangedSelection', onSelectionChange);
    myDiagram.addDiagramListener('ObjectDoubleClicked', onDoubleClick);
    myDiagram.addDiagramListener('LayoutCompleted', onLayoutCompleted);
    myDiagram.addDiagramListener('LinkDrawn', onLinkDrawn);
    myDiagram.addDiagramListener('SelectionDeleting', onDeleting);
    myDiagram.addDiagramListener('SelectionDeleted', onDeleted);
    myDiagram.addDiagramListener('ExternalObjectsDropped', onDrop);
    myDiagram.model.addChangedListener(onChange);

    myDiagram.grid.visible = false;
    myDiagram.grid.gridCellSize = new go.Size(8, 8);
    myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
    myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

    makeTemplate("FocalArtifact", 275, 150, '#000000', 14, [makePort("IN", true)], [makePort("OUT", false)]);
    makeTemplate("Artifact", 225, 105, 'transparent', 10, [makePort("IN", true)], [makePort("OUT", false)]);

    myDiagram.linkTemplate = g(
        go.Link, { routing: go.Link.AvoidsNodes, curve: go.Link.JumpOver, corner: 10, relinkableFrom: false, relinkableTo: false }, // the whole link panel
        g(go.Shape, { stroke: "gray", strokeWidth: 2 }), // the link shape
        g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" }), // the arrowhead
        g(go.Panel, "Auto",
            g(go.Shape, { visible: false, fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }), stroke: null },
                //only visible if there's a label
                new go.Binding("visible", "text", function (a) { return (a ? true : false) })
            ), // the link shape
            g(go.TextBlock, { textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4 },   // the label
                new go.Binding("text", "text").makeTwoWay()
             )
        )
    );
    
    var myPalette = initializePalette();

    function initializePalette() {

        var pl = g(go.Palette, controlID_palette, {
            contentAlignment: go.Spot.TopCenter
            , allowDrop: true
            , initialAutoScale: go.Diagram.Uniform
            , model: new go.GraphLinksModel([{ template: "Artifact", backColor: 'black', foreColor: 'white', name: '', id: -1, key: -1, typeName: '', type: '', isDeletable: true }])
        })
        pl.model.nodeCategoryProperty = 'template';
        pl.model.nodeDataArray = [];
        pl.model.class = 'go.GraphLinksModel';
        pl.layout.spacing = new go.Size(3,3);
        return pl;
    }

    makeSearchTemplate();
    
    var myOverview = initializeOverview(myDiagram);

    function initializeOverview(diagram) {
        var ov = g(go.Overview, controlID_overview,
        { observed: diagram, contentAlignment: go.Spot.Center });

        return ov;
    }

    function parseData(data) {
        myDiagram.startTransaction("load_all_data");
        myDiagram.model.nodeDataArray = [];
        myDiagram.model.linkDataArray = [];
        initialNodes = [];
        initialLinks = [];
        var modelList = [];
        var linkList = [];
        $('#' + controlID_message).hide();

        for (var i = 0; i < data.nodes.length; i++) {

            var d = data.nodes[i];
            var model = createNodeModel();

            var isFocalPoint = (d.obj == type && d.objid == id);// && d.level == 0);

            if (isFocalPoint) {
                $('#' + controlID_header).text('Lineage: ' + d.name);
            }

            model.template = isFocalPoint ? "FocalArtifact" : "Artifact";
            model.key = d.key;
            model.id = d.objid;
            model.type = d.obj;
            model.level = d.level;
            model.name = d.name;
            model.typeName = d.type;
            model.foreColor = d.fore;
            model.backColor = d.back;
            model.diagramObjectType = "Node";
            model.exclude = d.exclude.toString();
            model.intersectMapId = d.intersectMapId;
            model.intersectId = d.intersectId;
            model.sourceRuleCount = d.sourceRuleCount;
            model.isVisible = (d.sourceRuleCount > 0);
            modelList.push(model);
        }

        for (var i = 0; i < data.links.length; i++) {
            var d = data.links[i];
            var link = createLinkModel();
            link.id = d.id;
            link.key = d.id;
            link.from = d.from;
            link.to = d.to;
            link.text = d.text;
            link.predicateId = d.predicateId;
            link.diagramObjectType = "Link";
            linkList.push(link);
        }

        for (var i = 0; i < modelList.length; i++) {
            myDiagram.model.addNodeData(modelList[i]);
        }
        for (var i = 0; i < linkList.length; i++) {
            myDiagram.model.addLinkData(linkList[i]);
        }       

        //get deep copy of lists
        initialNodes = $.extend(true, [], modelList);//JSON.parse(JSON.stringify(modelList));
        initialLinks = $.extend(true, [], linkList); //JSON.parse(JSON.stringify(linkList));


        myDiagram.commitTransaction("load_all_data");
        reOrderLayout();

    }


    function populateDiagram() {
        var results = $.ajax({
                url: '/relations/' + type + '/' + id + '/sources',
            data: null,
            success: function (data) {
                //console.log('populate');
                //myDiagram = initializeDiagram();
                parseData(data);
                reOrderLayout();
                myDiagram.zoomToFit();
            }
        });
    }
    populateDiagram();


    function populateTypeSelectList() {

        $.ajax({
            url: '/services/glossary/artifacts',
            data: null,
            success: function (data) {
                $('#' + controlID_add_artifact_type).html('');
                var output = [];
                for (var i = 0; i < data.length; i++) {
                    output.push('<option value="' + data[i].ID + '">' + data[i].Name + '</option>');
                }
                $('#' + controlID_add_artifact_type).html(output.join(''));
            }
        });
    }
    populateTypeSelectList();


    function getArtifact() {
        var data = {
            id: $('#' + controlID_add_artifact_type).val(),
            search: $('#' + controlID_add_search_text).val()
        };

        $.ajax({
            url: '/Diagrams/getArtifact',
            data: data,
            success: function (data) {
                $('#' + controlID_add_search_message).hide();
                myPalette.model.nodeDataArray = [];
                temp = data[0];
                for (var i = 0; i < data.length; i++) {
                    data[i].template = "Artifact";
                    data[i].type = data[i].objectType;
                    data[i].key = data[i].type + data[i].id.toString();
                    data[i].isDeletable = true;
                    myPalette.model.addNodeData(data[i]);
                }
                if (data.length < 1) {
                    $('#' + controlID_add_search_message).show();
                }
            },
            async: false
        });

        if (temp == null) {
            return;
        }
        var data = {
            type1: $('#' + controlID_add_artifact_type + ' option:selected').text(),
            type2: ''
        };

        //for (var i = 0; i < myDiagram.model.nodeDataArray.length; i++) {
        //    if (myDiagram.model.nodeDataArray[i].key == $('#ddlRel').val()) {
        //        data.type2 = myDiagram.model.nodeDataArray[i].type;
        //        break;
        //    }
        //}
        populatePredicateList();
        myPalette.scale = 1.0;
    }

    $('#' + controlID_add_search).on('click', getArtifact);
    $('#' + controlID_overlay_cancel).on('click', cancelAddLink);
    $('#' + controlID_overlay_add).on('click', addRelationship);
    $('#' + controlID_popover_add).on('keypress', '#' + controlID_add_search_text, function (e) {
        if (e.keyCode == 13) {
            $('#' + controlID_add_search).click();
            return false;
        }
    });

    function populatePredicateList() {
        $.ajax({
            url: '/diagrams/getpredicateinfo?type=1',
            success: function (data) {
                var output = [];
                predicates = [];

                output.push('<option value="0_1"></option>');
                for (var i = 0; i < data.length; i++) {
                    
                    output.push('<option value="' + data[i].id.toString() + '">' + data[i].name + '</option>');
                    predicates.push(data[i]);
                }
                $('#' + controlID_overlay_predicates).html(output.join(''));
            }
        });
    }

    function cancelAddLink() {
        if (newLink != null) {
            if (overlayEditLinkKey != null) {
                overlayEditLinkKey = null;
            } else {
                myDiagram.startTransaction("removeLink");
                myDiagram.model.removeLinkData(newLink);
                myDiagram.commitTransaction("removeLink");
            }
            newLink = null;
        }

        resetOverlay();
        $('#' + controlID_overlay).hide();
    };

    function resetOverlay() {
        $('#' + controlID_overlay_predicates).val(0);
        $('#' + controlID_overlay_add).prop('disabled', true);
    }

    function findLinkDataForKey(key) {
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].key == key)
                return myDiagram.model.linkDataArray[i];
        }
    }


    function addRelationship() {

        var id = $('#' + controlID_overlay_predicates).val();
        var text = null;

        for (var i = 0; i < predicates.length; i++) {
            if (predicates[i].id.toString() == id.toString()) {
                text = predicates[i].name;
            }
        }
        //console.log(overlayEditLinkKey);

        myDiagram.startTransaction("nameRelationship")
        if (overlayEditLinkKey != null) {
            var link = findLinkDataForKey(overlayEditLinkKey);
            myDiagram.model.setDataProperty(link, 'text', text);
            myDiagram.model.setDataProperty(link, 'predicateId', id);

            //get the id if possible (if link is deleted and re-added)
            for (var i = 0; i < initialLinks.length; i++) {
                if (initialLinks[i].from == link.from && initialLinks[i].to == link.to) {
                    link.id = initialLinks[i].id;
                    //console.log('intersectMapID: ' + link.id);
                    myDiagram.model.setDataProperty(link, 'id', initialLinks[i].id);
                }
            }
        } else {
            newLink.predicateId = id;
            newLink.text = text;
            newLink.diagramObjectType = "Link";
            newLink.isDeletable = true;
            newLink.id = overlayEditLinkKey;
            newLink.key = overlayEditLinkKey;
            if (newLink.id == null) {
                for (var i = 0; i < initialLinks.length; i++) {
                    if (initialLinks[i].from == newLink.from && initialLinks[i].to == newLink.to) {
                        newLink.id = initialLinks[i].id;
                        newLink.key = initialLinks[i].key;
                       // console.log('intersectMapID: ' + link.id);
                        //myDiagram.model.setDataProperty(link, 'id', initialLinks[i].id);
                    }
                }
            }

            var index = -1;

            for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
                if (myDiagram.model.linkDataArray[i].from == newLink.from &&
                    myDiagram.model.linkDataArray[i].to == newLink.to) {
                    myDiagram.model.removeLinkData(myDiagram.model.linkDataArray[i]);
                    break;
                }
            }

            myDiagram.model.addLinkData(newLink);
        }


        myDiagram.commitTransaction("nameRelationship");
        $('#' + controlID_overlay).hide();
        newLink = null;
        overlayEditLinkKey = null;

        resetOverlay();

    };

    amplify.subscribe("SaveAction", function (saveActionEventData) {
        try {
            switch (saveActionEventData.context) {
                case contextList.SourceToTarget:
                    populateDiagram();
                    break;
            }
        } catch (e) {
            logError("artifact.item : SaveAction", e);
        }
    });
}


function SearchResultsGrid(contextList, defaultItemsPerPage,initialPhrase) {
    var phrase;
    var searchSource;
    var loadCategories;
    var searchVm;
    var self = this;
    var advSearchText;

    mainCtrlId = 'SearchArea';
    categoriesCtrlId = 'CategoryResults';
    resultsCtrlId = 'SearchResults';
    if (defaultItemsPerPage === undefined) defaultItemsPerPage = 10;
    if (initialPhrase !== undefined) phrase = initialPhrase;
    

    var resultsctrl = '#' + resultsCtrlId;
    var categoryctrl = '#' + categoriesCtrlId;

    searchVm = new SearchViewModel();
    try {
        ko.applyBindings(searchVm, document.getElementById(mainCtrlId));
    }
    catch (e) {
        console.log(e);
    }

    //#region Event Registration

    
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion

    loadCategories = true;

    if ($("#SearchString").val().length == 0 && phrase !== undefined && phrase.length > 0)
        $("#SearchString").val(phrase);

    phrase = $("#SearchString").val();

    var source = getSource(phrase, '', '');

    var dataAdapter = getDataAdapter(source);

    //region Event Handlers

    function unsubscribe(data) {
        searchVm = null;        
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }
    
    //#endregion

    $(resultsctrl).jqxDataTable(
    {
        pageable: true,
        pagerButtonsCount: 10,
        serverProcessing: true,
        pagerMode: 'default',
        source: dataAdapter,
        theme: 'transparent',
        width: '98%',      
        enableHover: false,
        showHeader: false,
        columns: [
            { text: ' ', dataField: 'Merged', width: '99%' }           
        ]
    });

    self.doSearch = function (val) {
        phrase = val;
        advSearchText = '';

        $(resultsctrl).show();
        loadCategories = true;

        var searchSource = getSource(phrase, '', '');

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable('goToPage', 0);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    self.doAdvancedSearch = function () {
        advSearchText = searchVm.advancedFilterJSON();
        phrase = '';
        
        $(resultsctrl).show();
        loadCategories = true;

        var searchSource = getSource(phrase, '', '', advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable('goToPage', 0);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    self.showAdvanced = function (text) {
        searchVm.showAdvanced(text);
    }

    var showOnlyRelevantType = function (categoryType, e) {
        $(categoryctrl + ' a').removeClass('selected');
        $(e.target).addClass('selected');

        var searchSource = getSource(phrase, '', categoryType == 'All' ? '' : categoryType, advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    var showOnlyRelevantCategory = function (category, e) {
        $(categoryctrl + ' a').removeClass('selected');
        $(e.target).addClass('selected');

        var searchSource = getSource(phrase, category, '', advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    function getSource(term, selGroup, selType, advCriteria) {
        return {
            datatype: "json",
            pagesize: defaultItemsPerPage,
            datafields: [
                { name: 'NormalizedScore', type: 'float' },
                { name: 'Name', type: 'string' },
                { name: 'Type', type: 'string' },
                { name: 'Group', type: 'string' },
                { name: 'Description', type: 'string' },
                { name: 'ID', type: 'number' },
                { name: 'Url', type: 'string' },
                { name: 'Merged', type: 'string' },
            ],
            type: 'POST',
            dataType: 'json',
            url: '/search/results',
            data: { search: term, from: 0, size: defaultItemsPerPage, group: selGroup, type: selType, adv: (advCriteria === undefined ? '' : advCriteria) },
            id: 'ID',
            sortcolumn: 'NormalizedScore',
            sortdirection: 'desc',
            root: "Results",
        };
    }

    function getDataAdapter(source) {
        return new $.jqx.dataAdapter(source,
                {
                    formatData: function (data) {
                        data.from = data.pagenum * data.pagesize;
                        data.size = data.pagesize;
                        return data;
                    },
                    downloadComplete: function (data, status, xhr) {
                        if (!source.totalRecords) {
                            source.totalRecords = parseInt(data.Result.Matches);
                            if (source.totalRecords > 10000) source.totalRecords = 10000;
                        }
                    },
                    loadComplete: function (data) {
                        var msg = "";

                        if (data) {
                            if (data.Result.Matches == 0) {
                                $(resultsctrl).hide();
                                searchVm.elapsedTime("No search results found for the specified search term.");
                            }
                        }

                        if (loadCategories) {
                            msg = 'Search found ' + data.Result.Matches.toLocaleString() + ' matches in (' + (data.Result.ElapsedMS / 1000) + ' seconds)' + (data.Result.Matches > 10000 ? '  results limited to first 10,000 items.' : '');
                            searchVm.elapsedTime(msg);

                            data.Categories.unshift({ Name: 'All', ResultCount: data.Result.Matches, DisplayName: 'All' });
                            var cats = $.map(data.Categories, function (item) { return new SearchResultCategory(item); });
                            searchVm.categories(cats);

                            $('.search-category-link').each(function () {
                                $(this).click(function (e) {
                                    var c = $(this).data("category");
                                    showOnlyRelevantCategory(c, e);
                                });
                            });

                            $('.search-type-link').each(function () {
                                $(this).click(function (e) {
                                    var c = $(this).data("category-type");
                                    showOnlyRelevantType(c, e);
                                });
                                if ($(this).data("category-type") == "All") $(this).addClass('selected');
                            });

                            loadCategories = false;
                        }
                    },
                    loadError: function (xhr, status, error) {
                        throw new Error(error.toString());
                    },
                    beforeLoadComplete: function (records) {
                        var data = new Array();
                        for (var i = 0; i < records.length; i++) {
                            var row = records[i];
                            row.Merged = "<div class='search-res-container'><h4 class='search-result-name'><a href='/" + row.Url + "' class='search-result-link'>" + row.Name + "</a></h4><p class='search-result-desc'>" + (row.Description != null ? row.Description : "") + "</p><h5 class='search-result-attributes'>Category: <em class='result-category'>" + row.Type + "</em> &nbsp;&nbsp;Type: <em class='result-type'>" + row.Group + "</em></h5></div>";
                            data.push(row);
                        }

                        return data;
                    }
                }
            );
    }
}