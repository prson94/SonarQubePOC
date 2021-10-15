import * as go from 'gojs';
import { ProcessDiagramComponent } from './process-diagram.component';

//Note: If any change to templates causes node overall box to be bigger or smaller, 
//    for all nodes locationSpot propery should be updated for event and gateway nodes so nodes can snap correctly.
//    As node content can change nodes height we cannot use go.Spo.Center

export class ProcessDiagramTemplates {
    private static fontColor: string = '#202020';
    private static textFont: string = `14px 'Precisely'`;
    private static textFont12: string = `12px 'Precisely'`;

    //event
    private static eventNodeRadius = 48;

    //gateway
    private static sideLength = 42;


    private static palleteItemFillColor: string = '#f1f2f3';
    private static palleteItemStrokeColor: string = '#597897';


    static activity_BodyPanel(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;

        return $(go.Panel, 'Auto',
            {
                stretch: go.GraphObject.Horizontal
            },
            $(go.Shape,
                {
                    fill: "white",
                    minSize: new go.Size(200, 36),
                },
                new go.Binding('stroke', 'refItemColor').makeTwoWay()
            ),
            $(go.TextBlock,
                {
                    background: 'white',
                    alignment: go.Spot.LeftCenter,
                    stroke: this.fontColor,
                    textAlign: "left",
                    font: this.textFont,
                    editable: true,
                    margin: new go.Margin(6, 0, 0, 10),
                    isMultiline: true,
                    spacingBelow: 3,
                    maxSize: new go.Size(180, NaN),
                    wrap: go.TextBlock.WrapDesiredSize,
                    textValidation: function (tb: go.TextBlock, oldVal, newVal) {
                        component.dynEditorService.updateForm({ assetUid: tb.part.data.key, fieldName: 'Name', fieldValue: newVal });
                        return true;
                    }
                },
                new go.Binding("text", "Name").makeTwoWay()
            )
        );
    }

    static activity_HeaderPanel(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;

        function isColorLight(color) {
            const hex = color.replace('#', '');
            const c_r = parseInt(hex.substr(0, 2), 16);
            const c_g = parseInt(hex.substr(2, 2), 16);
            const c_b = parseInt(hex.substr(4, 2), 16);
            const brightness = ((c_r * 299) + (c_g * 587) + (c_b * 114)) / 1000;
            return brightness > 155;
        }

        function getGovernanceRoleValue(data: any) {
            var val = data.governanceDisplayValue as string;
            if (val) {
                var hasIcon = !!data.icon;
                var hasRelationship = +data.relCount > 0;
                var trimSize = val.length;

                if (hasIcon || hasRelationship)
                    trimSize = 27;

                if (!hasIcon && !hasRelationship)
                    trimSize = 30;

                if (hasIcon && hasRelationship)
                    trimSize = 22;

                if (val.length > trimSize)
                    return val.substring(0, trimSize - 1) + '...';
            }
            return val;
        }

        var headerPanel = $(go.Panel, 'Auto',
            {
                stretch: go.GraphObject.Horizontal
            },
            $(go.Shape,
                {
                    strokeWidth: 1,
                    minSize: new go.Size(200, NaN),
                    maxSize: new go.Size(200, 32)
                },
                new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                new go.Binding('fill', 'refItemColor').makeTwoWay()

            ),
            $(go.TextBlock,
                {
                    alignment: go.Spot.LeftCenter,
                    stroke: "white",
                    textAlign: "center",
                    font: '14px FontAwesome',
                    margin: new go.Margin(6, 0, 0, 12),
                    minSize: new go.Size(NaN, 24)
                },
                new go.Binding("text", "icon").makeTwoWay(),
                new go.Binding("visible", "icon", function (icon) {
                    if (!icon)
                        return false;
                    return true;
                }),
                new go.Binding("stroke", "", function (data) {
                    if (isColorLight(data.refItemColor)) {
                        return "#202020";
                    }
                    return "white";
                })
            ),
            $(go.TextBlock,
                {
                    alignment: go.Spot.LeftCenter,
                    stroke: "white",
                    textAlign: "left",
                    font: this.textFont,
                    margin: new go.Margin(12, 0, 0, 34),
                    minSize: new go.Size(160, 24),
                },
                new go.Binding("text", "", function (data) {
                    return getGovernanceRoleValue(data);
                }),
                new go.Binding("margin", "icon", function (icon) {
                    if (!icon)
                        return new go.Margin(12, 0, 0, 8);
                    return new go.Margin(12, 0, 0, 34);
                }),
                new go.Binding("stroke", "", function (data) {
                    if (isColorLight(data.refItemColor)) {
                        return "#202020";
                    }
                    return "white";
                })
            ),
            this.getRelBadge('activity', component)
        );

        headerPanel.toolTip = $("ToolTip", {
            visible: false,
        },
            new go.Binding("visible", "", function (data) {
                var val = getGovernanceRoleValue(data);
                return val.indexOf('...') > 0 ? true : false;
            }
            ),

            $(go.TextBlock, {
                margin: 4,
            },
                new go.Binding("text", "governanceDisplayValue")
            )
        );

        return headerPanel;
    }

