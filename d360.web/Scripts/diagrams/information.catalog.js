function information_catalog_diagram(controlID, typeID) {

    var diagramID = controlID + '_Diagram';
    var svgID = controlID + '_svg';

    var templateHtml = '';
    templateHtml += '<div class="zoomBar">';
    templateHtml += '<button type="button" rel="out" value="-" class="zoomBtn btn btn-info btn-xs"><i class="fa fa-minus"></i></button>';
    templateHtml += '<button type="button" rel="in" value="+" class="zoomBtn btn btn-info btn-xs"><i class="fa fa-plus"></i></button>';
    templateHtml += '<button type="button" rel="reset" value="reset" class="zoomBtn btn btn-info btn-xs"><i class="fa fa-refresh"></i></button>';
    templateHtml += '</div>';
    templateHtml += '<div id="' + diagramID + '"></div>';

    controlID = '#' + controlID;
    diagramID = '#' + diagramID;

    $(controlID).html(templateHtml);

    var tooltip = {
        tooltip0: "root tooltip",
        tooltip1: "level 1 tooltip",
        tooltip2: "level 2 tooltip",
        tooltip3: "level 3 tooltip",
        tooltip4: "level 4 tooltip",
        tooltip5: "level 5 tooltip"
    };

    var margin = {
        top: 20,
        right: 120,
        bottom: 20,
        left: 120
    },
    width = 1000;//$(controlID).width() - margin.right - margin.left,
    height = 600 - margin.top - margin.bottom;
                
    var i = 0,
        duration = 750,
        rectW = 100,
        rectH = 80;

    var initialY = 10;
    var initialX = $(diagramID).width() / 2;

    var color = d3.scale.category20();

    var tree = d3.layout.tree().nodeSize([rectW + 10, rectH + 20]);
    
    var longElbow = function (d, i) {
        var hy = (d.target.y - d.source.y) / 2;
        var rectDiff = (rectW / 2);
        return "M" + (d.source.x + rectDiff) + "," + d.source.y
                + "V" + (d.source.y + hy)
                + "H" + (d.target.x + rectDiff)
                + "V" + d.target.y;
    };

    var zoomOptions = {
        minZoomLevel: 0.1,
        maxZoomLevel: 5
    };

    var connector = longElbow;

    //var pageWidth = $(document).width();
    //var initialX = (pageWidth / 2) - 300;
    //var initialY = 20;
    //
    //var svg = d3.select("#body").append("svg").attr("width", pageWidth).attr("height", 1000)
    //       .call(zm = d3.behavior.zoom().scaleExtent([zoomOptions.minZoomLevel, zoomOptions.maxZoomLevel]).on("zoom", redraw)).append("g")
    //       .attr("transform", "translate(" + initialX + "," + initialY + ")");

    var svg = d3.select(diagramID)
        .append("svg")
        .attr("width", '100%')//$(diagramID).width())
        .attr("height", 600)
        .call(zm = d3.behavior.zoom().scaleExtent([zoomOptions.minZoomLevel, zoomOptions.maxZoomLevel]).on("zoom", redraw))
        .append("g")
        .attr("transform", "translate(" + initialX + "," + initialY + ")");

    //necessary so that zoom knows where to zoom and unzoom from
    zm.translate([initialX, initialY]);

    d3.json('/diagrams/InformationCatalogDiagramData?id=' + typeID, function (error, flare) {
        root = flare;
        root.x0 = height / 2;
        root.y0 = 0;

        function collapse(d) {
            if (d.children) {
                d._children = d.children;
                d._children.forEach(collapse);
                d.children = null;
            }
        }

        root.children.forEach(collapse);
        update(root);
    });

  
    $('.zoomBtn').on('click', function (e) {
        zoomBtn($(this).attr('rel'));
        e.preventDefault();
    })

       
    function update(source) {

        var iconSize = 25;

        // Compute the new tree layout.
        var nodes = tree.nodes(root).reverse(),
            links = tree.links(nodes);

        // Normalize for fixed-depth.
        nodes.forEach(function (d) {
            d.y = d.depth * 180;
        });

        // Update the nodes
        var node = svg.selectAll("g.node")
            .data(nodes, function (d) {
                return d.id || (d.id = ++i);
            });

        // Enter any new nodes at the parent's previous position.
        var nodeEnter = node.enter().append("g")
            .attr("class", "node")
            .attr("transform", function (d) {
                return "translate(" + source.x0 + "," + source.y0 + ")";
            });
            //.on("click", click);
            //.on("mouseover", function (d) {
            //    div.transition().duration(200).style("opacity", .9);
            //    div.html("Loading...")
            //       .style("left", (d3.event.pageX) + "px")
            //       .style("background", color(d.depth))
            //       .style("top", (d3.event.pageY - 28) + "px");
            //    //d3.json("tooltip.json", function (error, tooltip) {
            //    UpdateToolTip(tooltip, d);
            //    //})
            //})
            //.on("mouseout", function (d) {
            //    div.transition()
            //       .duration(500)
            //       .style("opacity", 0);
            //});

        nodeEnter.append("rect")
            .attr("width", rectW)
            .attr("height", rectH)
            .attr("ry", 3)
            .attr("rx", 3)
            //.attr("stroke", "black")                
            .attr("stroke", function (d) { return color(d.depth); })
            .attr("stroke-width", 1)
            .style("fill", function (d) {
                return d._children ? color(d.depth) : "#fff";
            });

        nodeEnter.append("text")
            .attr("x", rectW / 2)
            .attr("y", rectH / 2)                
            .attr("dy", "-.40em")
            .attr("text-anchor", "middle")
            .text(function (d) {
                return d.name.replace('/', ' / ');
            })
            .call(wrap, (rectW - 5))
            .on("click", click);
            
        nodeEnter.append('text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr('data-context', 'Preview')
            .attr('data-type', 'Taxonomy')
            .attr('data-id', function(d) { return d.id; })
            .attr("title", 'Go to this item.')
            .attr("class", 'diagram-icon')
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return (rectW/2) - 14; })
            .attr("y", function (d) { return rectH - 3; })
            .text(function (d) { return "\uf05a"; }) //fa-info
            .on("click", iconLink);

        nodeEnter.append('text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Related Items')
            .attr("class", 'diagram-icon')
            .style("opacity", function (d) { return (d.RelationshipsExist ? '1' : '0.3'); })
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return (rectW / 2) + 2; })
            .attr("y", function (d) { return rectH - 3; })
            .text(function (d) { return "\uf079"; }) //fa-retweet
            .on("click", iconRelationships);

        // Transition nodes to their new position.
        var nodeUpdate = node.transition()
            .duration(duration)
            .attr("transform", function (d) {
                return "translate(" + d.x + "," + d.y + ")";
            });

        nodeUpdate.select("rect")
            .attr("width", rectW)
            .attr("height", rectH)
            //.attr("stroke", "black")
            .attr("stroke", function (d) { return color(d.depth); })
            .attr("stroke-width", 1)
            .style("fill", function (d) {
                return d._children ? color(d.depth) : "#fff";
            });
            

        nodeUpdate.select("text")
            .style("fill-opacity", 1);
                

        // Transition exiting nodes to the parent's new position.
        var nodeExit = node.exit().transition()
            .duration(duration)
            .attr("transform", function (d) {
                return "translate(" + source.x + "," + source.y + ")";
            })
            .remove();

        nodeExit.select("rect")
            .attr("width", rectW)
            .attr("height", rectH)            
            //.attr("stroke", "black")
            .attr("stroke", function (d) { return color(d.depth); })
            .attr("stroke-width", 1);
            

        nodeExit.select("text");

        // Update the links¦
        var link = svg.selectAll("path.link")
            .data(links, function (d) {
                return d.target.id;
            });

        // Enter any new links at the parent's previous position.
        link.enter().insert("path", "g")
            .attr("class", "link")
            .attr("x", rectW / 2)
            .attr("y", rectH / 2)
            .attr("d", function (d) {
                var o = {
                    x: source.x0,
                    y: source.y0
                };
                return connector({
                    source: o,
                    target: o
                });
            });

        // Transition links to their new position.
        link.transition().duration(duration).attr("d", connector);

        // Transition exiting nodes to the parent's new position.
        link.exit().transition()
            .duration(duration)
            .attr("d", function (d) {
                var o = {
                    x: source.x,
                    y: source.y
                };
                return connector({
                    source: o,
                    target: o
                });
            })
            .remove();

        // Stash the old positions for transition.
        nodes.forEach(function (d) {
            d.x0 = d.x;
            d.y0 = d.y;
        });
    }

    //handle the zoom buttons
    function zoomBtn(action) {
        var currentZoom = zm.scale();
        var zoomScale = 1;

        if (currentZoom <= 1) zoomScale = 0.1;

        if (action == 'reset') {
            zm.scale(1).translate([initialX, initialY]).event(svg);
        }
        else if (action == 'in') {
            if (currentZoom < zoomOptions.maxZoomLevel) {
                var newScale = currentZoom + zoomScale;

                console.log(newScale);
                zm.scale(newScale)
                    .event(svg);
            }
        } else {
            if (currentZoom > zoomOptions.minZoomLevel) {
                var newScale = currentZoom - zoomScale;

                console.log(newScale);

                zm.scale(newScale)
                    .event(svg);
            }
        }
    }

    function iconLink(d) {
        location.assign(d.url);
    }

    function iconRelationships(d) {
        console.log(d);
        $(this).qtip({
            content: {
                title: 'Related Items',
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/diagrams/DiagramRelationshipsTooltip?type=Taxonomy&id=' + d.id
                }
            },
            position: {
                at: 'bottom center', // Position the tooltip above the link
                my: 'top center',
                viewport: $(window), // Keep the tooltip on-screen at all times
                effect: false // Disable positioning animation
            },
            overwrite: false,
            show: {
                event: event.type,  // show using same event as above.
                solo: false,         // Only show one tooltip at a time
                ready: true
            },
            hide: {
                fixed: true,
                delay: 500,
            },
            style: {
                width: '700',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
        });
    }

    // Toggle children on click.
    function click(d) {
        if (d.children) {
            d._children = d.children;
            d.children = null;
        } else {
            d.children = d._children;
            d._children = null;
        }
        update(d);
    }

    //Redraw for zoom
    function redraw() {
        //return; //quick fix to stop diagram from moving around.  stops all translation from working though.

        //console.log("here", d3.event.translate, d3.event.scale);
        //if (d3.event.scale != 1) {
            svg.attr("transform",
            "translate(" + d3.event.translate + ")"
            + " scale(" + d3.event.scale + ")");
        //}
    }

    function wrap(text, width) {
        text.each(function () {
            var text = d3.select(this),
                words = text.text().split(/\s+/).reverse(),
                word,
                line = [],
                lineNumber = 0,
                lineHeight = 1.1, // ems
                y = text.attr("y"),
                x = text.attr("x"),
                dy = parseFloat(text.attr("dy")),
                tspan = text.text(null).append("tspan").attr("x", x).attr("y", y).attr("dy", dy + "em");
            while (word = words.pop()) {
                line.push(word);
                tspan.text(line.join(" "));
                if (tspan.node().getComputedTextLength() > width) {
                    line.pop();
                    tspan.text(line.join(" "));
                    line = [word];
                    tspan = text.append("tspan").attr("x", x).attr("y", y).attr("dy", ++lineNumber * lineHeight + dy + "em").text(word);
                }
            }
        });
    }
}