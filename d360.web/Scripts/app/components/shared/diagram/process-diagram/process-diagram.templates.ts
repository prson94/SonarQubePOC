import * as go from 'gojs';

export class ProcessDiagramTemplates {
    private static fontColor: string = '#202020';
    private static textFont: string = `14px 'Source Sans Pro',sans-serif`;

    //event
    private static eventNodeRadius = 56;

    //gateway
    private static sideLength = 42;

    public static eventTemplate(component: any) {
        var $ = go.GraphObject.make;
        function showSmallPorts(node, show) {
            if (!(node as go.Node).isEnabled) {
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
                locationSpot: go.Spot.Center,
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                selectionChanged: (node) => {
                    component.onSelectionChanged(node)
                }
            },
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
                    this.makePort("T", go.Spot.Top, false, true),
                    this.makePort("L", go.Spot.Left, true, true),
                    this.makePort("R", go.Spot.Right, true, true),
                    this.makePort("B", go.Spot.Bottom, true, false),
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
                        stroke: 'black'
                    }
                    , new go.Binding("text", "Name").makeTwoWay())
            )
        );

    }


    static get activity_BodyPanel() {
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
                    textAlign: "center",
                    font: this.textFont,
                    editable: true,
                    margin: new go.Margin(6, 0, 0, 10),
                    isMultiline: true,
                    spacingBelow: 3,
                    maxSize: new go.Size(180, NaN),
                    wrap: go.TextBlock.WrapDesiredSize,
                },
                new go.Binding("text", "Name").makeTwoWay()
            )
        );
    }

    static get activity_HeaderPanel() {
        var $ = go.GraphObject.make;
        return $(go.Panel, 'Auto',
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
                }
                , new go.Binding("text", "icon").makeTwoWay()
            ),
            $(go.TextBlock,
                {
                    alignment: go.Spot.LeftCenter,
                    stroke: "white",
                    textAlign: "center",
                    font: this.textFont,
                    margin: new go.Margin(14, 0, 0, 30),
                    minSize: new go.Size(NaN, 24),
                }
                , new go.Binding("text", "governanceDisplayValue").makeTwoWay()
            )
        );
    }

    public static activityTemplate(component: any) {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        function showSmallPorts(node, show) {
            if (!(node as go.Node).isEnabled) {
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
                locationSpot: go.Spot.Center,
                selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate("RoundedRectangle"),
                selectionChanged: (node) => { component.onSelectionChanged(node) }

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
                    $(go.Panel, this.activity_HeaderPanel),
                    $(go.Panel, this.activity_BodyPanel)
                )
            ),
            this.makePort("T", go.Spot.Top, true, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, true),
            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            }
        );
    }

    public static gatewayTemplate(component: any) {
        var $ = go.GraphObject.make;
        function showSmallPorts(node, show) {
            if (!(node as go.Node).isEnabled) {
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
            { locationSpot: go.Spot.Center },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true, selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate(),
                selectionChanged: (node) => {
                    component.onSelectionChanged(node)
                }
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
                            stroke: "#708EA6",
                            fill: 'white',
                            strokeWidth: 2
                        },
                        new go.Binding("figure"),
                        new go.Binding("fill")),
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
                    this.makePort("T", go.Spot.Top, false, true),
                    this.makePort("L", go.Spot.Left, true, true),
                    this.makePort("R", go.Spot.Right, true, true),
                    this.makePort("B", go.Spot.Bottom, true, false),
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
                        stroke: 'black'
                    }
                    , new go.Binding("text", "Name").makeTwoWay())
            )
            ,

            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            },
        );
    }

    public static get linkTemplate() {
        var $ = go.GraphObject.make;
        var linkSelectionAdornmentTemplate =
            $(go.Adornment, "Link",
                $(go.Shape,
                    // isPanelMain declares that this Shape shares the Link.geometry
                    { isPanelMain: true, fill: null, stroke: "deepskyblue", strokeWidth: 0 })  // use selection object's strokeWidth
            );

        return $(go.Link,  // the whole link panel
            { selectable: true, selectionAdornmentTemplate: linkSelectionAdornmentTemplate },
            { relinkableFrom: true, relinkableTo: true, reshapable: true },
            {
                routing: go.Link.AvoidsNodes,
                curve: go.Link.JumpOver,
                corner: 5,
                toShortLength: 4
            },
            new go.Binding("points").makeTwoWay(),
            $(go.Shape,  // the link path shape
                { isPanelMain: true, strokeWidth: 1 }),
            $(go.Shape,  // the arrowhead
                { toArrow: "Standard", stroke: null }),
            $(go.Panel, "Auto",
                new go.Binding("visible", "isSelected").ofObject(),
                $(go.Shape, "RoundedRectangle",  // the link shape
                    { fill: "#F8F8F8", stroke: null }),
                $(go.TextBlock,
                    {
                        textAlign: "center",
                        font: "10pt helvetica, arial, sans-serif",
                        stroke: "#919191",
                        margin: 2,
                        minSize: new go.Size(10, NaN),
                        editable: true,

                    },
                    new go.Binding("name").makeTwoWay())
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


    private static makePort(name, spot: go.Spot, output, input, node = null) {
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
            });
    }




}