    private static getRelBadge(type: string, component: ProcessDiagramComponent): go.Panel {
        var $ = go.GraphObject.make;
        var RectangleMargin = new go.Margin(5, 5, 0, 0);
        if (type == 'gateway') {
            var RectangleMargin = new go.Margin(28, 28, 0, 0);
        }
        if (type == 'event') {
            var RectangleMargin = new go.Margin(24, 38, 0, 0);
        }

        var badge = $(go.Panel, 'Spot',
            {
                alignment: go.Spot.TopRight,
                cursor: 'pointer',
                click: (node) => {
                    component.doControlledAction('open-related-assets');
                }
            },
            $(go.Shape, "Rectangle",
                {
                    maxSize: new go.Size(NaN, 22),
                    margin: RectangleMargin,
                    fill: '#006fba',
                    strokeWidth: 1,
                    stroke: "white"
                },
                new go.Binding("maxSize", "relCount", function (v) {
                    var defaultWidth = 28;
                    return new go.Size(defaultWidth, 22);
                }),
                new go.Binding("visible", "relCount", function (v) {
                    if (v > 0) return true;
                    return false;
                })
            ),
            $(go.TextBlock,
                {
                    font: this.textFont12,
                    textAlign: "center",
                    stroke: 'white'
                },
                new go.Binding("text", "relCount").makeTwoWay(),
                new go.Binding("visible", "relCount", function (v) {
                    if (v > 0) return true;
                    return false;
                })
            )
        );

        badge.toolTip = $("ToolTip",
            $(go.TextBlock, {
                margin: 4,
                text: "View and edit relationships"
            })
        );

        return badge;
    }


    private static linkValidation(fromnode: go.Node, fromport, tonode: go.Node, toport) {

        var doesLinkExist: boolean = false;
        try {
            var links = fromnode.diagram.links.filter(x => x.data);
            doesLinkExist = links.any(x => (x.data.from == fromnode.data.key && x.data.to == tonode.data.key)
                || (x.data.from == tonode.data.key && x.data.to == fromnode.data.key));

            if (doesLinkExist && fromport != toport) {
                doesLinkExist = false;
            }
        }
        catch (ex) {
            console.warn(ex);
            doesLinkExist = false;
        }
        return !doesLinkExist;
    }

