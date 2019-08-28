import * as _ from 'lodash';
import * as go from 'gojs';
import {MenuItem} from 'primeng/primeng';
import {AfterViewInit, Component, EventEmitter, Input, OnInit, Output, ViewChild} from '@angular/core';

import {
    DiagramObjectType,
    LineageEditorModel,
    LineageEditorTechnicalModel,
    LineageView,
    LinkModel,
    NodeModel,
} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';

import {DiagramBaseComponent} from '../diagram-base.component';
import { MessagesObservableService } from '../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-lineage-editor-preview',
    templateUrl: './lineage-editor-preview.component.html',
    providers: [DiagramService]
})

export class LineageEditorPreviewComponent extends DiagramBaseComponent implements OnInit, AfterViewInit {
    @Input() businessModel: LineageEditorModel = new LineageEditorModel();
    @Input() technicalModel: LineageEditorTechnicalModel = new LineageEditorTechnicalModel();
    @Input() type: string;
    @Input() id: number;
    @Input() view: LineageView = LineageView.SystemFlow;
    @Input() height: number = 300;
    @Input() header: string = "Preview Lineage Changes";
    @Output() viewChange = new EventEmitter();
    @ViewChild('diagram') diagramRef;

    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];

    public menuItems: MenuItem[] = [];

    constructor(private diagramService: DiagramService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.initializeMenuItems();
        this.initializeDiagram();
    }

    ngAfterViewInit() {
        this.resizeDiagram();
    }

    private initializeDiagram() {
        this.diagram = this.createDiagram();

        this.diagram.nodeTemplateMap.add("Focal", this.createFocalNode());
        this.diagram.nodeTemplateMap.add("Normal", this.createNormalNode());
        this.diagram.nodeTemplateMap.add("SupportFocal", this.createSupportFocalNode());
        this.diagram.nodeTemplateMap.add("SupportNormal", this.createSupportNormalNode());
        this.diagram.nodeTemplateMap.add("Fusion", this.createFusionNode());

        this.diagram.linkTemplateMap.add("", this.createDefaultLink());
        this.diagram.linkTemplateMap.add("Support", this.createSupportLink());

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.populateDiagram();
    }

    private initializeMenuItems() {
        this.menuItems = [];

        let eye: MenuItem = {
            icon: 'fa fa-eye',
            items: []
        };

        let eyeSub: MenuItem[] = [
            {label: 'Business System Flow'},
            {label: 'Business Data Flow'},
            {label: 'Technical Lineage'}
        ];

        eye.items = eyeSub;

        this.menuItems.push(
            eye,
            {icon: 'fa fa-search-minus'},
            {icon: 'fa fa-search-plus'}
        );
    }

    public menuClick(e: MenuItem) {
        if (e.icon == 'fa fa-search-plus') {
            this.diagram.scale += .1;

            if (this.diagram.scale > 2.5) {
                this.diagram.scale = 2.5;
            }
        } else if (e.icon == 'fa fa-search-minus') {
            this.diagram.scale -= .1;

            if (this.diagram.scale < .1) {
                this.diagram.scale = .1;
            }
        } else if (e.label == 'Business System Flow') {
            this.view = LineageView.SystemFlow;
            this.viewChange.emit(LineageView.SystemFlow);
            this.populateDiagram();
        } else if (e.label == 'Business Data Flow') {
            this.view = LineageView.DataFlow;
            this.viewChange.emit(LineageView.DataFlow);
            this.populateDiagram();
        } else if (e.label == 'Technical Lineage') {
            this.view = LineageView.Technical;
            this.viewChange.emit(LineageView.Technical);
            this.populateDiagram();
        }
    }

    private populateDiagram() {
        this.isLoading = true;
        this.businessModel.Existing = [];
        this.technicalModel.Existing = [];

        this.diagramService.previewLineage(
            this.type,
            this.id,
            this.view,
            this.businessModel,
            this.technicalModel
        ).subscribe(
            data => {
                this.parseData(data);

                this.diagram.zoomToFit();

                this.isLoading = false;
            }
        );
    }

    private parseData(data: any) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.initialNodes = [];
        this.initialLinks = [];
        var modelList = [];
        var linkList = [];

        if (data.nodes) {
            for (var i = 0; i < data.nodes.length; i++) {
                var d = data.nodes[i];
                var model = new NodeModel();
                var isFocalPoint = (d.obj == this.businessModel.Focal && d.objid == this.businessModel.FocalID);

                model.template = d.template;
                model.key = d.key;
                model.obj = d.obj;
                model.objid = d.objid;
                model.type = d.obj;
                model.name = this.htmlDecode(d.name);
                model.typeName = d.typeName;
                model.fore = d.fore;
                model.back = d.back;
                model.diagramObjectType = DiagramObjectType.Node;
                model.intersectId = d.intersectId;

                model.sourceRuleCount = d.sourceRuleCount;
                model.mappingRuleCount = d.mappingRuleCount;
                model.hasSourceRules = d.HasSourceRules;
                model.hasMappingRules = (d.mappingRuleCount > 0);
                model.actionCount = d.actions;
                model.hasActions = (d.actions > 0);
                model.hasTransformations = (d.transformationCount > 0);

                model.mapItems = d.mapItems;

                if (d.other) {
                    model.other = this.htmlDecode(d.other);
                }

                modelList.push(model);
            }
        }

        if (data.links) {
            for (var i = 0; i < data.links.length; i++) {
                var d = data.links[i];
                var link = new LinkModel();

                link.Category = d.category;
                link.from = d.from;
                link.to = d.to;
                link.diagramObjectType = DiagramObjectType.Link;
                link.sourceMappingCount = d.mappingRuleCount;
                link.hasMappingRules = (d.mappingRuleCount > 0);
                link.hasTransformations = (d.transformation);
                link.hasProperties = (link.hasTransformations || link.hasMappingRules);
                link.mapItems = d.mapItems;
                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            let n = modelList[i];

            if (n.key.startsWith('0.S') || n.key.startsWith('0.T')) {
                //combine editor nodes with existing items if applicable
                let node = modelList.find(m => m.obj == n.obj && m.objid == n.objid && m.key != n.key);

                if (node != null) {
                    let sourceLinks = linkList.filter(l => (<any>l).to == node.key).map(l => (<any>l).from);
                    let nLinks = linkList.filter(l => (<any>l).to == n.key).map(l => (<any>l).from);
                    let isAdded = false;

                    sourceLinks.forEach(s => {
                        let commonLink = nLinks.find(l => l == s);

                        if (commonLink != null) {
                            let toLinks = linkList.filter(l => (<any>l).to == n.key);
                            let fromLinks = linkList.filter(l => (<any>l).from == n.key);

                            toLinks.forEach(l => {
                                l.to = node.key;
                            });

                            fromLinks.forEach(l => {
                                l.from = node.key;
                            });

                            isAdded = true;

                            return;
                        }
                    });
                    if (!isAdded) {
                        this.diagram.model.addNodeData(modelList[i]);
                    }
                } else {
                    this.diagram.model.addNodeData(modelList[i]);
                }
            } else {
                this.diagram.model.addNodeData(modelList[i]);
            }
        }

        dm.linkCategoryProperty = "Category";

        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
            dm.setCategoryForLinkData(linkList[i], linkList[i].Category);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.diagram.commitTransaction("load_all_data");
    }

    private htmlDecode(val: string): string {
        val = val.replace(/&#39;/g, '\'');
        val = val.replace(/&amp;/g, '&');
        val = val.replace(/&lt;/g, '<');
        val = val.replace(/&gt;/g, '>');
        val = val.replace(/&#34;/g, '"');

        return val;
    }

    //#region events

    private resizeDiagram() {
        this.diagramRef.nativeElement.style.height = this.height + 'px';
    }

    private onMouseEnterNode(e: any, node: go.Node) {
        node.isShadowed = true;
    }

    private onMouseLeaveNode(e: any, node: go.Node) {
        node.isShadowed = false;
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        let dg = this.g(go.Diagram, 'LineagePreviewDiagram', {
            initialContentAlignment: go.Spot.Left,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: this.g(go.LayeredDigraphLayout, {direction: 0, columnSpacing: 50, layerSpacing: 50}),
            "undoManager.isEnabled": false
        });

        let model = (dg.model as go.GraphLinksModel);

        model.nodeCategoryProperty = "template";
        model.linkFromPortIdProperty = "frompid";
        model.linkToPortIdProperty = "topid";
        model.nodeDataArray = [];
        model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = false;
        dg.model.isReadOnly = true;

        return dg;
    }


    private createFocalNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 150;
        let nodeBorderColor = '#000000';
        let nodeFontSize = 14;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                    width: nodeWidth,
                    height: nodeHeight
                },
                this.g(go.Shape, "RoundedRectangle", {
                        stroke: nodeBorderColor,
                        strokeWidth: 2,
                        spot1: go.Spot.TopLeft,
                        spot2: go.Spot.BottomRight,
                        name: "NodeShape"
                    },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
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
                    this.g(go.TextBlock, {
                            row: 1,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Left,
                    alignmentFocus: new go.Spot(0, 0.5, -8, 0)
                },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Right,
                    alignmentFocus: new go.Spot(1, 0.5, 8, 0)
                },
                [this.makePort("OUT", false)]));
    }

    private createNormalNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 105;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                    width: nodeWidth,
                    height: nodeHeight
                },
                this.g(go.Shape, "RoundedRectangle", {
                        stroke: nodeBorderColor,
                        strokeWidth: 2,
                        spot1: go.Spot.TopLeft,
                        spot2: go.Spot.BottomRight,
                        name: "NodeShape"
                    },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
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
                    this.g(go.TextBlock, {
                            row: 1,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Left,
                    alignmentFocus: new go.Spot(0, 0.5, -8, 0)
                },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Right,
                    alignmentFocus: new go.Spot(1, 0.5, 8, 0)
                },
                [this.makePort("OUT", false)]));
    }

    private createSupportFocalNode(): go.Node {
        let nodeWidth = 140;
        let nodeHeight = 80;
        let nodeBorderColor = '#000000';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                    width: nodeWidth,
                    height: nodeHeight
                },
                this.g(go.Shape, "RoundedRectangle", {
                        stroke: nodeBorderColor,
                        strokeWidth: 2,
                        spot1: go.Spot.TopLeft,
                        spot2: go.Spot.BottomRight,
                        name: "NodeShape"
                    },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
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
                    this.g(go.TextBlock, {
                            row: 1,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Left,
                    alignmentFocus: new go.Spot(0, 0.5, -8, 0)
                },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Right,
                    alignmentFocus: new go.Spot(1, 0.5, 8, 0)
                },
                [this.makePort("OUT", false)]));

    }

    private createSupportNormalNode(): go.Node {
        let nodeWidth = 130;
        let nodeHeight = 70;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                    width: nodeWidth,
                    height: nodeHeight
                },
                this.g(go.Shape, "RoundedRectangle", {
                        stroke: nodeBorderColor,
                        strokeWidth: 2,
                        spot1: go.Spot.TopLeft,
                        spot2: go.Spot.BottomRight,
                        name: "NodeShape"
                    },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
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
                    this.g(go.TextBlock, {
                            row: 1,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Left,
                    alignmentFocus: new go.Spot(0, 0.5, -8, 0)
                },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Right,
                    alignmentFocus: new go.Spot(1, 0.5, 8, 0)
                },
                [this.makePort("OUT", false)]));
    }

    private createFusionNode(): go.Node {
        let nodeWidth = 225;
        let nodeHeight = 80;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                    width: nodeWidth,
                    height: nodeHeight
                },
                this.g(go.Shape, "RoundedRectangle", {
                        stroke: nodeBorderColor,
                        strokeWidth: 2,
                        spot1: go.Spot.TopLeft,
                        spot2: go.Spot.BottomRight,
                        name: "NodeShape"
                    },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
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
                    this.g(go.TextBlock, {
                            row: 1,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                            row: 2,
                            margin: 3,
                            maxSize: new go.Size(180, NaN),
                            font: 'bold ' + (nodeFontSize - 2) + "pt sans-serif"
                        },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "other").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Left,
                    alignmentFocus: new go.Spot(0, 0.5, -8, 0)
                },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                    alignment: go.Spot.Right,
                    alignmentFocus: new go.Spot(1, 0.5, 8, 0)
                },
                [this.makePort("OUT", false)]));
    }

    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
            this.g(go.Shape, {
                    stroke: "gray", strokeWidth: 2
                },
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", function (h) {
                    return h ? "black" : "gray"
                })), // the link shape
            this.g(go.Shape, {toArrow: "standard", fill: "gray", stroke: "gray"}), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                        visible: false,
                        fill: this.g(go.Brush, "Radial", {
                            0: "rgb(255, 255, 255)",
                            0.3: "rgb(255, 255, 255)",
                            1: "rgba(255, 255, 255, 0)"
                        }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) {
                        return !!a
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                        textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                    },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    private createSupportLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            this.g(go.Shape, {
                    stroke: "blue", strokeWidth: 2
                },
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", function (h) {
                    return h ? "black" : "gray"
                })), // the link shape
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                        visible: false,
                        fill: this.g(go.Brush, "Radial", {
                            0: "rgb(255, 255, 255)",
                            0.3: "rgb(255, 255, 255)",
                            1: "rgba(255, 255, 255, 0)"
                        }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) {
                        return !!a
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                        textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                    },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }


    private makeIconPanel(icon, tooltip, binding, fontSize) {
        fontSize -= 2;
        let iconPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 2
            },
            this.g(go.Shape, "Circle",
                {
                    stroke: null,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, {fill: "lightyellow"}), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: tooltip
                    })))
                },
                new go.Binding("fill", "fore")),
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: (fontSize) + "pt FontAwesome",
                    text: icon,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, {fill: "lightyellow"}), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: tooltip
                    })))
                },
                new go.Binding("stroke", "back")
            ),
            new go.Binding("visible", binding)
        );

        return iconPanel;
    }

    private makePort(name: string, leftside: boolean) {
        var port = this.g(go.Shape, "Circle", {
            fill: "white",
            stroke: "gray",
            strokeWidth: 3,
            desiredSize: new go.Size(9, 9),
            portId: name, // declare this object to be a "port"
            cursor: "pointer" // show a different cursor to indicate potential link point
        });

        var panel = this.g(go.Panel, "Horizontal", {
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

    //#endregion
}
