//temporary workaround for Angular 2
//newest lineage diagram uses .cshtml with <script> tags which are not supported
//script moved to this function and called from TS through declaration after ng2 loads the html template
//need to also pass w which is the replacement for id="Panel" - ElementRef to angular reference
function NewLineageDiagram2(w) {

    var lineageObject = $('#objectType_Value').val();
    var lineageObjectID = $('#objectID_Value').val();

    //#region Vars

    var readonly = false;

    var originalObject = lineageObject;
    var originalObjectID = lineageObjectID;
    var viewID = 1;
    var fullscreen = false;
    var selectedData = null;
    var permissions = new PermissionsModel();
    permissions.GetPermissionsForObject(lineageObject, lineageObjectID);

    var initialLinks = [];
    var initialNodes = [];
    var newLink = null;
    var overlayEditLinkKey = null;
    var selection = null;

    //#endregion

    function sizePanel() {
        var windowHeight = $(window).innerHeight();
        var tileTopOffset = $(w).offset();
        var height = windowHeight - tileTopOffset.top - 75; //height();
        $('#LineageDiagram').height(height);
    }

    function unsubscribe() {
        $('#LineageWindow0').jqxWindow('destroy');
        $('#LineageWindow1').jqxWindow('destroy');
        $('#LineageWindow2').jqxWindow('destroy');
        $('#LineageWindow3').jqxWindow('destroy');
        $('#LineageWindow4').jqxWindow('destroy');
        $('#LineageWindow5').jqxWindow('destroy');
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    //#region Model Creation

    function createLinkModel() {
        return {
            id: null,
            key: null,
            Category: '',
            from: null,
            fromIntersectId: 0,
            fromPortId: "OUT",

            to: null,
            toIntersectId: 0,
            toPortId: "IN",

            text: null,
            //intersectRoleId: null,
            diagramObjectType: "Link",
            sourceMappingCount: 0,
            hasMappingRules: false,
            transformation: null,
            hasTransformations: false,
            hasProperties: false,
            mapItems: null
        };
    };

    function createNodeModel() {
        return {
            key: null,
            obj: null,
            objid: null,
            name: null,
            typeName: null,
            back: null,
            fore: null,
            highlightColor: null,
            diagramObjectType: "Node",
            template: "Artifact",
            intersectId: null,
            sourceRuleCount: 0,
            sourceMappingCount: 0,
            hasMappingRules: false,
            hasSourceRules: false,
            challengeCount: 0,
            hasChallenges: false,
            openEventCount: 0,
            hasOpenEvents: false,
            openIssueCount: 0,
            hasOpenIssues: false,
            transformationCount: 0,
            hasTransformations: false,
            mapItems: null,
            other: null
        };
    };

    //#endregion

    $(function () {
        var g = go.GraphObject.make;
        var myDiagram = initializeDiagram();

        //#region Control instantiation

        var windowSettings = {
            height: 'auto', width: 500,
            draggable: true,
            autoOpen: false,
            zIndex: 1002,
            minWidth: 400, minHeight: 300,
            maxWidth: 900, maxHeight: 500
        };

        $('#LineageWindow0').jqxWindow(windowSettings);
        $('#LineageWindow1').jqxWindow(windowSettings);
        $('#LineageWindow2').jqxWindow(windowSettings);
        $('#LineageWindow3').jqxWindow(windowSettings);
        $('#LineageWindow4').jqxWindow(windowSettings);
        $('#LineageWindow5').jqxWindow(windowSettings);

        $('#ManageLineageTools').jqxMenu({ theme: 'flat', mode: 'horizontal', height: 50 });
        $('#ManageLineageTools').hide();
        $('#ManageLineageTools').on('itemclick', function (event) {
            var li = event.args;
            switch ($(li).data('action')) {
                case 'cancel':
                    //#region
                    $('#LineageHeaderText').text("Lineage");
                    $('#LineageManager').hide();
                    $('#ManageLineageTools').hide();
                    $('#LineageDiagram').fadeIn();
                    $('#LineageTools').fadeIn();
                    break;
                    //#endregion
                case 'add':
                    //#region
                    lineageModel.AddItem();
                    break;
                    //#endregion
                case 'save':
                    //#region
                    lineageModel.Save().then(function (data) {
                        $('#LineageHeaderText').text("Lineage");
                        $('#LineageManager').hide();
                        $('#ManageLineageTools').hide();
                        $('#LineageDiagram').fadeIn();
                        $('#LineageTools').fadeIn();
                        populateDiagram();  //refresh the diagram.
                    });
                    break;
                    //#endregion
            }
        });

        $('#ManageMappingTools').jqxMenu({ theme: 'flat', mode: 'horizontal', height: 50 });
        $('#ManageMappingTools').hide();
        $('#ManageMappingTools').on('itemclick', function (event) {
            var li = event.args;
            switch ($(li).data('action')) {
                case 'cancel':
                    //#region
                    $('#LineageHeaderText').text("Lineage");
                    $('#LineageMapRuleManager').hide();
                    $('#LineageMultiMapRuleManager').hide();
                    $('#ManageMappingTools').hide();
                    $('#LineageDiagram').fadeIn();
                    $('#LineageTools').fadeIn();
                    break;
                    //#endregion
                case 'add':
                    //#region
                    mapRulesModel.AddItem();
                    break;
                    //#endregion
                case 'save':
                    //#region
                    mapRulesModel.SaveRules().then(function (data) {
                        $('#LineageHeaderText').text("Lineage");
                        $('#LineageMapRuleManager').hide();
                        $('#LineageMultiMapRuleManager').hide();
                        $('#ManageMappingTools').hide();
                        $('#LineageDiagram').fadeIn();
                        $('#LineageTools').fadeIn();
                        populateDiagram();  //refresh the diagram.
                    });
                    break;
                    //#endregion
            }
        });

        $('#ManageSourceTools').jqxMenu({ theme: 'flat', mode: 'horizontal', height: 50 });
        $('#ManageSourceTools').hide();
        $('#ManageSourceTools').on('itemclick', function (event) {
            var li = event.args;
            switch ($(li).data('action')) {
                case 'cancel':
                    //#region
                    $('#LineageHeaderText').text("Lineage");
                    $('#LineageSourceRuleManager').hide();
                    $('#ManageSourceTools').hide();
                    $('#LineageDiagram').fadeIn();
                    $('#LineageTools').fadeIn();
                    break;
                    //#endregion
                case 'save':
                    //#region
                    sourceRuleModel.Save().then(function (data) {
                        $('#LineageHeaderText').text("Lineage");
                        $('#LineageSourceRuleManager').hide();
                        $('#ManageSourceTools').hide();
                        $('#LineageDiagram').fadeIn();
                        $('#LineageTools').fadeIn();
                        populateDiagram();  //refresh the diagram.
                    });
                    break;
                    //#endregion
            }
        });

        $('#LineageTools').jqxMenu({ theme: 'flat', mode: 'horizontal', height: 50 });
        $("#LineageTools").jqxMenu('setItemOpenDirection', 'ToolView', 'left', 'down');
        $("#LineageTools").jqxMenu('setItemOpenDirection', 'ToolManage', 'left', 'down');
        $("#LineageTools").jqxMenu('setItemOpenDirection', 'ToolWindows', 'left', 'down');
        $('#LineageTools').on('itemclick', function (event) {
            var li = event.args;
            var offset = $(li).offset();
            switch ($(li).data('action')) {
                case 'reset':
                    //#region
                    lineageObject = originalObject;
                    lineageObjectID = originalObjectID;
                    populateDiagram();
                    break;
                    //#endregion
                case 'businesslineage':
                    //#region
                    var data = {
                        object: lineageObject,
                        objectID: lineageObjectID
                    };
                    $('#LineageHeaderText').text("Manage Lineage");
                    $('#LineageDiagram').hide();
                    $('#LineageManager').show();
                    $('#LineageTools').hide();
                    $('#ManageLineageTools').show();

                    lineageModel = new LineagePanelViewModel(data, permissions);
                    ko.cleanNode($('#LineageManager')[0]);
                    ko.applyBindings(lineageModel, $('#LineageManager')[0]);
                    break;
                    //#endregion
                case 'sourcerules':
                    //#region
                    var selected = myDiagram.selection;

                    if (selected == null)
                        return;

                    var selected = selected.first().data;

                    if (selected == null)
                        return;

                    var data = {
                        target: lineageObject,
                        targetID: lineageObjectID,
                        object: selected.type,
                        objectID: selected.id,
                        ID: 0
                    };

                    $('#LineageHeaderText').text("Manage Source Rules");
                    $('#LineageDiagram').hide();
                    $('#LineageTools').hide();
                    $('#ManageSourceTools').show();
                    $('#LineageSourceRuleManager').show();

                    sourceRuleModel = new MapSequencesModel(data, permissions);
                    ko.cleanNode($('#LineageSourceRuleManager')[0]);
                    ko.applyBindings(sourceRuleModel, $('#LineageSourceRuleManager')[0]);
                    break;
                    //#endregion
                case 'nodemappings':
                    //#region
                    var selected = myDiagram.selection;
                    if (selected == null)
                        return;
                    var selected = selected.first().data;
                    if (selected == null)
                        return;

                    if (selected.diagramObjectType == 'Link') {
                        var from = myDiagram.model.findNodeDataForKey(selected.from);
                        var to = myDiagram.model.findNodeDataForKey(selected.to);

                        var data = {
                            SourceName: from.name,
                            SourceIntersectID: selected.fromIntersectId,
                            SourceDiagramKey: selected.from,

                            TargetName: to.name,
                            TargetIntersectID: selected.toIntersectId,
                            TargetDiagramKey: selected.to
                        };
                        $('#LineageHeaderText').text("Manage Mappings");

                        $('#LineageDiagram').hide();
                        $('#LineageTools').hide();
                        $('#ManageMappingTools').show();
                        $('#LineageMapRuleManager').show();

                        mapRulesModel = new MapRulesModel(data, permissions);
                        ko.cleanNode($('#LineageMapRuleManager')[0]);
                        ko.applyBindings(mapRulesModel, $('#LineageMapRuleManager')[0]);
                    }
                    break;
                    //#endregion
                case 'allmappings':
                    //#region
                    var maps = [];

                    for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
                        var link = myDiagram.model.linkDataArray[i];
                        var from = myDiagram.model.findNodeDataForKey(link.from);
                        var to = myDiagram.model.findNodeDataForKey(link.to);
                        maps.push({
                            SourceName: from.name,
                            SourceIntersectID: link.fromIntersectId,
                            SourceDiagramKey: link.from,

                            TargetName: to.name,
                            TargetIntersectID: link.toIntersectId,
                            TargetDiagramKey: link.to
                        });
                    }

                    $('#LineageHeaderText').text("Manage Mappings");
                    $('#LineageDiagram').hide();
                    $('#LineageTools').hide();
                    $('#ManageMappingTools').show();
                    $('#LineageMultiMapRuleManager').show();

                    mapRulesModel = new MultiMapRulesModel({ Maps: maps }, permissions);
                    ko.cleanNode($('#LineageMultiMapRuleManager')[0]);
                    ko.applyBindings(mapRulesModel, $('#LineageMultiMapRuleManager')[0]);
                    break;
                    //#endregion
                case 'view1':
                    //#region
                    viewID = 1;
                    populateDiagram();
                    break;
                    //#endregion
                case 'view2':
                    //#region
                    viewID = 2;
                    populateDiagram();
                    break;
                    //#endregion
                case 'view3':
                    //#region
                    viewID = 3;
                    populateDiagram();
                    break;
                    //#endregion
                case 'w_info':
                    //#region
                    $("#LineageWindow0").jqxWindow('open');
                    $("#LineageWindow0").jqxWindow('move', offset.left - 50, offset.top + 50);
                    break;
                    //#endregion
                case 'w_sourcerules':
                    //#region
                    $("#LineageWindow1").jqxWindow('open');
                    $("#LineageWindow1").jqxWindow('move', offset.left - 75, offset.top + 75);
                    break;
                    //#endregion
                case 'w_mappings':
                    //#region
                    $("#LineageWindow2").jqxWindow('open');
                    $("#LineageWindow2").jqxWindow('move', offset.left - 100, offset.top + 100);
                    break;
                    //#endregion
                case 'w_roles':
                    //#region
                    $("#LineageWindow3").jqxWindow('open');
                    $("#LineageWindow3").jqxWindow('move', offset.left - 50, offset.top + 50);
                    break;
                    //#endregion
                case 'w_fusion':
                    //#region
                    $("#LineageWindow4").jqxWindow('open');
                    $("#LineageWindow4").jqxWindow('move', offset.left - 75, offset.top + 75);
                    break;
                    //#endregion
                case 'w_xforms':
                    //#region
                    $("#LineageWindow5").jqxWindow('open');
                    $("#LineageWindow5").jqxWindow('move', offset.left - 100, offset.top + 100);
                    break;
                    //#endregion
                case 'close':
                    //#region
                    unsubscribe();
                    amplify.publish('ClosePanel');
                    break;
                    //#endregion
            }
        });

        $("#LineageZoomSlider").jqxSlider({ theme: theme, width: 150, showButtons: true, min: 750, max: 2250, value: 1500, showTicks: false });

        //#endregion

        $('#LineageZoomSlider').on('slide', function (event) {
            var val = event.args.value;
            myDiagram.scale = (val / 1500);
        });

        //#region Node/Link Templates

        var nodeWidth = 200;
        var nodeHeight = 150;
        var nodeBorderColor = '#000000';
        var nodeFontSize = 14;
        //var nodeInPorts = [makePort("IN", true)];
        //var nodeOutPorts = [makePort("OUT", false)];

        //#region Focal Template

        var focalNode = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: nodeWidth,
            height: nodeHeight
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: nodeBorderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "back").makeTwoWay()
        ),
        g(go.Panel,
            go.Panel.Horizontal,
            {
                alignment: go.Spot.BottomLeft,
                margin: 5
            },
            makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
            makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
            makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
            makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
            makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                font: "bold " + nodeFontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
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
        [makePort("IN", false)]),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        [makePort("OUT", false)]));

        myDiagram.nodeTemplateMap.add("Focal", focalNode);

        //#endregion

        //#region Normal Template

        nodeWidth = 200;
        nodeHeight = 105;
        nodeBorderColor = 'transparent';
        nodeFontSize = 10;

        var normalNode = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: nodeWidth,
            height: nodeHeight
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: nodeBorderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "back").makeTwoWay()
        ),
        g(go.Panel,
            go.Panel.Horizontal,
            {
                alignment: go.Spot.BottomLeft,
                margin: 5
            },
            makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
            makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
            makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
            makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
            makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                font: "bold " + nodeFontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
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
        [makePort("IN", false)]),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        [makePort("OUT", false)]));

        myDiagram.nodeTemplateMap.add("Normal", normalNode);

        //#endregion

        //#region SupportFocal Template

        var nodeWidth = 140;
        var nodeHeight = 80;
        var nodeBorderColor = '#000000';
        var nodeFontSize = 9;

        var supportFocalNode = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: nodeWidth,
            height: nodeHeight
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: nodeBorderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "back").makeTwoWay()
        ),
        g(go.Panel,
            go.Panel.Horizontal,
            {
                alignment: go.Spot.BottomLeft,
                margin: 5
            },
            makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
            makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
            makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
            makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
            makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                font: "bold " + nodeFontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
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
        [makePort("IN", false)]),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        [makePort("OUT", false)]));

        myDiagram.nodeTemplateMap.add("SupportFocal", supportFocalNode);

        //#endregion

        //#region SupportNormal Template

        var nodeWidth = 130;
        var nodeHeight = 70;
        var nodeBorderColor = 'transparent';
        var nodeFontSize = 9;

        var supportNode = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: nodeWidth,
            height: nodeHeight
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: nodeBorderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "back").makeTwoWay()
        ),
        g(go.Panel,
            go.Panel.Horizontal,
            {
                alignment: go.Spot.BottomLeft,
                margin: 5
            },
            makeIconPanel("\uf128", "Has outstanding challenges", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
            makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
            makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize),
            makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
            makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
            makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                font: "bold " + nodeFontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
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
        [makePort("IN", false)]),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        [makePort("OUT", false)]));

        myDiagram.nodeTemplateMap.add("SupportNormal", supportNode);

        //#endregion

        //#region Fusion Template

        var nodeWidth = 225;
        var nodeHeight = 80;
        var nodeBorderColor = 'transparent';
        var nodeFontSize = 9;

        var supportNode = g(go.Node, "Spot",
        {
            mouseEnter: mouseEnter,
            mouseLeave: mouseLeave
        },
        g(go.Panel, "Auto", {
            width: nodeWidth,
            height: nodeHeight
        },
        g(go.Shape, "RoundedRectangle", {
            stroke: nodeBorderColor,
            strokeWidth: 2,
            spot1: go.Spot.TopLeft,
            spot2: go.Spot.BottomRight,
            name: "NodeShape"
        },
        new go.Binding("fill", "back").makeTwoWay()
        ),
        g(go.Panel, "Table",
            g(go.TextBlock, {
                row: 0,
                margin: 3,
                alignment: go.Spot.Top,
                editable: false,
                maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                font: "bold " + nodeFontSize + "pt sans-serif"
            },
                new go.Binding("text", "name").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 1,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
                new go.Binding("text", "typeName").makeTwoWay()
            ),
            g(go.TextBlock, {
                row: 2,
                margin: 3,
                maxSize: new go.Size(180, NaN),
                font: 'bold ' + (nodeFontSize - 2) + "pt sans-serif"
            },
                new go.Binding("stroke", "fore").makeTwoWay(),
                new go.Binding("text", "other").makeTwoWay()
            )
        )),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Left,
            alignmentFocus: new go.Spot(0, 0.5, -8, 0)
        },
        [makePort("IN", false)]),
        g(go.Panel, "Vertical", {
            alignment: go.Spot.Right,
            alignmentFocus: new go.Spot(1, 0.5, 8, 0)
        },
        [makePort("OUT", false)]));

        myDiagram.nodeTemplateMap.add("Fusion", supportNode);

        //#endregion

        //#region Default Link Template

        myDiagram.linkTemplateMap.add("", g(
                go.Link, {
                    routing: go.Link.AvoidsNodes,
                    corner: 10,
                    relinkableFrom: false,
                    relinkableTo: false
                }, // the whole link panel
                new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
                g(go.Shape, {
                    stroke: "gray", strokeWidth: 2
                },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
                g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" }), // the arrowhead
                g(go.Panel, "Auto",
                    g(go.Shape, {
                        visible: false,
                        fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                    ), // the link shape
                    g(go.TextBlock, {
                        textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                    },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                    )
                )
            )
        );

        //#endregion

        //#region Support Template

        myDiagram.linkTemplateMap.add("Support", g(
                go.Link, {
                    routing: go.Link.AvoidsNodes,
                    corner: 10,
                    relinkableFrom: false,
                    relinkableTo: false
                }, // the whole link panel
                g(go.Shape, {
                    stroke: "blue", strokeWidth: 2
                },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
                //g(go.Shape, { toArrow: "standard", fill: "blue", stroke: "blue" }), // the arrowhead
                g(go.Panel, "Auto",
                    g(go.Shape, {
                        visible: false,
                        fill: g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                    ), // the link shape
                    g(go.TextBlock, {
                        textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                    },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                    )
                )
            )
        );

        //#endregion

        //#endregion

        //#region MapItems

        var mapItemsSource = {
            dataType: "json",
            url: null,
            dataFields: [
                { name: 'MapItemID' },
                { name: 'SourceType' },
                { name: 'SourceName' },
                { name: 'Source' },
                { name: 'SourceID' },
                { name: 'SourceFusion' },
                { name: 'SourceFusionAttribute' },
                { name: 'SourceFusionAttributeType' },
                { name: 'TargetType' },
                { name: 'TargetName' },
                { name: 'Target' },
                { name: 'TargetID' },
                { name: 'TargetFusion' },
                { name: 'TargetFusionAttribute' },
                { name: 'TargetFusionAttributeType' }
            ]
        };

        var mapItemsAdapter = new $.jqx.dataAdapter(mapItemsSource);

        $('#Lineage_mappingrules_content').jqxGrid({
            source: mapItemsAdapter,
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
            theme: list_theme,
            columngroups: [
                { text: 'Source', name: 'S' },
                { text: 'Target', name: 'T' }
            ],
            columns: [
                {
                    datafield: "Source",
                    columngroup: "S",
                    text: "Business",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return '<span style="margin: 3px 0px 3px 0px"><b>' + data.SourceName + '</b><br/>' + data.SourceType + '</span>';
                    }
                },
                {
                    datafield: "SourceID",
                    columngroup: "S",
                    text: "Technical",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return '<span style="margin: 3px 0px 3px 0px">' + data.SourceFusion + '<br/>' + data.SourceFusionAttributeType + '<br/>' + data.SourceFusionAttribute + '</span>';
                    }
                },
                {
                    datafield: "Target",
                    columngroup: "T",
                    text: "Business",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return '<span style="margin: 3px 0px 3px 0px"><b>' + data.TargetName + '</b><br/>' + data.TargetType + '</span>';
                    }
                },
                {
                    datafield: "TargetID",
                    columngroup: "T",
                    text: "Technical",
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return '<span style="margin: 3px 0px 3px 0px">' + data.TargetFusion + '<br/>' + data.TargetFusionAttributeType + '<br/>' + data.TargetFusionAttribute + '</span>';
                    }
                }
            ]
        });

        $('#Lineage_mappingrules_content').on('bindingcomplete', mappingBindingComplete);

        //#endregion

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

        $('#Lineage_responsibilities_content').jqxGrid({
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
                { name: 'Object' },
                { name: 'ObjectID' },
                { name: 'ObjectName' },
                { name: 'ObjectUrl' },
                { name: 'ObjectTypeName' }
            ]
        };

        var technicalRelationsAdapter = new $.jqx.dataAdapter(technicalRelationsSource);

        $('#Lineage_fusion_content').jqxGrid({
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
                $(this).jqxGrid('autoresizecolumns');
            },
            columns: [
                //{ text: 'Type', groupable: true, datafield: 'TargetTypeName' },
                {
                    text: 'Name', groupable: false, datafield: 'ObjectName',
                    cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                        return textrenderer("<div class='cell-value-name'>" + data.ObjectName + "</div><div class='cell-value-type'>" + data.ObjectTypeName + "</div>");
                    }
                }
            ]
        });

        //#endregion

        //#region Methods

        function mappingBindingComplete(event) {
            try {
                $('#Lineage_mappingrules_content').jqxGrid('autoresizecolumns');
            } catch (e) { }
        }

        //function findLinkDataForKey(key) {
        //    for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
        //        if (myDiagram.model.linkDataArray[i].key == key)
        //            return myDiagram.model.linkDataArray[i];
        //    }
        //}

        //function findLinkByFromToIntersects(from, to) {
        //    for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
        //        if (myDiagram.model.linkDataArray[i].fromIntersectId == from && myDiagram.model.linkDataArray[i].toIntersectId == to)
        //            return myDiagram.model.linkDataArray[i];
        //    }
        //}

        //function findNodeIndexByObject(obj, objid) {
        //    for (var i = 0; i < myDiagram.model.nodeDataArray.length; i++) {
        //        if (myDiagram.model.nodeDataArray[i].obj == obj && myDiagram.model.nodeDataArray[i].objid == objid)
        //            return i
        //    }
        //    return -1;
        //}

        //function findLinkIndexByObjects(source, sourceid, target, targetid) {
        //    var sourceIx = findNodeIndexByObject(source, sourceid);
        //    var targetIx = findNodeIndexByObject(target, targetid);

        //    if (sourceIx < 0 || targetIx < 0)
        //        return -1;

        //    var sourceKey = myDiagram.model.nodeDataArray[sourceIx].key;
        //    var targetKey = myDiagram.model.nodeDataArray[targetIx].key;

        //    for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
        //        if (myDiagram.model.linkDataArray[i].from == sourceKey && myDiagram.model.linkDataArray[i].to == targetKey)
        //            return i;
        //    }
        //    return -1;
        //}

        //function getImmediateParents(key) {
        //    var parents = [];
        //    for (var i = 0; i < myDiagram.model.linkDataArray.length; i++) {
        //        if (myDiagram.model.linkDataArray[i].to == key) {
        //            parents.push(myDiagram.model.findNodeDataForKey(myDiagram.model.linkDataArray[i].from));
        //        }
        //    }
        //    return parents;
        //}

        function htmlDecode(s) {
            s = s.replace(/&#39;/g, '\'');
            s = s.replace(/&amp;/g, '&')
            s = s.replace(/&lt;/g, '<')
            s = s.replace(/&gt;/g, '>')
            s = s.replace(/&#34;/g, '"');

            return s;
        }

        function initializeDiagram() {
            var dg = g(go.Diagram, 'LineageDiagram', {
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

        function makeIconPanel(icon, tooltip, binding, fontSize) {
            fontSize -= 2;
            var iconPanel = g(go.Panel,
                "Auto",
                {
                    alignment: go.Spot.Center,
                    margin: 2
                },
                    g(go.Shape, "Circle",
                        {
                            stroke: null,
                            toolTip: g(go.Adornment, "Auto", g(go.Shape, { fill: "lightyellow" }), g(go.Panel, "Vertical", g(go.TextBlock, { margin: 3, text: tooltip })))
                        },
                    new go.Binding("fill", "fore")),
                g(go.TextBlock,
                    {
                        row: 0,
                        margin: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: (fontSize) + "pt FontAwesome",
                        text: icon,
                        toolTip: g(go.Adornment, "Auto", g(go.Shape, { fill: "lightyellow" }), g(go.Panel, "Vertical", g(go.TextBlock, { margin: 3, text: tooltip })))
                    },
                    new go.Binding("stroke", "back")
                ),
            new go.Binding("visible", binding)
            );

            return iconPanel;
        }

        function mouseEnter(e, node) {
            node.isShadowed = true;
        };

        function mouseLeave(e, node) {
            node.isShadowed = false;
        };

        function refreshControls(data) {
            toggleButtons(data);
            toggleTabs(data);
        }

        function toggleTabs(data) {

            if (!data || data == null) {

                $("#Lineage_info_detail").html('');

                try {
                    technicalRelationsSource.url = null;
                    $('#Lineage_fusion_content').jqxGrid('updatebounddata');
                } catch (e) { }

                try {
                    lineageResponsibilitySource.url = null;
                    $('#Lineage_responsibilities_content').jqxGrid('updatebounddata');
                } catch (e) { }

                try {
                    mapItemsSource.url = null;
                    $('#Lineage_mappingrules_content').jqxGrid('updatebounddata');
                } catch (e) { }

                $('#Lineage_sourcerules_content').html('');

            } else {
                if (data.diagramObjectType == 'Node') {

                    try {
                        if (data.obj && data.objid) {
                            $.ajax({
                                url: '/resources/' + data.obj + '/' + data.objid + '/templates/tooltip/Preview',
                                async: true
                            }).done(function (data) {
                                $('#Lineage_info_detail').html(data);
                            }).fail(function () {
                                $('#Lineage_info_detail').html(errorInfo);
                            });
                        }
                        else {
                            $("#Lineage_info_detail").html('');
                        }
                    } catch (e) { }

                    try {
                        if (lineageObject && lineageObjectID && data.obj && data.objid) {
                            technicalRelationsSource.url = '/relations/ChildRelationshipsBySourceAndTarget?s=' + lineageObject + '&sID=' + lineageObjectID + '&t=' + data.obj + '&tID=' + data.objid;
                        }
                        else {
                            technicalRelationsSource.url = null;
                        }
                        $('#Lineage_fusion_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        if (data.obj && data.objid) {
                            lineageResponsibilitySource.url = '/api/' + data.obj + '/' + data.objid + '/ownership?showHidden=false';
                        }
                        else {
                            lineageResponsibilitySource.url = null;
                        }
                        $('#Lineage_responsibilities_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        mapItemsSource.url = null;
                        $('#Lineage_mappingrules_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        if (lineageObject && lineageObjectID && data.obj && data.objid) {
                            $.ajax({
                                url: '/api/' + lineageObject + '/' + lineageObjectID + '/sources/' + data.obj + '/' + data.objid + '/rules',
                                async: true
                            }).done(function (data) {
                                var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                                $('#Lineage_sourcerules_content').html(sourceTemplate(data));
                            });
                        }
                        else {
                            $('#Lineage_sourcerules_content').html('');
                        }
                    } catch (e) { }

                } else if (data.diagramObjectType == 'Link') {

                    var from = myDiagram.model.findNodeDataForKey(data.from);
                    var to = myDiagram.model.findNodeDataForKey(data.to);

                    try {
                        if (lineageObject && lineageObjectID && from.obj && from.objid && to.obj && to.objid) {
                            $.ajax({
                                url: '/api/' + lineageObject + '/' + lineageObjectID + '/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/rules',
                                async: true
                            }).done(function (data) {
                                var sourceTemplate = Handlebars.getTemplate('LineageDiagramSourceRules');
                                $('#Lineage_sourcerules_content').html(sourceTemplate(data));
                            });
                        }
                        else {
                            $('#Lineage_sourcerules_content').html('');
                        }
                    } catch (e) { }

                    try {
                        if (from.template !== "Fusion" && to.template !== "Fusion") {
                            mapItemsSource.url = '/api/maps/' + from.obj + '/' + from.objid + '/' + to.obj + '/' + to.objid + '/mapitems';
                        }
                        else {
                            mapItemsSource.url = null;
                        }
                        $('#Lineage_mappingrules_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        technicalRelationsSource.url = null;
                        $('#Lineage_fusion_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        lineageResponsibilitySource.url = null;
                        $('#Lineage_responsibilities_content').jqxGrid('updatebounddata');
                    } catch (e) { }

                    try {
                        if (data.id) {
                            mapItemsSource.url = '/api/maps/' + data.id + '/mapitems';
                        }
                        else {
                            mapItemsSource.url = null;
                        }
                        $('#Lineage_info_detail').jqxGrid('updatebounddata');
                    } catch (e) { }

                }
            }
        }

        function toggleButtons(data) {
            if (data == null) {
                $('#LineageTools').jqxMenu('disable', 'w_info', true);
                $('#LineageTools').jqxMenu('disable', 'w_sourcerules', true);
                $('#LineageTools').jqxMenu('disable', 'w_mappings', true);
                $('#LineageTools').jqxMenu('disable', 'w_roles', true);
                $('#LineageTools').jqxMenu('disable', 'w_fusion', true);

                //$("#" + controlID_ribbon_sourcerule).hide(delay);
                //$("#" + controlID_ribbon_multimaprule).show(delay);
                //$("#" + controlID_ribbon_maprule).hide(delay);
            } else {
                //$("#" + controlID_ribbon_multimaprule).hide(delay);

                if (data.diagramObjectType == 'Node') {
                    $('#LineageTools').jqxMenu('disable', 'w_info', false);
                    $('#LineageTools').jqxMenu('disable', 'w_sourcerules', false);
                    $('#LineageTools').jqxMenu('disable', 'w_mappings', true);
                    $('#LineageTools').jqxMenu('disable', 'w_roles', false);
                    $('#LineageTools').jqxMenu('disable', 'w_fusion', false);
                } else {
                    $('#LineageTools').jqxMenu('disable', 'w_info', true);
                    $('#LineageTools').jqxMenu('disable', 'w_sourcerules', true);
                    $('#LineageTools').jqxMenu('disable', 'w_mappings', false);
                    $('#LineageTools').jqxMenu('disable', 'w_roles', true);
                    $('#LineageTools').jqxMenu('disable', 'w_fusion', true);
                }
            }
        }

        function parseData(data) {
            myDiagram.startTransaction("load_all_data");
            myDiagram.model.nodeDataArray = [];
            myDiagram.model.linkDataArray = [];
            initialNodes = [];
            initialLinks = [];
            var modelList = [];
            var linkList = [];
            if (data.nodes) {
                for (var i = 0; i < data.nodes.length; i++) {

                    var d = data.nodes[i];
                    var model = createNodeModel();

                    var isFocalPoint = (d.obj == lineageObject && d.objid == lineageObjectID);

                    if (isFocalPoint) {
                        //$('#' + controlID_header).text('Lineage: ' + htmlDecode(d.name));
                    }

                    model.template = d.template;// isFocalPoint ? "Focal" : "Normal";
                    model.key = d.key;
                    model.obj = d.obj;
                    model.objid = d.objid;
                    model.type = d.obj;
                    model.name = htmlDecode(d.name);
                    model.typeName = d.typeName;
                    model.fore = d.fore;
                    model.back = d.back;
                    model.diagramObjectType = "Node";
                    model.intersectId = d.intersectId;

                    model.sourceRuleCount = d.sourceRuleCount;
                    model.mappingRuleCount = d.mappingRuleCount;
                    model.hasSourceRules = (d.sourceRuleCount > 0);
                    model.hasMappingRules = (d.mappingRuleCount > 0);
                    model.challengeCount = d.challenges;
                    model.hasChallenges = (d.challenges > 0);
                    model.openEventCount = d.openEventCount;
                    model.hasOpenEvents = (d.openEventCount > 0);
                    model.openIssueCount = d.issues;
                    model.hasOpenIssues = (d.issues > 0);
                    model.hasTransformations = (d.transformationCount > 0);

                    model.mapItems = d.mapItems;

                    if (d.other)
                        model.other = htmlDecode(d.other);

                    modelList.push(model);
                }
            }

            if (data.links) {
                for (var i = 0; i < data.links.length; i++) {
                    var d = data.links[i];
                    var link = createLinkModel();
                    //myDiagram.model.setCategoryForLinkData(link, d.category);
                    //link.id = d.id;
                    //link.intersectTypeId = d.intersectTypeId;
                    //link.key = d.id;
                    link.Category = d.category;
                    link.from = d.from;
                    //link.fromIntersectId = d.fromIntersectId;
                    link.to = d.to;
                    //link.toIntersectId = d.toIntersectId;
                    //link.text = d.role;
                    //link.intersectRoleId = d.intersectRoleId;
                    link.diagramObjectType = "Link";
                    link.sourceMappingCount = d.mappingRuleCount;
                    link.hasMappingRules = (d.mappingRuleCount > 0);
                    link.hasTransformations = (d.transformation);
                    link.hasProperties = (link.hasTransformations || link.hasMappingRules);
                    link.mapItems = d.mapItems;
                    linkList.push(link);
                }
            }

            for (var i = 0; i < modelList.length; i++) {
                myDiagram.model.addNodeData(modelList[i]);
            }

            myDiagram.model.linkCategoryProperty = "Category";

            for (var i = 0; i < linkList.length; i++) {
                myDiagram.model.addLinkData(linkList[i]);
                myDiagram.model.setCategoryForLinkData(linkList[i], linkList[i].Category);
            }

            //get deep copy of lists
            initialNodes = $.extend(true, [], modelList);
            initialLinks = $.extend(true, [], linkList);

            refreshControls(null);  //set buttons/expanders to defaults

            myDiagram.commitTransaction("load_all_data");
            reOrderLayout();
        }

        function populateDiagram() {
            var results = $.ajax({
                url: '/diagrams/' + lineageObject + '/' + lineageObjectID + '/lineage/' + viewID,
                data: null
            }).done(function (data, status, xhr) {
                parseData(data);
                reOrderLayout();
                myDiagram.zoomToFit();
            });
        }

        function reOrderLayout() {
            myDiagram.layout.invalidateLayout();
            myDiagram.requestUpdate();
        }

        //#endregion

        //#region Diagram settings

        myDiagram.addDiagramListener('ViewportBoundsChanged', function () {
            var s = myDiagram.scale;
            var h = 500;
            if (s > 1) {
                h = h * s;
            }
            $('#LineageZoomSlider').val(Math.round(myDiagram.scale * 1500));
        });
        myDiagram.addDiagramListener('ChangedSelection', function (e) {
            selection = e.diagram.selection;

            if (selection.count == 0) {
                selectedData = null;
            } else {
                //get a deep copy of the selection as an array
                var sel = $.extend(true, [], selection.toArray());

                if (sel != null && sel.length != 0) {
                    selectedData = sel[0].data;
                }
            }

            refreshControls(selectedData);
        });
        myDiagram.addDiagramListener('ObjectDoubleClicked', function (e) {
            var obj = e.diagram.selection.first().data;
            if (obj != null) {
                if (obj.diagramObjectType == 'Node') {
                    lineageObject = obj.obj;
                    lineageObjectID = obj.objid;

                    populateDiagram();
                }
            }
        });

        myDiagram.grid.visible = false;
        myDiagram.grid.gridCellSize = new go.Size(8, 8);
        myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        //#endregion

        amplify.subscribe("PageResized", function () {
            sizePanel();
        });
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

        sizePanel();
        populateDiagram();
    });
}