    public static eventTemplate(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;
        function showSmallPorts(node, show) {
            if (node.diagram && (node as go.Node).diagram.isReadOnly) {
                return;
            }
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? 'white' : null;
                    port.stroke = show ? 'black' : null;
                }
            });
        }
        return $(go.Node, "Spot",
            {
                locationSpot: new go.Spot(0.5, 0, 0, 56),
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                width: 112.2,
                cursor: 'move',
                linkValidation: this.linkValidation
            }
            ,
            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    $(go.Shape,
                        {
                            fill: "transparent",
                            stroke: "black",
                            strokeWidth: 2,
                            geometryString: 'M 230 230 A 45 45, 0, 1, 1, 230,229',
                            width: this.eventNodeRadius + 6,
                            height: this.eventNodeRadius + 6

                        },
                        new go.Binding('visible', 'isSelected').ofObject(),
                    ),
                    $(go.Panel, 'Auto',
                        $(go.Shape, "Circle",
                            {
                                portId: "",
                                fromLinkable: true,
                                toLinkable: true,
                                cursor: "pointer",
                                fill: 'transparent',
                                strokeWidth: 2,
                                width: this.eventNodeRadius,
                                height: this.eventNodeRadius,
                                margin: new go.Margin(2, 2, 2, 2)
                            },
                            new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                        ),
                        $(go.TextBlock,
                            {
                                alignment: go.Spot.Center,
                                stroke: '#708EA6',
                                textAlign: "center",
                                font: '32px FontAwesome',
                                margin: new go.Margin(4, 0, 0, 0)
                            },
                            new go.Binding("text", "icon").makeTwoWay(),
                            new go.Binding("stroke", "refItemColor").makeTwoWay())
                    ),
                    this.makePort("T", go.Spot.Top),
                    this.makePort("L", go.Spot.Left),
                    this.makePort("R", go.Spot.Right),
                    this.makePort("B", go.Spot.Bottom),
                )
                ,
                $(go.TextBlock,
                    {
                        font: this.textFont,
                        margin: 4,
                        textAlign: "center",
                        spacingBelow: 3,
                        maxSize: new go.Size(120, NaN),
                        wrap: go.TextBlock.WrapDesiredSize,
                        editable: true,
                        stroke: 'black',
                        textValidation: function (tb: go.TextBlock, oldVal, newVal) {
                            component.dynEditorService.updateForm({ assetUid: tb.part.data.key, fieldName: 'Name', fieldValue: newVal });
                            return true;
                        }
                    }
                    , new go.Binding("text", "Name").makeTwoWay())
            ),
            this.getRelBadge('event', component)
        );

    }
    public static activityTemplate(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        function showSmallPorts(node, show) {
            if (node.diagram && (node as go.Node).diagram.isReadOnly) {
                return;
            }
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? 'white' : null;
                    port.stroke = show ? 'black' : null;
                }
            });
        }

        return $(go.Node, "Spot",
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true,
                locationSpot: new go.Spot(0.5, 0, 0, 24),
                selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate("RoundedRectangle"),
                cursor: 'move',
                linkValidation: this.linkValidation
            },
            $(go.Panel, 'Auto',
                $(go.Shape, "RoundedRectangle",
                    {
                        portId: "",
                        strokeWidth: 1,
                        fromLinkable: true,
                        toLinkable: true,
                        margin: new go.Margin(2, 2, 2, 2),
                        cursor: "pointer",
                    },
                    new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                    new go.Binding('fill', 'refItemColor').makeTwoWay()
                ),
                $(go.Panel, 'Vertical',
                    $(go.Panel, this.activity_HeaderPanel(component)),
                    $(go.Panel, this.activity_BodyPanel(component))
                )
            ),
            this.makePort("T", go.Spot.Top),
            this.makePort("L", go.Spot.Left),
            this.makePort("R", go.Spot.Right),
            this.makePort("B", go.Spot.Bottom),
            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            }
        );
    }

    public static gatewayTemplate(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;
        function showSmallPorts(node, show) {
            if (node.diagram && (node as go.Node).diagram.isReadOnly) {
                return;
            }
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? 'white' : null;
                    port.stroke = show ? 'black' : null;
                }
            });
        }
        return $(go.Node, "Spot",
            {
                locationSpot: new go.Spot(0.5, 0, -15.5, 62.5),
                cursor: 'move',
                linkValidation: this.linkValidation
            },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true, selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate()
            },
            new go.Binding("angle").makeTwoWay(),
            $(go.Panel, "Vertical",
                { name: "PANEL" },
                new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                $(go.Panel, "Auto",
                    { name: "PANEL" },
                    new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                    $(go.Shape, "Rectangle",
                        {
                            angle: 45,
                            width: this.sideLength + 8,
                            height: this.sideLength + 8,
                            fill: 'transparent',
                            stroke: "black",
                            strokeWidth: 2,
                            visible: false
                        },
                        new go.Binding('visible', 'isSelected').ofObject()),
                    $(go.Shape, "Rectangle",
                        {
                            angle: 45,
                            width: this.sideLength,
                            height: this.sideLength,
                            portId: "",
                            fromLinkable: true,
                            toLinkable: true,
                            cursor: "pointer",
                            fill: 'white',
                            strokeWidth: 2
                        },
                        new go.Binding("figure"),
                        new go.Binding("fill"),
                        new go.Binding("stroke", "refItemColor").makeTwoWay()),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.Center,
                            margin: new go.Margin(5, 0, 0, 0),
                            textAlign: "center",
                            font: '24px FontAwesome',
                            minSize: new go.Size(24, 24)
                        },
                        new go.Binding("text", "icon").makeTwoWay(),
                        new go.Binding("stroke", "refItemColor").makeTwoWay()),
                    this.makePort("T", go.Spot.Top),
                    this.makePort("L", go.Spot.Left),
                    this.makePort("R", go.Spot.Right),
                    this.makePort("B", go.Spot.BottomCenter),

                ),
                $(go.TextBlock,
                    {
                        font: this.textFont,
                        margin: 4,
                        textAlign: "center",
                        spacingBelow: 3,
                        maxSize: new go.Size(120, NaN),
                        wrap: go.TextBlock.WrapDesiredSize,
                        editable: true,
                        stroke: 'black',
                        textValidation: function (tb: go.TextBlock, oldVal, newVal) {
                            component.dynEditorService.updateForm({ assetUid: tb.part.data.key, fieldName: 'Name', fieldValue: newVal });
                            return true;
                        }
                    }
                    , new go.Binding("text", "Name").makeTwoWay())

            ),
            this.getRelBadge('gateway', component)
            ,
            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            },
        );
    }
    public static deletedNodeTemplate(component: ProcessDiagramComponent) {
        var $ = go.GraphObject.make;  // for conciseness in defining templates


        return $(go.Node, "Spot",
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                locationSpot: new go.Spot(0.5, 0, 0, 24),
                cursor: 'default',
                movable: false
            },
            $(go.Panel, 'Auto',
                $(go.Shape, "RoundedRectangle",
                    {
                        portId: "",
                        strokeWidth: 1,
                        fromLinkable: true,
                        toLinkable: true,
                        margin: new go.Margin(2, 2, 2, 2),
                        cursor: "pointer",
                    },
                    new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                    new go.Binding('fill', 'refItemColor').makeTwoWay()
                ),
                $(go.Panel, 'Vertical',
                    $(go.Panel, 'Auto',
                        {
                            stretch: go.GraphObject.Horizontal
                        },
                        $(go.Shape,
                            {
                                strokeWidth: 1,
                                minSize: new go.Size(200, NaN),
                                maxSize: new go.Size(200, 32)
                            },
                            new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                            new go.Binding('fill', 'refItemColor').makeTwoWay()

                        ),
                        $(go.TextBlock,
                            {
                                alignment: go.Spot.LeftCenter,
                                stroke: "#b21a3e",
                                textAlign: "center",
                                font: '14px FontAwesome',
                                margin: new go.Margin(6, 0, 0, 6),
                                minSize: new go.Size(NaN, 24)
                            }
                            , new go.Binding("text", "icon").makeTwoWay()
                        ),
                    ),
                    $(go.Panel, 'Auto',
                        {
                            stretch: go.GraphObject.Horizontal
                        },
                        $(go.Shape,
                            {
                                fill: "white",
                                minSize: new go.Size(200, 36),
                            },
                            new go.Binding('stroke', 'refItemColor').makeTwoWay(),
                            new go.Binding('fill', 'refItemColor').makeTwoWay()),
                        $(go.TextBlock,
                            {
                                background: 'white',
                                alignment: go.Spot.LeftCenter,
                                stroke: 'white',
                                textAlign: "center",
                                font: this.textFont,
                                editable: true,
                                margin: new go.Margin(0, 0, 0, 10),
                                isMultiline: true,
                                spacingBelow: 3,
                                maxSize: new go.Size(180, NaN),
                                wrap: go.TextBlock.WrapDesiredSize,
                            },
                            new go.Binding("text", "Name").makeTwoWay(),
                            new go.Binding('background', 'refItemColor').makeTwoWay()
                        )
                    )
                )
            ),
            this.makePort("T", go.Spot.Top),
            this.makePort("L", go.Spot.Left),
            this.makePort("R", go.Spot.Right),
            this.makePort("B", go.Spot.Bottom)
        );
    }

    public static get linkTemplate() {
        var $ = go.GraphObject.make;

        function isDirectLink(link: go.Link) {
            let directLinks: string[] = ['BT', 'TB', 'LR', 'RL'];

            var dir = link.data.fromPort + link.data.toPort;

            if (directLinks.indexOf(dir) != -1)
                return true;

            return false;
        }

        function getPortsDistance(link: go.Link) {
            var from = link.fromNode;
            var to = link.toNode;

            var distance = Math.sqrt(from.position.distanceSquaredPoint(to.position));
            return distance;
        }

        return $(go.Link,  // the whole link panel
            {
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                curviness: 50,
                relinkableFrom: true,
                relinkableTo: true,
                reshapable: true,
                routing: go.Link.AvoidsNodes,
                curve: go.Link.JumpOver,
                corner: 5,
                toShortLength: 4,
                cursor: 'pointer',
                toEndSegmentLength: 10
            },
            new go.Binding("points").makeTwoWay(),
            new go.Binding("layerName", "isSelected", function (selected) {
                return selected ? 'Foreground' : '';
            }).ofObject(),
            new go.Binding("fromEndSegmentLength", "", function (link: go.Link) {
                if (isDirectLink(link) && getPortsDistance(link) < 150) {
                    return 10;
                }
                return link.data.label ? 50 : 10;
            }).ofObject(),
            $(go.Shape,
                {
                    isPanelMain: true,
                    strokeWidth: 1
                },
                new go.Binding("stroke", "isSelected", function (data) {
                    return data ? '#166aa8' : '#000000';
                }).ofObject(),
                new go.Binding("fill", "isSelected", function (data) {
                    return data ? '#166aa8' : '#000000';
                }).ofObject(),
                new go.Binding("strokeWidth", "isSelected", function (data) {
                    return data ? 3 : 1;
                }).ofObject()),
            $(go.Shape,  // the arrowhead
                {
                    toArrow: "Standard",
                    stroke: null,
                    fill: null
                },
                new go.Binding("stroke", "isSelected", function (data) {
                    return data ? '#166aa8' : '#000000';
                }).ofObject(),
                new go.Binding("fill", "isSelected", function (data) {
                    return data ? '#166aa8' : '#000000';
                }).ofObject()
            ),
            $(go.Panel, "Auto", {
                segmentIndex: 0,
                segmentOffset: new go.Point(50, 0),
                toolTip: $("ToolTip",
                    $(go.TextBlock, {
                        margin: 4,
                        text: "View and edit relationships"
                    },
                        new go.Binding("text", "label").makeTwoWay()
                    )
                )
            },

                new go.Binding("visible", "", function (data) {
                    return data.data.label ? true : false;
                }).ofObject(),
                new go.Binding("segmentOffset", "", function (link: go.Link) {
                    if (isDirectLink(link) && getPortsDistance(link) < 150) {
                        return new go.Point(20, 0);
                    }
                    return new go.Point(50, 0);
                }).ofObject(),
                new go.Binding("segmentFraction", "", function (link: go.Link) {
                    if (isDirectLink(link) && getPortsDistance(link) < 150) {
                        return 0.5;
                    }
                    return null;
                }).ofObject(),
                $(go.Shape, "RoundedRectangle",  // the link shape
                    {
                        fill: "#166aa8",
                        stroke: "#166aa8",
                        strokeWidth: 4
                    },
                    new go.Binding("stroke", "isSelected", function (data) {
                        return data ? '#166aa8' : '#000000';
                    }).ofObject(),
                    new go.Binding("fill", "isSelected", function (data) {
                        return data ? '#166aa8' : '#000000';
                    }).ofObject()),
                $(go.TextBlock,
                    {
                        textAlign: "center",
                        font: this.textFont12,
                        background: "#166aa8",
                        stroke: "white",
                        minSize: new go.Size(20, NaN),
                        maxSize: new go.Size(60, NaN),
                        margin: new go.Margin(2, 2, 2, 2)
                    },
                    new go.Binding("text", "", function (node: go.Link) {
                        if (node.data.label) {
                            var label = node.data.label as string;
                            if (label.length > 20)
                                return label.substr(0, 20) + '...';

                            return label;
                        }
                        return "";
                    }).ofObject(),
                    new go.Binding("background", "isSelected", function (data) {
                        return data ? '#166aa8' : '#000000';
                    }).ofObject())
            )
        );
    }


    private static nodeSelectionAdornmentTemplate(shape: string) {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Auto",
            $(go.Shape, shape, { fill: null, stroke: "black", strokeWidth: 2 }),
            $(go.Placeholder)
        );
    }

    private static nodeSelectionEmptyTemplate() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Auto",
            $(go.Shape, { fill: null, stroke: null, strokeWidth: 0 }),
            $(go.Placeholder)
        );
    }


    private static makePort(name, spot: go.Spot) {
        var $ = go.GraphObject.make;
        // the port is basically just a small transparent square
        return $(go.Shape, "Circle",
            {

                fill: null,  // not seen, by default; set to a translucent gray by showSmallPorts, defined below
                stroke: null,
                desiredSize: new go.Size(7, 7),
                alignment: spot,  // align the port on the main Shape
                alignmentFocus: spot,  // just inside the Shape
                portId: name,  // declare this object to be a "port"
                fromSpot: spot,
                toSpot: spot,  // declare where links may connect at this port
                fromLinkable: true,
                toLinkable: true,  // declare whether the user may draw links to/from here
                cursor: "pointer"  // show a different cursor to indicate potential link point
            }
        );
    }



    public static eventTemplate_pallete() {
        var $ = go.GraphObject.make;

        return $(go.Node, "Spot",
            {
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                cursor: 'pointer',
                toolTip: this.GetTooltip()
            },
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    $(go.Shape, 'Rectangle',
                        {
                            fill: this.palleteItemFillColor,
                            strokeWidth: 0,
                            width: 80,
                            height: 80,
                            cursor: 'pointer'
                        }
                    ),
                    $(go.Panel, 'Auto',
                        $(go.Shape, "Circle",
                            {
                                portId: "",
                                fromLinkable: true,
                                toLinkable: true,
                                cursor: "pointer",
                                fill: 'white',
                                stroke: this.palleteItemStrokeColor,
                                strokeWidth: 2,
                                width: this.eventNodeRadius,
                                height: this.eventNodeRadius,
                                margin: new go.Margin(2, 2, 2, 2)
                            }
                        ),
                        $(go.TextBlock,
                            {
                                alignment: go.Spot.Center,
                                stroke: this.palleteItemStrokeColor,
                                textAlign: "center",
                                font: '30px FontAwesome',
                                margin: new go.Margin(4, 0, 0, 0)
                            },
                            new go.Binding("text", "icon").makeTwoWay())
                    )
                )
                ,
                $(go.TextBlock,
                    {
                        font: this.textFont12,
                        margin: new go.Margin(8, 4, 4, 4),
                        textAlign: "center",
                        spacingBelow: 3,
                        maxSize: new go.Size(80, NaN),
                        maxLines: 2,
                        wrap: go.TextBlock.WrapDesiredSize,
                        editable: true,
                        stroke: this.fontColor
                    }
                    , new go.Binding("text", "Name").makeTwoWay())
            )
        );

    }

    public static activityTemplate_pallete() {
        var $ = go.GraphObject.make;

        return $(go.Node, "Spot",
            {
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                cursor: 'pointer',
                toolTip: this.GetTooltip(),
            },
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    $(go.Shape, 'Rectangle',
                        {
                            fill: this.palleteItemFillColor,
                            strokeWidth: 0,
                            width: 80,
                            height: 80,
                            cursor: 'pointer'
                        }
                    ),
                    $(go.Panel, 'Auto',
                        $(go.Shape, "RoundedRectangle",
                            {
                                fill: this.palleteItemStrokeColor,
                                strokeWidth: 0,
                                width: 48,
                                height: 48
                            }
                        ),
                        $(go.TextBlock,
                            {
                                alignment: go.Spot.Center,
                                stroke: 'white',
                                textAlign: "center",
                                font: '28px FontAwesome',
                                margin: new go.Margin(4, 0, 0, 0)
                            },
                            new go.Binding("text", "icon").makeTwoWay())
                    )
                )
                ,
                $(go.TextBlock,
                    {
                        font: this.textFont12,
                        margin: new go.Margin(8, 4, 4, 4),
                        textAlign: "center",
                        spacingBelow: 3,
                        maxSize: new go.Size(80, NaN),
                        maxLines: 2,
                        wrap: go.TextBlock.WrapDesiredSize,
                        editable: true,
                        stroke: this.fontColor
                    }
                    ,
                    new go.Binding("text", "Name").makeTwoWay())
            )
        );

    }

    public static gatewayTemplate_pallete() {
        var $ = go.GraphObject.make;

        return $(go.Node, "Spot",
            {
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                cursor: 'pointer',
                toolTip: this.GetTooltip(),

            },
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    $(go.Shape, 'Rectangle',
                        {
                            fill: this.palleteItemFillColor,
                            strokeWidth: 0,
                            width: 80,
                            height: 80,
                            cursor: 'pointer'
                        }
                    ),
                    $(go.Panel, 'Auto',
                        $(go.Shape, "Rectangle",
                            {
                                stroke: this.palleteItemStrokeColor,
                                strokeWidth: 2,
                                fill: 'white',
                                width: 35,
                                height: 35,
                                angle: 45
                            }
                        ),
                        $(go.TextBlock,
                            {
                                alignment: go.Spot.Center,
                                stroke: this.palleteItemStrokeColor,
                                textAlign: "center",
                                font: '24px FontAwesome',
                                margin: new go.Margin(4, 0, 0, 0)
                            },
                            new go.Binding("text", "icon").makeTwoWay())
                    )
                )
                ,
                $(go.TextBlock,
                    {
                        font: this.textFont12,
                        margin: new go.Margin(8, 4, 4, 4),
                        textAlign: "center",
                        spacingBelow: 3,
                        maxSize: new go.Size(80, NaN),
                        maxLines: 2,
                        wrap: go.TextBlock.WrapDesiredSize,
                        editable: true,
                        stroke: this.fontColor
                    }
                    , new go.Binding("text", "Name").makeTwoWay())
            )
        );

    }

    public static blankTemplate_pallete() {
        var $ = go.GraphObject.make;

        return $(go.Node, "Spot",
            {
                cursor: 'default',
                selectable: false,

            },
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    $(go.Shape, 'Rectangle',
                        {
                            fill: 'transparent',
                            strokeWidth: 0,
                            width: 80,
                            height: 80,
                        }
                    ))));
    }

    private static showToolTip(obj: go.GraphObject, diagram: go.Diagram, tool: go.Tool) {
        var toolTipDIV = document.getElementById('toolTipDIV');

        var partPos = obj.part.diagram.transformDocToView(obj.part.position);
        var diaPos = obj.diagram.div.getBoundingClientRect();

        toolTipDIV.style.left = (partPos.x + diaPos.x - 28) + "px";
        toolTipDIV.style.top = (partPos.y + diaPos.y) + "px";
        document.getElementById('toolTipParagraph').innerHTML = obj['data'].PopupDescription;
        if (obj['data'].PopupDescription)
            toolTipDIV.style.display = "block";
    }

    private static hideToolTip() {
        var toolTipDIV = document.getElementById('toolTipDIV');
        toolTipDIV.style.display = "none";
    }

    private static GetTooltip() {
        var $ = go.GraphObject.make;

        return $(go.HTMLInfo, {
            show: this.showToolTip,
            hide: this.hideToolTip,
        });
    }

    private static newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}