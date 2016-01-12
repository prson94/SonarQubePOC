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
    var detailControlID = controlID + "AttributeDetail";
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
    html += '<div id="' + detailControlID + '"></div>';
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
    detailControlID = '#' + detailControlID;
    editorControlID = '#' + editorControlID;

    //#endregion

    //#region Clean up previous control logic before re-creating

    //try { $(treeControlID).unbind('rowSelect'); } catch (e) { }
    try { amplify.unsubscribe('AttributeToolAction'); } catch (e) { } 
    try { amplify.unsubscribe('CancelAction'); } catch (e) { } 
    try { amplify.unsubscribe('SaveAction'); } catch (e) { } 
    try { $(treeControlID).jqxTreeGrid('destroy'); } catch(e){}
    //try { $(toolbarControlID).AttributeToolbar('destroy'); } catch (e) { }
    try { $(detailControlID).Detail('destroy'); } catch (e) { }
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
        $(detailControlID).Detail('clear');
        $(viewerControlID).hide();
        $(editorControlID).fadeIn();
        $(editorControlID).load(uri);
    }

    var attributeSwitchToViewer = function (t, i) {
        $(editorControlID).html('');
        if (t && i) {
            $(detailControlID).Detail('reload', t, i);
        }
        else {
            $(detailControlID).Detail('clear');
        }
        $(viewerControlID).fadeIn();
        $(editorControlID).hide();
    }

    $(detailControlID).Detail({ context: contextList.Attribute, prefix: 'Attribute', listenfortoolactions: false });

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
                    $(treeControlID).jqxTreeGrid('selectRow', (rows[0].Items[0] !== null ? rows[0].Items[0].uid : rows[0].uid));
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
        var badge = $('#DetailsTile_AttributeBadge');
        if (badge) {
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
            if (count > 0) {
                badge.show();
                badge.text(count);
            }
            else {
                badge.text('');
                badge.hide();
            }
        }
    }

    function treeControlRowSelect(evt) {
        try {
            // event args.
            var args = evt.args;
            // row data.
            var row = args.row;
            // row key.
            var key = args.key;

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
                $(detailControlID).Detail('reload', detailtype, detailid);
            }
            else {
                $(detailControlID).Detail('clear');
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

function DetailsTile(controlID, contextList, permissions, type, id, context, shouldHideSynonyms) {
    controlID = '#' + controlID;
    var _DetailSubTile = '#DetailSubTile';
    var _SynonymsSubTile = '#SynonymsSubTile';
    var _AttributesSubTile = '#AttributesSubTile';

    var source = shouldHideSynonyms ? $("#detailTileNoSynonymsTmpl").html() : $("#detailTileTmpl").html();
    var template = Handlebars.compile(source);
    $(controlID).html(template({}));

    $(controlID).addClass('tile');
    $(controlID).addClass('tile-detail');

    var model = function () {
        var self = this;
        self.RibbonIndex = 0;

        self.G_TargetType = '';
        self.G_TargetID = -1;
        self.S_TargetType = '';
        self.S_TargetID = -1;
        self.A_TargetType = '';
        self.A_TargetID = -1;

        self.updateRibbonData = function (selectedIndex) {
            if (selectedIndex !== undefined)
                self.RibbonIndex = selectedIndex;

            switch (self.RibbonIndex) {
                case 0:
                    //#region
                    if ((self.G_TargetType != type) || (self.G_TargetID != id)) {
                        self.G_TargetType = type;
                        self.G_TargetID = id;
                    }
                    //#endregion
                    break;
                case 1:
                    //#region
                    if ((self.S_TargetType != type) || (self.S_TargetID != id)) {
                        self.S_TargetType = type;
                        self.S_TargetID = id;
                    }
                    //#endregion
                    break;
                case 2:
                    //#region
                    if ((self.A_TargetType != type) || (self.A_TargetID != id)) {
                        self.A_TargetType = type;
                        self.A_TargetID = id;
                        AttributesTile('AttributesSubTile', contextList, permissions, self.A_TargetType, self.A_TargetID, '', false);
                    }
                    //#endregion
                    break;
            }
        }
    };

    //$('#DetailTileTabs').tabs();

    var m = new model();

    //#region Detail Sub Tile

    $(_DetailSubTile).Detail({ type: type, id: id, context: context });

    //#endregion

    //#region Synonyms Grid

    if (!shouldHideSynonyms) {
        $('#SynonymsExpander').jqxExpander({ theme: theme, expanded: false });

        var srcSynonym = {
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

        var adapterSynonym = new $.jqx.dataAdapter(srcSynonym);

        $(_SynonymsSubTile).jqxGrid({
            source: adapterSynonym,
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

    //#endregion

    $('#AttributesExpander').jqxExpander({ theme: theme, expanded: false });
    //AttributesTile('AttributesSubTile', contextList, permissions, type, id, '', false);

    //#region Events

    function attributesExpanded() {
        AttributesTile('AttributesSubTile', contextList, permissions, type, id, '', false);
    }

    function synonymsExpanded() {
        if (!shouldHideSynonyms) {
            $(_SynonymsSubTile).jqxGrid('updatebounddata');
        }
    }

    function pageResized() {
        try {
            if (!shouldHideSynonyms) {
                $(_SynonymsSubTile).jqxGrid('refresh');
            }
        } catch (e) {
        }
    }

    function ribbonSelect() { //(event) {
        var tab = $(this);
        var ix = tab.data('index'); //event.args.selectedIndex
        m.updateRibbonData(ix);
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case context:
                    $(_DetailSubTile).Detail('reload', type, id);
                    break;
                case contextList.Attribute:
                    if (data.custom) {
                        if (data.custom.AttributeTypeID === 1) {
                            $(_SynonymsSubTile).jqxGrid('updatebounddata');
                        }
                    }
                    break;
                case contextList.Synonym:
                    if (!shouldHideSynonyms) {
                        $(_SynonymsSubTile).jqxGrid('updatebounddata');
                    }
                    break;
            }
        } catch (e) {
            logError("Parts.js : TagsTile : SaveAction", e);
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        //amplify.unsubscribe('CommandExecuted', commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);

        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        //$('#DetailTileTabs > li > a').off('click', ribbonSelect);
        $('#AttributesExpander').off('expanded', attributesExpanded)
        if (!shouldHideSynonyms) {
            $('#SynonymsExpander').off('expanded', synonymsExpanded);
        }
    }

    //amplify.subscribe("CommandExecuted", commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    $('#AttributesExpander').on('expanded', attributesExpanded);
    //$('#DetailTileTabs > li > a').on('click', ribbonSelect);

    if (!shouldHideSynonyms) {
        $('#SynonymsExpander').on('expanded', synonymsExpanded);
        $(_SynonymsSubTile).on('bindingcomplete', function (event) {
            var count = 0;
            try {
                count = $(_SynonymsSubTile).jqxGrid('getrows').length;
            } catch (e) {
                count = 0;
            }
            if (count > 0) {
                $('#SynonymsCount').html("&#160;(<b>" + count + "</b>)")
            }
            else {
                $('#SynonymsCount').html("")
            }
        });
    }

    //#endregion
}

function DetailTile(controlID, contextList, permissions, type, id, hideTitle) {
    var ribbonControlID = controlID + "_ribbon";
    //var toolsControlID = controlID + "_tools";
    var fieldControlPrefix = "det" + controlID;
    controlID = '#' + controlID;

    if (!$(controlID).hasClass('tile'))
    {
        $(controlID).css('min-height', 125);
        $(controlID).addClass('tile');
        $(controlID).addClass('tile-detail');
        //$(controlID).append('<div id="' + toolsControlID + '"></div>');

        //toolsControlID = '#' + toolsControlID;
        //TileTools(toolsControlID, [
        //        { icon: 'plus', uri: '/relations/RelationOverlay?type=' + type + '&id=' + id, context: contextList.Intersect, title: 'Add field' }
        //]);
    }

    //#region Parse

    var parseSectionHeaders = function (sections) {
        try {
            var html = '<div class="col s12">';
            if (sections.length > 1) {
                html += '<ul id="DetailsTabControl" class="tabs">';
                $.each(sections, function (idx, v) {
                    html += '<li class="tab col s3">';
                    html += '<a';
                    if (idx === 0) {
                        html += ' class="active"';
                    }
                    html += ' href="#Section' + v.ID + '">' + v.Name + '</a>';

                    html += '</li>';
                });
                html += '</ul>';
            }
            html += '</div>';
            return html;
        } catch (e) {
            logError("parts.js : DetailTile.parseSectionHeaders", e);
        }
    }

    var getFieldHtmlForColumn = function (fields, row, column, sectionID) {
        var html = "";

        $.each(fields, function (idx, f) {
            if (f.Row === row && f.Column === column) {
                var fieldFriendlyName = f.Name;
                if (f.ScriptProperty) {
                    fieldFriendlyName = eval(f.ScriptProperty);
                }

                html += "<div id='" + fieldControlPrefix + f.FieldName + "' class='FieldName FieldDisplayName'><span id='Tip_" + sectionID + "_" + f.FieldName + "'>" + fieldFriendlyName + "</span></div>";
                if (f.TooltipContext && f.TooltipID && f.TooltipType && f.TooltipUrl) {
                    html += "<div><a href='" + f.TooltipUrl +
                        "' data-type='" + f.TooltipType +
                        "' data-context='" + f.TooltipContext +
                        "' data-id='" + f.TooltipID + "'>" +
                        f.Value + "</a></div>";
                }
                else {
                    html += "<div>" + f.Value + "</div>";
                }
                return false;
            }
        });

        return html;
    }

    var parseSectionFields = function(sections) {
        try {
            var html = "";

            $.each(sections, function (idx, section) {

                html += '<div class="col s12" id="Section' + section.ID + '">';

                //#region Finds the row count/ column count

                var tableMatrix = [];
                var currentRow = 0;
                var tabMatrixItem = null;
                $.each(section.Fields, function (idx, v) {
                    if (v.Row) {
                        if (v.Row !== currentRow) {
                            if (tabMatrixItem) tableMatrix.push(tabMatrixItem);
                            currentRow = v.Row;
                            tabMatrixItem = { Row: currentRow, Columns: 0, ColumnCount: 0 };
                        }
                        if (v.Column) {
                            if (tabMatrixItem.ColumnCount < v.Column) {
                                tabMatrixItem.ColumnCount = v.Column;
                                tabMatrixItem.Columns = Math.round(12 / v.Column);
                            }
                        }
                    }
                });
                if (tabMatrixItem) tableMatrix.push(tabMatrixItem);   //Add the last item to make sure we get the last row.

                //#endregion

                //#region Build the HTML

                var currentColumn = 0;
                $.each(tableMatrix, function (i, m) {
                    html += "<div class='row'>";

                    currentColumn = 1;
                    while (currentColumn <= m.ColumnCount) {
                        html += "<div class='col s" + m.Columns + "'>";
                        html += getFieldHtmlForColumn(section.Fields, m.Row, currentColumn, section.ID);
                        html += "</div>";
                        currentColumn++;
                    }

                    html += "</div>";
                });

                //#endregion

                html += '</div>';

            });

            return html;

        } catch (e) {
            logError("parts.js : DetailTile.parseSectionFields", e);
        }
    }

    var parseTooltips = function (sections) {
        try {

            $.each(sections, function (idx, section) {

                $.each(section.Fields, function (idx, field) {

                    if (field.FieldDescription && field.FieldDescription !== '') {
                        $('#Tip_' + section.ID + '_' + field.FieldName).qtip({
                            content: {
                                text: field.FieldDescription,
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

                });

            });

        } catch (e) {
            logError("parts.js : DetailTile.parseTooltips", e);
        }
    }

    //#endregion

    $.ajax('/api/' + type + '/' + id + '/detail', {
        type: 'GET'
    }).done(function (data, status, xhr) {
        var html = '';
        html += parseSectionHeaders(data);
        html += parseSectionFields(data);

        $(controlID).html('');
        if (!hideTitle) {
            $(controlID).append('<header>Definition</header>');
        }
        $(controlID).append('<div id="' + ribbonControlID + '" class="row"></div>');
        ribbonControlID = '#' + ribbonControlID;
        $(ribbonControlID).fadeOut(100);
        $(ribbonControlID).append(html);

        parseTooltips(data);

        $('#DetailsTabControl').tabs();
    }).fail(function (xhr, status, error) {
        $(ribbonControlID).append('An error occured while trying to poll for object details: ' + error);
    }).always(function () {
        $(ribbonControlID).fadeIn(250);
    });
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

    //if (!title) {
        title = 'Field Definition';
    //}

    try {
        $(controlID).html('<header>' + title + '<div id="' + toolsControlID + '"></div></header>' + '<div id="' + gridControlID + '"></div>');
        gridControlID = '#' + gridControlID;
        toolsControlID = '#' + toolsControlID;

        source = {
            datatype: 'json',
            url: '/fields/' + type + '/' + id + '.json',
            datafields:
            [
                { name: 'Object' },
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

function _FusionItemsGrid(controlID, fusionTypeID, fusionID, defaultTypeDefinition, definition, dataUri,id) {
    var gridControlID = controlID + "_grid";
    controlID = '#' + controlID;
    $(controlID).html('<div id="' + gridControlID + '"></div>');
    gridControlID = '#' + gridControlID;

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
        
        _FusionItemsGrid(_innercontrolID, fusionTypeID, fusionID, typeDefinition, typeDefinition, '/fusion/ItemsByParent?fusionTypeID=' + fusionTypeID + '&fusionID=' + fusionID + '&parentType=FusionAttributeType&parentID=' + initialData.FusionAttributeTypeID + '&parentFusionAttributeID=' + initialData.ParentID);
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
                { icon: 'plus', uri: "/form/AddPeopleResponsibility?type=" + type + "&id=" + id, context: contextList.Responsibility, title: 'Add responsibility' }
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
                { name: 'AssigningIconBackColor' },
                { name: 'AssigningIconForeColor' },
                { name: 'AssigningIconText' },
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
                }//,
                //{ datafield: "ContextItems", text: "Context", hidden: (summaryOnly) },
                //{ datafield: "ResponsibilityID", text: "", width: '80px', filterable: false, sortable: false, hidden: (summaryOnly),
                //  cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                //        var tools = [];

                //        if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID) {
                //            if (permissions.HasPermission("Governance", "Update")) {
                //                tools.push({ icon: 'pencil', urlprefix: '/form/EditPeopleResponsibility?id={0}' });
                //            }
                //            if (permissions.HasPermission("Governance", "Delete")) {
                //                tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteResponsibility?id={0}' });
                //            }
                //        }

                //        return renderToolsHtml(value, tools, contextList.Responsibility, data);
                //    }
                //}
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

    try {
        controlID = '#' + controlID;

        parent = $(controlID);

        var html = "";
        html += "<header>Relationships<div id='" + toolsControlID + "'></div></header>";
        html += "<table style='width: 100%'>";//"<div class='row'>";
        html += "<tr>";
        html += "<td style='width: 50%'><div id='AggregateTileChart1' class='col s6' style='margin: auto; width: 100%'></div></td>";
        html += "<td style='width: 50%'><div id='AggregateTileChart2' class='col s6' style='margin: auto; width: 100%'></div></td>";
        html += "</tr>";
        html += "<tr>";
        html += "<td colspan='2' style='margin: auto; width: 100%'><div id='AggregateTileChart3' class='col s12' style='width: 60%'></div></td>";
        html += "</tr>";
        html += "</table>";//"</div>";

        parent.html(html);

        toolsControlID = '#' + toolsControlID;
        if (permissions.HasPermission("Relationship", "Update")) {
            TileTools(toolsControlID, [
                    { icon: 'pencil', uri: '/relations/RelationOverlay?type=' + type + '&id=' + id, context: contextList.Intersect, title: 'Manage Relationships' }
            ]);
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
                var cht = $('#AggregateTileChart' + this);
                if (nodes.length <= 0) {
                    cht.css('height', '40px');
                    cht.html('No data to display');
                }
                else {
                    var groupName = nodes[0].GroupName;
                    var critical = nodes[0].Critical;
                    cht.css('height', '300px');
                    cht.jqxChart({
                        source: nodes,
                        title: groupName,
                        description: '',
                        enableAnimations: false,
                        showLegend: true,
                        showBorderLine: false,
                        legendLayout : { flow: 'horizontal' },
                        //padding: { left: 5, top: 5, right: 5, bottom: 5 },
                        //titlePadding: { left: 0, top: 0, right: 0, bottom: 10 },
                        seriesGroups: [
                            {
                                useGradientColors: false,
                                type: 'pie',
                                showLegend: true,
                                enableSeriesToggle: true,
                                series: [
                                    {
                                        dataField: 'Count',
                                        displayText: 'TypeName',
                                        showLabels: true,
                                        //labelRadius: 125,
                                        labelLinesEnabled: true,
                                        labelLinesAngles: true,
                                        labelsAutoRotate: false
                                        //initialAngle: 0,
                                        //radius: 100,
                                        //minAngle: 0,
                                        //maxAngle: 180,
                                        //centerOffset: 0,
                                        //offsetY: 180,
                                        //formatFunction: function (value, itemIndex, serie, group) {
                                        //    return value;
                                        //}
                                    }
                                ],
                                click: function (e) {
                                    var clickBaseUri = '/Relations/AggregateRelationOverlay?criticalOnly=' + (critical ? 'true' : 'false') + '&';
                                    var data = nodes[e.elementIndex];                                    
                                    var url = clickBaseUri + 'type=' + type + '&id=' + id + '&targetType=' + data.Type + '&targetID=' + data.TypeID + '&intersectTypeID=' + data.IntersectTypeID;
                                    openTileOverlay(url);
                                }
                            }
                        ]
                    });
                    cht.jqxChart('addColorScheme', 'myScheme', colors);
                    cht.jqxChart('colorScheme', 'myScheme');
                    cht.jqxChart('refresh');

                    $(document).on('resize', function () {
                        cht.jqxChart('refresh');
                    });
                }
            });
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

function SourcingResponsibilityTile(controlID, contextList, permissions, type, id) {
    var source = {
        datatype: 'json',
        url: "/api/" + type + "/" + id + "/sources",
        datafields: [
            { name: 'ResponsibilityID' },
            { name: 'AssigningItemType' },
            { name: 'AssigningIconBackColor' },
            { name: 'AssigningIconForeColor' },
            { name: 'AssigningIconText' },
            { name: 'AssigningItemID' },
            { name: 'AssigningItemName' },
            { name: 'AssigningItemUrl' },
            { name: 'ResponsibleObjectType' },
            { name: 'ResponsibleObjectID' },
            { name: 'ResponsibleObjectName' },
            { name: 'ObjectName' },
            { name: 'ObjectUrl' },
            { name: 'ObjectType' },
            { name: 'ObjectID' },
            { name: 'Role' },
            { name: 'ResponsibleObjectUrl' },
            { name: 'ContextItems' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(source);

    try {
        if (gridExists(controlID)) {
            $(controlID).jqxGrid('source', adapter);
            $(controlID).jqxGrid('updatebounddata');
        }
        else {
            $(controlID).jqxGrid({
                altrows: true,
                width: grid_width,
                autoheight: true,
                sortable: true,
                filterable: true,
                showfilterrow: true,
                pagesizeoptions: ['10', '20', '50'],
                pagesize: 20,
                pageable: true,
                autorowheight: true,
                columnsresize: true,
                source: adapter,
                theme: list_theme,
                columns: [
                        {
                            datafield: "ResponsibleObjectName",
                            text: "Artifact",
                            width: '22%',
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                try {
                                    var html = previewLinkRenderer(data.ResponsibleObjectType, data.ResponsibleObjectID, data.ResponsibleObjectUrl, data.ResponsibleObjectName);
                                    data = null;
                                    return html;
                                } catch (e) {
                                    console.log(e);
                                }
                            }
                        },
                        { datafield: "Role", text: "Role", width: '22%' },
                        { datafield: "ContextItems", text: "Context", width: '40%', },
                        {
                            datafield: "ResponsibilityID",
                            text: "",
                            width: '80px',
                            filterable: false,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                try {
                                    var tools = [];

                                    if (data.ObjectType == data.AssigningItemType && data.ObjectID == data.AssigningItemID && permissions.HasPermission("Governance", "Update")) {
                                        tools.push({ icon: 'pencil', urlprefix: '/form/EditResponsibility?id={0}' });
                                        tools.push({ icon: 'trash-o', urlprefix: '/form/DeleteResponsibility?id={0}' });
                                    }

                                    return renderToolsHtml(value, tools, contextList.Responsibility, data);
                                } catch (e) {
                                    console.log(e);
                                }
                            }
                        }
                ]
            });
        }

    } catch (e) {
    }

    //#region Event Subscriptions

    function pageResized() {
        $(controlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case contextList.Artifact:
                case contextList.DomainList:
                case contextList.Responsibility:
                case contextList.SourcingResponsibility:
                case contextList.Taxonomy:
                    $(controlID).jqxGrid('updatebounddata');
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

function TagsTile(controlID, permissions, type, id) {
    controlID = '#' + controlID;
    var gridID = '#TagsTileGrid';
    var textID = '#TagsTileText';
    var tagApi = '/api/tags/' + type + '/' + id;
    var source = $("#tagsTileTmpl").html();
    var template = Handlebars.compile(source);
    $(controlID).html(template({ }));

    //#region Grid

    var source = {
        datatype: 'json',
        url: tagApi,
        datafields:
        [
            { name: 'ID' },
            { name: 'Name' }
        ]
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
        showemptyrow: false,
        showheader: false,
        showfilterrow: false,
        filterable: false,
        pageable: false,
        theme: list_theme,
        columns: [
            {
                datafield: "Name",
                text: "Name"//,
                //cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                //    return previewLinkRenderer('Artifact', data.ID, data.Url, data.Name);
                //}
            }//,
            //{
            //    datafield: "ID",
            //    text: "",
            //    width: '40px',
            //    sortable: false,
            //    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
            //        var tools = [
            //            { icon: 'trash-o', urlprefix: '/form/RelatedArtifact/' + id + '/' + data.ID, context: 'action', method: 'DELETE' }
            //        ];
            //        return renderToolsHtml(value, tools, 'action');
            //    }
            //}
        ]
    });

    //#endregion

    //#region Textbox

    var textDataSource = {
        datatype: "json",
        datafields: [
            { name: 'ID' },
            { name: 'Name' }
        ],
        url: '/api/tags/'
    };
    var textDataAdapter = new $.jqx.dataAdapter(textDataSource);

    $(textID).jqxInput({ source: textDataAdapter, placeHolder: "Add a tag...", displayMember: "Name", valueMember: "ID", width: '100%', height: 25 });

    //#endregion

    //#region Events

    //function commandExecuted(command) {
    //    if (command == "TagAdded") {
    //        $(gridID).jqxGrid('updatebounddata');
    //        $(textID).jqxInput('val', '');
    //    }
    //    if (command == "TagDeleted") {
    //        $(gridID).jqxGrid('updatebounddata');
    //    }
    //}

    function pageResized() {
        $(gridID).jqxGrid('refresh');
    }

    //function saveAction(data) {
    //    try {
    //        switch (data.context) {
    //            case 'Tag':
    //                $(gridID).jqxGrid('updatebounddata');
    //                break;
    //        }
    //    } catch (e) {
    //        logError("Parts.js : TagsTile : SaveAction", e);
    //    }
    //}

    function textChange() {
        var value = $(textID).val();
        $.ajax({
            url: tagApi,
            dataType: 'json',
            type: 'POST',
            data: { Name: value }
        }).done(function () {
            $(gridID).jqxGrid('updatebounddata');
            $(textID).jqxInput('val', '');
        });
    }

    function textSelect(event) {
        if (event.args) {
            var item = event.args.item;
            if (item) {
                $.ajax({
                    url: tagApi,
                    dataType: 'json',
                    type: 'PUT',
                    data: { ID: item.value }
                }).done(function () {
                    $(gridID).jqxGrid('updatebounddata');
                    $(textID).jqxInput('val', '');
                });
            }
        }
    }

    function unsubscribe(data) {
        source = null;
        adapter = null;

        //amplify.unsubscribe('CommandExecuted', commandExecuted);
        amplify.unsubscribe("PageResized", pageResized);
        $(textID).off('change', textChange);
        $(textID).off('select', textSelect);
        //amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    //amplify.subscribe("CommandExecuted", commandExecuted);
    amplify.subscribe("PageResized", pageResized);
    $(textID).on('change', textChange);
    $(textID).on('select', textSelect);
    //amplify.subscribe("SaveAction", saveAction);
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

function YourWorkflowTasks(controlID, title, showTitle) {
    var chartControlID = controlID + "_chart";
    var gridControlID = controlID + "_grid";
    var labelControlID = controlID + "_label";
    controlID = '#' + controlID;
    var html = "";
    if (showTitle) html = "<header>" + title + "</header>";//<span id='" + controlID + "_HelpTip'><i class='fa fa-question'></i></span></header>";
    html += '<div class="directions">Click on a pie slice in the chart to get a list of your tasks by type.</div>';
    html += '<div class="row">';
    html += '<div class="col l4 m12 s12"><div id="' + chartControlID + '"></div></div>';
    html += '<div class="col l8 m12 s12"><h4 id="' + labelControlID + '"></h4><div id="' + gridControlID + '"></div></div>';
    html += '</div>';
    $(controlID).html(html);
    chartControlID = '#' + chartControlID;
    gridControlID = '#' + gridControlID;
    labelControlID = '#' + labelControlID;

    var chart = $(chartControlID);
    var chartSource;
    var chartAdapter;
    var gridSource;
    var gridAdapter;

    //#region Event Subscriptions

    var itemsBindComplete = function (event) {
        $(gridControlID).jqxGrid('autoresizecolumns');
    };

    var bindComplete = function (event) {
        $(chart).jqxGrid('selectrow', 0);
    };

    var rowSelect = function (event) {
        var args = event.args;
        var rowBoundIndex = args.rowindex;

        var data = args.row;
        switch (data.WorkflowTypeID) {
            case 1:
                //#region Suggest
                gridSource.datafields = [
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
                ];
                $(gridControlID).jqxGrid('columns', [
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
                    //{ datafield: "ProposedDescription", text: "Proposed Description" },
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
                ]);
                //#endregion
                break;
            case 2:
                //#region Certify
                gridSource.datafields = [
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
                ];
                $(gridControlID).jqxGrid('columns', [
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
                ]);
                //#endregion
                break;
            case 3:
                //#region WorkIssue
                gridSource.datafields = [
                    { name: 'WorkflowID' },
                    { name: 'Issue', type: 'string' },
                    { name: 'ResourceID', type: 'number' },
                    { name: 'ResourceName', type: 'string' },
                    { name: 'ResourceUrl', type: 'string' },
                    { name: 'DateStarted', type: 'date' },
                    { name: 'Activity', type: 'string' },
                    { name: 'ActivityDescription', type: 'string' },
                    { name: 'ActivityName', type: 'string' }
                ];
                $(gridControlID).jqxGrid('columns', [
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
                ]);
                //#endregion
                break;
            default:
                //#region Not known
                gridSource.datafields = [
                    { name: 'Activity' },
                    { name: 'ActivityDescription', type: 'string' },
                    { name: 'ActivityName', type: 'string' },
                    { name: 'DateStarted', type: 'date' },
                    { name: 'Workflow' },
                    { name: 'WorkflowDescription', type: 'string' },
                    { name: 'WorkflowName', type: 'string' },
                    { name: 'WorkflowID' },
                    { name: 'Properties', type: 'array' }
                ];
                $(gridControlID).jqxGrid('columns', [
                    {
                        columntype: 'dropdownlist',
                        filtertype: 'checkedlist',
                        datafield: "WorkflowName",
                        text: "Workflow",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return quickTipRenderer(data.WorkflowName, data.WorkflowDescription);
                        }
                    },
                    {
                        columntype: 'dropdownlist',
                        filtertype: 'checkedlist',
                        datafield: "ActivityName",
                        text: "Activity",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            return quickTipRenderer(data.ActivityName, data.ActivityDescription);
                        }
                    },
                    {
                        datafield: "Properties", text: "Properties",
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var html = "";
                            for (var key in data.Properties) {
                                if (data.Properties.hasOwnProperty(key)) {
                                    if (html != "") html += ", ";
                                    html += "<b>" + key + ":</b> " + data.Properties[key];
                                }
                            }
                            return html;
                        }
                    },
                    {
                        datafield: "DateStarted", text: "Date Started", columntype: 'datetimeinput', filtertype: 'range', cellsformat: "MMM d yyyy", // hh:mm:ss tt
                    },
                    {
                        datafield: "WorkflowID", text: "", sortable: false, filterable: false, width: '40px',
                        cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            var tools = [];

                            tools.push({ icon: 'check-circle-o', urlprefix: 'workflow/' + data.WorkflowID + '/overlay' });

                            return renderToolsHtml(value, tools, contextList.Artifact, data);
                        }
                    }
                ]);
                //#endregion
                break;
        }

        $(labelControlID).text(data.WorkflowTypeName + " Tasks");

        gridSource.url = '/services/workflow/tasks/types/' + data.WorkflowTypeID + '?$orderby=DateStarted%20asc';
        $(gridControlID).jqxGrid('updatebounddata');
    };

    function pageResized() {
        chart.jqxGrid('refresh');
        $(gridControlID).jqxGrid('refresh');
    }

    function saveAction(data) {
        try {
            switch (data.context) {
                case "Workflow":
                    var reloadChartData = function () {
                        var pr = new $.Deferred();
                        chartAdapter.dataBind();
                        return pr.promise();
                    }
                    reloadChartData().then(function () {
                        chart.jqxGrid('updatebounddata');
                        $(gridControlID).jqxGrid('updatebounddata');
                    });
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        chartSource = null;
        chartAdapter = null;
        gridSource = null;
        gridAdapter = null;

        amplify.unsubscribe("PageResized", pageResized);
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        $(chart).off('rowselect', rowSelect);
        $(chart).off("bindingcomplete", bindComplete);
        $(gridControlID).off("bindingcomplete", itemsBindComplete);
        chart = null;
    }

    $(gridControlID).on("bindingcomplete", itemsBindComplete);
    $(chart).on("bindingcomplete", bindComplete);
    $(chart).on('rowselect', rowSelect);
    amplify.subscribe("PageResized", pageResized);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion

    //#region Item Grid

    var gridSource = {
        datatype: 'json',
        url: null,
        datafields: [{ name: 'WorkflowID' } ]
    };

    var gridAdapter = new $.jqx.dataAdapter(gridSource);

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
            columns: [
                {
                    datafield: "WorkflowID",
                    text: "",
                    sortable: false,
                    filterable: false
                }
            ]
        });
    } catch (e) {
    }

    //#endregion

    //#region Type Grid

    try {
        chartSource = {
            datatype: 'json',
            url: '/services/workflow/tasks/types/breakdown',
            datafields:
            [
                { name: 'Workflow' },
                { name: 'WorkflowTypeID' },
                { name: 'WorkflowTypeName' },
                { name: 'Count' }
            ]
        };

        chartAdapter = new $.jqx.dataAdapter(chartSource, { async: false });

        $(chart).jqxGrid({
            altrows: true,
            width: grid_width,
            autoheight: true,
            sortable: true,
            filterable: true,
            showfilterrow: true,
            pagesizeoptions: ['10', '20', '50'],
            pagesize: 10,
            pageable: true,
            autorowheight: true,
            source: chartAdapter,
            theme: list_theme,
            columns: [
                    { datafield: "WorkflowTypeName", text: "Workflow" },
                    { datafield: "Count", text: "# Assignments", width: '30%' }
            ]
        });
    } catch (e) {
        console.log(e);
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

    //var controlID_add = controlID + '_add';
    var controlID_add_search_text = controlID + '_add_search_text';
    var controlID_add_search = controlID + '_add_search';
    var controlID_add_artifact_type = controlID + '_add_artifact_type';
    var controlID_add_search_message = controlID + '_add_search_message';

    //var controlID_overlay_radio_existing = controlID + '_overlay_radio_existing';
    //var controlID_overlay_radio_new = controlID + '_overlay_radio_new';
    var controlID_overlay_existing = controlID + '_overlay_existing';
    var controlID_overlay_new = controlID + '_overlay_new';
    var controlID_overlay_relationship = controlID + '_overlay_relationship';
    var controlID_overlay_predicates = controlID + '_overlay_predicates';
    var controlID_overlay_cancel = controlID + '_overlay_cancel';
    var controlID_overlay_add = controlID + '_overlay_add';
    var controlID_overlay_roles = controlID + '_overlay_roles';
    //var controlID_overlay_pname = controlID + '_overlay_pname';
    //var controlID_overlay_phrase = controlID + '_overlay_phrase';

    var controlID_responsibilities = controlID + '_responsibilities';
    var controlID_responsibilities_table = controlID + '_responsibilities_table';

    var controlID_fusion = controlID + '_fusion';
    var controlID_fusion_body = controlID + '_fusion_body';
    var controlID_fusion_table = controlID + '_fusion_table';

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
    var controlID_ribbon_add = controlID + '_ribbon_add';
    var controlID_ribbon_undo = controlID + '_ribbon_undo';
    var controlID_ribbon_redo = controlID + '_ribbon_redo';
    var controlID_ribbon_remove = controlID + '_ribbon_remove';

    var controlID_popover_add = controlID + '_popover_add';
    

    $("#" + controlID_ribbon_zoom_100).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_zoom_fit).jqxButton({ theme: theme, height: "100%", width: "40%" });
    $("#" + controlID_ribbon_save).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_reset).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_fullscreen).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_add).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_remove).jqxButton({ theme: theme, height: "100%", width: 64 }).hide();
    $("#" + controlID_ribbon_undo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_redo).jqxButton({ theme: theme, height: "100%", width: 64 });
    $("#" + controlID_ribbon_zoom_out).jqxRepeatButton({ delay: 3, theme: theme });
    $("#" + controlID_ribbon_zoom_in).jqxRepeatButton({ delay: 3, theme: theme });

    $("#" + controlID_info).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_responsibilities).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_fusion).jqxExpander({ theme: theme }).jqxExpander('collapse');
    $("#" + controlID_ribbon_expander).jqxExpander({ theme: theme }).jqxExpander('collapse');


    $("#" + controlID_message).hide();
    //$("#" + controlID_overlay_roles).jqxDropDownList({ theme: theme, width: '100%' });
    //$("#" + controlID_popover_add).jqxPopover({ theme: 'default', title: 'Add a Node', showCloseButton: true, autoClose: false, selector: $('#' + controlID_ribbon_add), offset: { left: 0, top: 0 }, arrowOffsetValue: 0 })
    //.after('open',function (e) {
    //    myPalette.scale = 1.0;
    //});

    //var toggleAdd = false;
    $("#" + controlID_ribbon_add).on('click', function () {
        $('#' + controlID_popover_add).toggle(200).css('left',$(this).position().left + 1).css('top',$(this).position().top + 150);
    });
    //$("#" + controlID_controls_zoom).jqxScrollBar({ theme: theme, width: 280, height: 18, min: 750, max: 2250, value: 1500 });
    //$("#" + controlID_overlay_radio_existing).jqxRadioButton({theme: theme}).jqxRadioButton('check');
    //$("#" + controlID_overlay_radio_new).jqxRadioButton({ theme: theme });

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
            window.scrollTo(0,0); //scroll to top
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
        },0);
    });

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
    var overlayEdit = false;
    var selectedNode = null;
    //#region methods

    if (readonly) {
        $('#' + controlID_ribbon_add).hide();
        $('#' + controlID_ribbon_save).hide();
    } else {
        $("#" + controlID_ribbon_remove).jqxButton({ theme: theme });
        $("#" + controlID_ribbon_remove).on('click', function () {
            markForDeletion(selectedNode);
            //populateDiagram();
        });
    }

    function createLinkModel() {
        return {
            id: null,
            from: null,
            frompid: "OUT",
            to: null,
            text: null,
            phrase: null,
            predicateName: null,
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
            intersectMapId: null
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
            toMaxLinks: 1, // don't allow more than one link into a port
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
               width: 125,
               height: 50,
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
                   maxSize: new go.Size(180, 50),
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
        //console.log(obj);
        if (obj != null) {
            if (obj.diagramObjectType == 'Node') {
                type = obj.type;
                id = obj.id;

                populateDiagram();
            }
            else if (obj.diagramObjectType == 'Link' && !readonly) {
                overlayEdit = true;
                showRelationshipOverlay(obj);


                //var fromNode = myDiagram.model.findNodeDataForKey(obj.from);
                //var toNode = myDiagram.model.findNodeDataForKey(obj.to);

                //newLink = obj;
                //$('#' + controlID_overlay).show();
                

            }
        }
    }

    function onSelectionChange(e) {
        var node = e.diagram.selection.first();
        selectedNode = null;
        
        $('#' + controlID_fusion_table).hide(200);

        if (node == null) {
            //$('#preview').html('');
            // $("#" + controlID_fusion).jqxExpander('expand');
            $("#" + controlID_ribbon_remove).hide(200);
            $('#' + controlID_fusion).jqxExpander('collapse');
            $('#' + controlID_responsibilities).jqxExpander('collapse');
            $('#' + controlID_info).jqxExpander('collapse');
            return;
        }
        $("#" + controlID_ribbon_remove).show(200);
        var data = node.data;

        if (data.diagramObjectType == 'Node') { //node selected
            selectedNode = data;
            $('#' + controlID_fusion).jqxExpander('collapse');
            $.ajax({
                url: '/resources/' + data.type + '/' + data.id + '/templates/tooltip/Preview',
                data: null,
                success: function (data) {

                    $('#' + controlID_info_body).html(data);
                },
                async: true
            });

            $.ajax({
                url: '/services/relationships/responsibilities/' + data.type + '/' + data.id,
                data: null,
                success: function (data) {

                    var html = formResponsibilitiesHtml(data);

                    $('#' + controlID_responsibilities_table + ' tr').slice(1).remove();

                    if (html == "") {
                        $('#' + controlID_responsibilities).jqxExpander('collapse');
                        $('#' + controlID_responsibilities_table).hide(200);
                    } else {

                        $('#' + controlID_responsibilities_table + ' > tbody:last-child').append(html);
                        $('#' + controlID_responsibilities_table).show(200);
                        $('#' + controlID_responsibilities).jqxExpander('expand');
                    }
                }
            });

            $.ajax({
                url: '/services/relationships/technical/' + originalObject + '/' + originalObjectID + '/' + data.type + '/' + data.id,
                success: function (data) {

                    var html = formFusionHtml(data);
                    $('#' + controlID_fusion_table + ' tr').slice(1).remove();

                    if (html == "") {
                        $('#' + controlID_fusion).jqxExpander('collapse');
                        $('#' + controlID_fusion_table).hide(200);
                    } else {
                        $('#' + controlID_fusion_table + ' > tbody:last-child').append(html);
                        $('#' + controlID_fusion_table).show(200);
                        $('#' + controlID_fusion).jqxExpander('expand');
                    }
                    //console.log(data);
                },
                async: true
            });

            $('#' + controlID_info).jqxExpander('expand');
            $('#' + controlID_fusion).jqxExpander('collapse');
        } else if (data.diagramObjectType == "Link") { //link selected
            $('#' + controlID_responsibilities).jqxExpander('collapse');
            $('#' + controlID_info_body).html('');
            $('#' + controlID_info).jqxExpander('collapse');
            var toNode = myDiagram.findNodeForKey(data.to);
            var fromNode = myDiagram.findNodeForKey(data.from);

            if (toNode != null && fromNode != null) {
                $.ajax({
                    url: '/services/relationships/technical/' + fromNode.data.type + '/' + fromNode.data.id + '/' + toNode.data.type + '/' + toNode.data.id,
                    success: function (data) {

                        var html = formFusionHtml(data);
                        $('#' + controlID_fusion_table + ' tr').slice(1).remove();

                        if (html == "") {
                            $('#' + controlID_fusion).jqxExpander('collapse');
                            $('#' + controlID_fusion_table).hide(200);
                        } else {
                            $('#' + controlID_fusion_table + ' > tbody:last-child').append(html);
                            $('#' + controlID_fusion_table).show(200);
                            $('#' + controlID_fusion).jqxExpander('expand');
                        }
                    },
                    async: true
                });
            }
        }
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

    function onDelete(e) {
        if (readonly) {
            e.cancel = true;
            return;
        }
        markForDeletion(selectedNode);
        //e.subject.each(function (d) {
        //    console.log('d: ' + d.data);
        //    markPendingExclusions(d.data);

        //});
        //console.log('pending exclusion nodes: ' + pendingExclusionNodes);
        //populateDiagram();
    };
    function markForDeletion(node) {
        console.log(node);
        if (node == null)
            return;

        //deletedNodes.push({
        //    type: node.type,
        //    id: node.id,
        //    targetID: node.intersectMapId
        //});

        //var affectedLinks = [];
        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            var link = myDiagram.model.linkDataArray[i];
            if (link.to == node.key || link.from == node.key) {
                myDiagram.model.removeLinkData(link);
            }
        }

        myDiagram.model.removeNodeData(node);
        checkModified();
        //$.ajax({
        //    method: 'DELETE',
        //    url: '/relations/' + type + '/' + id + '/sources/' + selectedNode.intersectMapId
        //}).done(function (data, status, xhr) {
        //    if (data.success) {
        //        populateDiagram();
        //        amplify.publish("SourceSave");
        //    }
        //}).fail(function (xhr, status, error) {
        //    amplify.publish("SourceFormStatus", { title: 'Error When Adding Source', message: xhr.statusText + xhr.responseText, success: false });
        //});
    }

    function onDrop(e) {
        $('#' + controlID_popover_add).hide();
        checkModified();
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

    function formFusionHtml(data) {
        var html = '';
        for (var i = 0; i < data.length; i++) {
            html += "<tr><td style='padding:2px'>" + (data[i].Attribute || '') + "</td>";
            html += "<td style='padding:2px'><a href='/" + (data[i].URL || '#') + "'>" + (data[i].Fusion || '') + "</a></td>";
            html += "<td style='padding:2px'>" + (data[i].Name || '') + "</td>";
            html += "<td style='padding:2px'>" + (data[i].Type || '') + "</td></tr>";
        }
        return html;
    }

    function formResponsibilitiesHtml(data) {
        var html = '';
        for (var i = 0; i < data.length; i++) {
            html += "<tr><td style='padding:2px'>" + (data[i].Type || '') + "</td>";
            html += "<td style='padding:2px'><a href='/" + (data[i].Url || '#') + "'>" + (data[i].Name || '') + "</td>";
            html += "<td style='padding:2px'><a href='/" + (data[i].OwnerUrl || '#') + "'>" + (data[i].Owner || '') + "</td>";
            html += "<td style='padding:2px'>" + (data[i].Context || '') + "</td></tr>";
        }
        return html;
    }

    function saveChanges() {
        if (readonly) return;
        $('#' + controlID_ribbon_save).prop('disabled', true);
        var nodeChanges = getNodeChanges();
        var linkChanges = getLinkChanges();

        var data = {
            AddedLinks: linkChanges.added,
            DeletedNodes: nodeChanges.deleted,
            AllNodes: myDiagram.model.nodeDataArray,
            AllLinks: myDiagram.model.linkDataArray
        }

        $.ajax({
            url: '/Diagrams/SaveChanges',
            data: JSON.stringify(data),
            processData: false,
            type: 'POST',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                $('#' + controlID_ribbon_save).prop('disabled', false);
                deletedNodes = [];
                
                populateDiagram();
            },
            failure: function (data) {
                $('#' + controlID_ribbon_save).prop('disabled', false);
            }
        });

    }

    function checkModified() {
        var nodes = getNodeChanges();
        var links = getLinkChanges();

        if (readonly) {
            $('#' + controlID_message).hide(200);
        } else if (nodes.deleted.length > 0 ||
            nodes.added.length > 0 ||
            nodes.modified.length > 0 ||
            links.added.length > 0 ||
            links.deleted.length > 0 ||
            links.modified.length > 0) {

            $('#' + controlID_message).show(200);
        } else {
            $('#' + controlID_message).hide(200);
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
                changes.deleted.push(initialLinks[i]);
            }
        }

        for (var i = 0; i < links.length; i++) {
            for (var j = 0; j < initialLinks.length; j++) {
                //modified logic
            }
        }

        //console.log(changes);
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
                        changes.modified.push(nodes[i]);
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

    myDiagram = g(go.Diagram, controlID_diagram, {
        initialContentAlignment: go.Spot.Left,
        //autoScale: go.Diagram.UniformToFill,
        allowDrop: true,
        initialAutoScale: go.Diagram.UniformToFill,
        scrollMode: go.Diagram.DocumentScroll,
        initialPosition: new go.Point(125, 125),
        layout: g(go.LayeredDigraphLayout, { direction: 0, columnSpacing: 50, layerSpacing: 50 }),
        "undoManager.isEnabled": true
    });
    myDiagram.model.class = go.GraphLinksModel;
    myDiagram.model.nodeCategoryProperty = "template";
    myDiagram.model.linkFromPortIdProperty = "frompid";
    myDiagram.model.linkToPortIdProperty = "topid";
    myDiagram.model.nodeDataArray = [];
    myDiagram.model.linkDataArray = [];
    myDiagram.toolManager.linkingTool.isEnabled = !readonly;



    //$("#" + controlID_overlay_radio_existing).bind('change', function (e) {
    //    var checked = e.args.checked;

    //    if (checked) {
    //        $('#' + controlID_overlay_existing).show(200);
    //        $('#' + controlID_overlay_new).hide(200);
    //        if ($('#' + controlID_overlay_predicates).val() != 0) {
    //            $('#' + controlID_overlay_add).prop('disabled', false);
    //        } else {
    //            $('#' + controlID_overlay_add).prop('disabled', true);
    //        }
    //    } else {
    //        $('#' + controlID_overlay_existing).hide(200);
    //        $('#' + controlID_overlay_new).show(200);
    //        if ($('#' + controlID_overlay_pname).val() != '' && $('#' + controlID_overlay_phrase).val() != '') {
    //            $('#' + controlID_overlay_add).prop('disabled', false);
    //        } else {
    //            $('#' + controlID_overlay_add).prop('disabled', true);
    //        }
    //    }
    //});

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

    //$('#' + controlID_overlay_pname).on('keyup', function (e) {
    //    var checked = $('#' + controlID_overlay_radio_new).jqxRadioButton('checked');
    //    if (checked) {
    //        if ($(this).val() == '' || $('#' + controlID_overlay_phrase).val() == '') {
    //            $('#' + controlID_overlay_add).prop('disabled', true);
    //        } else {
    //            $('#' + controlID_overlay_add).prop('disabled', false);
    //        }
    //    }
    //});

    //$('#' + controlID_overlay_phrase).on('keyup', function (e) {
    //    var checked = $('#' + controlID_overlay_radio_new).jqxRadioButton('checked');
    //    if (checked) {
    //        if ($(this).val() == '' || $('#' + controlID_overlay_pname).val() == '') {
    //            $('#' + controlID_overlay_add).prop('disabled', true);
    //        } else {
    //            $('#' + controlID_overlay_add).prop('disabled', false);
    //        }
    //    }
    //});

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
        type = originalObject;
        id = originalObjectID;
        populateDiagram();
    });

    //$("#" + controlID_controls_zoom).on('valueChanged', function (event) {
    //    var val = parseInt(event.currentValue);
    //    $('#' + controlID_ribbon_zoom_text).text(Math.round((val / 1500) * 100) + '%');
    //    myDiagram.scale = (val / 1500);
    //});

    //#endregion

    myDiagram.addDiagramListener('ViewportBoundsChanged', onViewportBoundsChanged);
    myDiagram.addDiagramListener('ChangedSelection', onSelectionChange);
    myDiagram.addDiagramListener('ObjectDoubleClicked', onDoubleClick);
    myDiagram.addDiagramListener('LayoutCompleted', onLayoutCompleted);
    myDiagram.addDiagramListener('LinkDrawn', onLinkDrawn);
    myDiagram.addDiagramListener('SelectionDeleting', onDelete);
    myDiagram.addDiagramListener('ExternalObjectsDropped', onDrop);

    myDiagram.grid.visible = false;
    myDiagram.grid.gridCellSize = new go.Size(8, 8);
    myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
    myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;
    //myDiagram.allowVerticalScroll = false;
    //myDiagram.allowHorizontalScroll = false;
    //myDiagram.scrollMode = go.Diagram.InfiniteScroll;


    makeTemplate("FocalArtifact", 275, 150, '#000000', 14, [makePort("", true)], [makePort("OUT", false)]);
    makeTemplate("Artifact", 225, 105, 'transparent', 10, [makePort("", true)], [makePort("OUT", false)]);
    //makeTemplate("FusionAttribute", 300, 50, 7, [makePort("", true)], [makePort("OUT", false)]);

    myDiagram.linkTemplate = g(
        go.Link, { routing: go.Link.Orthogonal, curve: go.Link.JumpOver, corner: 10, relinkableFrom: false, relinkableTo: false }, // the whole link panel
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
    

    myPalette = g(go.Palette, controlID_palette, {
        contentAlignment: go.Spot.BottomCenter
        , allowDrop: true
        , initialAutoScale: go.Diagram.Uniform
        , model: new go.GraphLinksModel([{ template: "Artifact", backColor: 'black', foreColor: 'white', name: '', id: -1, key: -1, typeName: '', type: '', isDeletable: true }])
    })
    {
        myPalette.model.nodeCategoryProperty = 'template';
        myPalette.model.nodeDataArray = [];
        myPalette.model.class = 'go.GraphLinksModel';
    }
    makeSearchTemplate();

    myOverview =  
      g(go.Overview, controlID_overview,
        { observed: myDiagram, contentAlignment: go.Spot.Center });


    function parseData(data) {

        myDiagram.startTransaction("load_all_data");
        myDiagram.model.nodeDataArray = [];
        myDiagram.model.linkDataArray = [];
        initialNodes = [];
        initialLinks = [];
        var modelList = [];
        var linkList = [];
        $('#' + controlID_message).hide(200);

        for (var i = 0; i < data.nodes.length; i++) {

            var d = data.nodes[i];
            var model = createNodeModel();

            var isFocalPoint = (d.obj == type && d.objid == id);// && d.level == 0);

            if (isFocalPoint) {
                $('#' + controlID_header).text('Lineage: ' + d.name);
            }

            model.template = isFocalPoint ? "FocalArtifact" : "Artifact";;
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

            //for (var j = 0; j < pendingExclusionNodes.length; j++) {
            //    if (pendingExclusionNodes[j].intersectMapId == model.intersectMapId) {
            //        model.exclude = 'true';
            //    }
            //}
            modelList.push(model);
        }

        for (var i = 0; i < data.links.length; i++) {
            var d = data.links[i];
            var link = createLinkModel();
            link.id = d.id;
            link.from = d.from;
            link.to = d.to;
            link.text = d.text;
            link.diagramObjectType = "Link";
            linkList.push(link);
        }

        for (var i = 0; i < modelList.length; i++) {
            myDiagram.model.addNodeData(modelList[i]);
        }
        for (var i = 0; i < linkList.length; i++) {
            myDiagram.model.addLinkData(linkList[i]);
        }
        //myDiagram.model.nodeDataArray = modelList;
        //myDiagram.model.linkDataArray = linkList;
        //myDiagram.model.nodeDataArray = modelList;
        //myDiagram.model.linkDataArray = linkList;
       
        initialNodes = modelList.slice();
        initialLinks = linkList.slice();

        myDiagram.commitTransaction("load_all_data");
        reOrderLayout();

    }


    function populateDiagram() {
        var results = $.ajax({
            url: '/diagrams/maps/' + type + '/' + id + '.json',
            data: null,
            success: function (data) {
                console.log(data);
                parseData(data);
                //populateNodeSelectList();
                myDiagram.zoomToFit();
            }
        });
    }
    populateDiagram();

    function markPendingExclusions(node) {
        var data = {
            type: node.type,
            id: node.id
        };
        pendingExclusionObjects.push({
            ObjectType: node.type,
            ObjectID: node.id
        });

        $.ajax({
            url: '/diagrams/GetExclusionsByMapObject',
            data: data,
            async: false,
            success: function (data) {
                for (var i = 0; i < data.length; i++) {
                    for (var j = 0; j < myDiagram.model.nodeDataArray.length; j++) {
                        var n = myDiagram.model.nodeDataArray[j];

                        if (n.intersectMapId == data[i]) {
                            //console.log('marking intersectmapid ' + n.intersectMapId + ' as excluded');
                            myDiagram.model.nodeDataArray[j].exclude = 'true';
                            pendingExclusionNodes.push(n);
                        }
                    }
                }
            }
        });

        //console.log(myDiagram.model.nodeDataArray);
    }


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
    $('#' + controlID_ribbon_save).on('click', saveChanges);

    function populatePredicateList() {
        $.ajax({
            url: '/diagrams/getpredicateinfo',
            success: function (data) {
                var output = [];
                predicates = [];

                output.push('<option value="0_1"></option>');
                for (var i = 0; i < data.length; i++) {
                    //data[i].id = data[i].intersecttypeid + '_' + data[i].intersecttyperoleid
                    output.push('<option value="' + data[i].id.toString() + '_' + data[i].direction.toString() + '">' + data[i].name + (data[i].direction == 2 ? ' (inverse)' : '') + '</option>');
                    predicates.push(data[i]);
                }
                $('#' + controlID_overlay_predicates).html(output.join(''));
            }
        });
    }

    function cancelAddLink() {
        if (newLink != null) {
            if (overlayEdit) {
                overlayEdit = false;
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
        //$('#' + controlID_overlay_pname).val('');
       // $('#' + controlID_overlay_phrase).val('');
        $('#' + controlID_overlay_add).prop('disabled', true);
        //$('#' + controlID_overlay_radio_existing).jqxRadioButton('check');
    }

    function addRelationship(id) {
        var id = ($('#' + controlID_overlay_predicates).val() || '').split('_')[0];
        var isInverse = ((($('#' + controlID_overlay_predicates).val() || '').split('_')[1]) == '2');
        var phrase = null;
        
        //if ($('#' + controlID_overlay_radio_existing).jqxRadioButton('checked')) {
        for (var i = 0; i < predicates.length; i++) {
            if ((predicates[i].id.toString() + '_' + predicates[i].direction.toString()) == $('#' + controlID_overlay_predicates).val()) {

                phrase = predicates[i].name;
            }
        }
        //}
        //else {
        //    rel = {
        //        name: $('#' + controlID_overlay_pname).val(),
        //        phrase: $('#' + controlID_overlay_phrase).val()
        //    };
        //}
        myDiagram.startTransaction("nameRelationship")

        newLink.predicateName = phrase;
        newLink.isInverse = isInverse;
        newLink.predicateId = id;
        newLink.phrase = phrase;
        newLink.text = phrase;
        newLink.diagramObjectType = "Link";
        newLink.isDeletable = true;

        var index = -1;

        for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
            if (myDiagram.model.linkDataArray[i].from == newLink.from &&
                myDiagram.model.linkDataArray[i].to == newLink.to) {
                myDiagram.model.removeLinkData(myDiagram.model.linkDataArray[i]);
                break;
            }
        }

        myDiagram.model.addLinkData(newLink);

        myDiagram.commitTransaction("nameRelationship");
        $('#' + controlID_overlay).hide();
        checkModified();
        newLink = null;

        resetOverlay();
      
    };



}
