import * as go from 'gojs';

export class ProcessDiagramTemplates {
    private static fontColor: string = '#202020';

    static get activityTemplate() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Node, "Auto",
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate("RoundedRectangle"),

            },
            $(go.Shape, "RoundedRectangle",
                {
                    fill: "#708EA6",
                    stroke: "#708EA6",
                    portId: "",
                    strokeWidth: 2,
                    fromLinkable: true,
                    toLinkable: true,
                    margin: new go.Margin(1, 1, 1, 1),
                    cursor: "pointer",
                }),
            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    { stretch: go.GraphObject.Horizontal },  // as wide as the whole node
                    $(go.Shape,
                        {
                            fill: "#708EA6",
                            stroke: "#708EA6",
                            strokeWidth: 2,

                            minSize: new go.Size(200, NaN)
                        }),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.LeftCenter,
                            stroke: "white",
                            textAlign: "center",
                            font: '18px FontAwesome',
                            margin: new go.Margin(6, 0, 0, 12),
                            minSize: new go.Size(NaN, 24)
                        }
                        , new go.Binding("text", "icon").makeTwoWay()
                    )),
                $(go.TextBlock,
                    {
                        alignment: go.Spot.LeftCenter,
                        background: "white",
                        stroke: this.fontColor,
                        textAlign: "center",
                        font: "bold 12pt sans-serif",
                        editable: true,
                        minSize: new go.Size(NaN, 30),
                        margin: new go.Margin(12, 0, 0, 5)

                    },
                    new go.Binding("text", "name").makeTwoWay()
                )
            ),
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            //{ // handle mouse enter/leave events to show/hide the ports
            //    mouseEnter: function (e, node) { showSmallPorts(node, true); },
            //    mouseLeave: function (e, node) { showSmallPorts(node, false); }
            //}
        );
    }

    static get eventTemplate() {
        var $ = go.GraphObject.make;
        return $(go.Node, "Spot",
            {
                locationSpot: go.Spot.Center,
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate()
            },
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            //{
            //    mouseEnter: function (e, node) { showSmallPorts(node, true); },
            //    mouseLeave: function (e, node) { showSmallPorts(node, false); }
            //},
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            $(go.Panel, "Vertical",
                {
                    name: "PANEL"
                },
                $(go.Panel, "Auto",
                    {
                        name: "PANEL"
                    },
                    $(go.Shape, "Circle",
                        {
                            fill: 'transparent',
                            stroke: "black",
                            strokeWidth: 2,
                            visible: false
                        },
                        new go.Binding('visible', 'isSelected').ofObject()),
                    $(go.Shape, "Circle",
                        {
                            portId: "",
                            fromLinkable: true,
                            toLinkable: true,
                            cursor: "pointer",
                            stroke: "#708EA6",
                            fill: 'white',
                            strokeWidth: 2,
                            width: 70,
                            height: 70,
                        }),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.Center,
                            stroke: '#708EA6',
                            textAlign: "center",
                            font: '48px FontAwesome',
                            margin: new go.Margin(5, 0, 0, 0)
                        },
                        new go.Binding("text", "icon").makeTwoWay())
                ),
                $(go.TextBlock,
                    {
                        font: "bold 11pt Helvetica, Arial, sans-serif",
                        margin: 8,
                        wrap: go.TextBlock.WrapFit,
                        editable: true,
                        stroke: 'black'
                    }
                    , new go.Binding("text", "name").makeTwoWay())
            )
        );
    }

    static get gatewayTemplate() {
        var $ = go.GraphObject.make;

        return $(go.Node, "Spot",
            { locationSpot: go.Spot.Center },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            { selectable: true, selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate() },
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
                            width: 58,
                            height: 58,
                            fill: 'transparent',
                            stroke: "black",
                            strokeWidth: 2,
                            visible: false
                        },
                        new go.Binding('visible', 'isSelected').ofObject()),
                    $(go.Shape, "Rectangle",
                        {
                            angle: 45,
                            width: 50,
                            height: 50,
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
                            stroke: '#708EA6',
                            textAlign: "center",
                            font: '32px FontAwesome'
                        },
                        new go.Binding("text", "icon").makeTwoWay())
                ),
                $(go.TextBlock,
                    {
                        font: "bold 11pt Helvetica, Arial, sans-serif",
                        margin: 8,
                        maxSize: new go.Size(160, NaN),
                        wrap: go.TextBlock.WrapFit,
                        editable: true,
                        stroke: 'black'
                    }
                    , new go.Binding("text", "name").makeTwoWay())
            )
            ,
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),

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
                { isPanelMain: true, strokeWidth: 2 }),
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


    private static makePort(name, spot, output, input) {
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
                fromSpot: spot, toSpot: spot,  // declare where links may connect at this port
                fromLinkable: output, toLinkable: input,  // declare whether the user may draw links to/from here
                cursor: "pointer"  // show a different cursor to indicate potential link point
            });
    }

    private showSmallPorts(node, show) {
        node.ports.each(function (port) {
            if (port.portId !== "") {  // don't change the default port, which is the big shape
                port.fill = show ? "rgba(0,0,0,0.3)" : null;
            }
        });
    }
}