function lineage(diagramID, height, customIcons) {

    //#region Variables

    var _chart = {};
    var _width = $(diagramID).width(),
        _height = height,
        _boxHeight = 100,
        _boxWidth = 150,
        _svg,
        _nodes,
        _i = 0,
        _tree,
        _diagonal,
        _bodyG,
        initialY = 0,
        initialX = 0,
        maxZoom = 4,
        minZoom = 0.1,
        x = function (d) {
            return _width - d.y - _boxWidth;
        },
        y = function (d) {
            return d.x;
        };

    //#endregion

    _chart.render = function () {
        if (!_svg) {
            _svg = d3.select(diagramID)
                .append("svg")
                .attr("height", _height)
                .attr("width", _width)
                .call(
                    zm = d3.behavior.zoom().scaleExtent([minZoom, maxZoom]).on('zoom', zoom)
                );
        }

        renderBody(_svg);
    };

    function renderBody(svg) {
        if (!_bodyG) {
            _bodyG = svg.append("g")
                .attr("transform", function (d) {
                    return "translate(-100,0)";
                });
        }

        _tree = d3.layout.tree().size([_height - 30, _width]);

        _diagonal = d3.svg.diagonal().projection(function (d) {
            return [x(d) + _boxWidth, y(d) + (_boxHeight / 2)];
        });

        render(_nodes);
    }
    function render(source) {
        var nodes = _tree.nodes(_nodes);
        var h = renderNodes(nodes, source);
        _tree.size([h, _width]);
        renderLinks(nodes, source);
    }
    function getSize(d) {
        var bbox = this.getBBox(),
            cbbox = this.parentNode.getBBox(),
            scale = Math.min(cbbox.width / bbox.width, cbbox.height / bbox.height);
        
        d.scale = scale < 10 ? scale : 10;
    }
    function renderNodes(nodes, source) {

        var depth = 1;
        var bottomCount = 0;
        var height = 500;

        nodes.forEach(function (d) {
            if (d.depth > depth) depth = d.depth;
            d.y = d.depth * 175;
        });
        var node = _bodyG.selectAll("g.node")
            .data(nodes, function (d) {
                if (d.depth == depth) {
                    bottomCount++;
                }
                return d.id || (d.id = ++_i);
            });
        if (bottomCount > 3)
            height = bottomCount * 150;

        var nodeEnter = node.enter().append("g")
            .attr("class", "node")
            .attr("transform", function (d) {
                return "translate(" + x(d) + "," + y(d) + ")";
            });

        nodeEnter.append("rect")
            .attr('width', _boxWidth)
            .attr('height', _boxHeight)
            .attr('class', 'box-withoutnodes')
            .style("fill", function (d) { return d.BackColor; });

        nodeEnter.append("polygon")
             .style("fill", function (d) {
                 return '#fff';//d.BackColor;
             })
            .style('opacity', function (d) { return (d.depth > 0) ? .13 : 0; })
             .attr('points', '100,0 150,' + _boxHeight / 2 + ' 100,' + _boxHeight);


        nodeEnter.append("text")
            .attr("dx", function (d) { return _boxWidth / 2; })
            .attr("dy", function (d) { return 20; })
            .attr("text-anchor", "middle")
            .text(function (d) { return d.Name; })
            .style("font-size", "1px")
            .each(getSize)
            .style("font-size", function(d) { return d.scale + "px"; })
            .style("fill", function (d) { return d.ForeColor; });
            //.call(wrap, (_boxWidth - 5));

        nodeEnter.append("text")
            .attr("dx", function (d) { return _boxWidth / 2; })
            .attr("dy", function (d) { return 32; })
            .attr("text-anchor", "middle")
            .text(function (d) { return d.Type; })           
            .style("fill", function (d) { return d.ForeColor; });


        nodeEnter.append("text")
            .attr("dx", function (d) { return _boxWidth / 2; })
            .attr("dy", function (d) { return 44; })
            .attr("text-anchor", function (d) { return "middle"; })
            .text(function (d) { return (d.Role) ? '( ' + d.Role + ' )' : ""; })            
            .style("fill", function (d) { return d.ForeColor; });

        renderIcons(nodeEnter);

        return height;
    }
    function renderIcons(nodeEnter) {

        var iconSize = 15;
        var basePosition = _boxWidth / 2;

        nodeEnter.append('svg:text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Go to this item.')
            .attr("class", 'diagram-icon')
            .attr('data-context', 'Preview')
            .attr('data-type', function (d) { return d.ObjectType; })
            .attr('data-id', function (d) { return d.ObjectID; })
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return 10; })
            .attr("y", function (d) { return _boxHeight - 10; })
            .text(function (d) { return "\uf05a"; }) //fa-info
            .on("click", iconLink);

        nodeEnter.append('svg:text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Related Items')
            .attr("class", 'diagram-icon')
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return 28; })
            .attr("y", function (d) { return _boxHeight - 10; })
            .text(function (d) { return "\uf079"; }) //fa-retweet
            .on("click", iconRelationships);

        nodeEnter.append('svg:text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Source Applicable When...')
            .attr("class", 'diagram-icon')
            .style("opacity", function (d) { return (d.Contexts ? '1' : '0.3'); })
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return 46; })
            .attr("y", function (d) { return _boxHeight - 10; })
            .text(function (d) { return "\uf02c"; }) //fa-tags
            .on("click", iconContexts);

        nodeEnter.append('svg:text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Technical Relationships')
            .attr("class", 'diagram-icon')
            .style("opacity", function (d) { return (d.Relationships ? '1' : '0.3'); })
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return 64; })
            .attr("y", function (d) { return _boxHeight - 10; })
            .text(function (d) { return "\uf1c0"; }) //fa-database
            .on("click", iconTechnicalRelationships);

        nodeEnter.append('svg:text')
            .attr("width", iconSize)
            .attr("height", iconSize)
            .attr("title", 'Transformations')
            .attr("class", 'diagram-icon')
            .style("opacity", function (d) { return (d.MappingGroups || d.Mappings || d.Transformations ? '1' : '0.3'); })
            .style("fill", function (d) { return d.ForeColor; })
            .attr("x", function (d) { return 82; })
            .attr("y", function (d) { return _boxHeight - 10; })
            .text(function (d) { return "\uf0c3"; }) //fa-flask
            .on("click", iconTransformations);

        customIcons(nodeEnter);

    }
    function renderLinks(nodes, source) {

        var link = _bodyG.selectAll("path.link")
                .data(_tree.links(nodes), function (d) {
                    return d.target.id;
                });

        link.enter().insert("svg:path", "g")
            .attr("class", "link")
            //.attr('marker-end', 'url(#Arrowhead)')
            .attr("d", function (d) {
                var o = { x: source.x, y: source.y };
                return _diagonal({ source: o, target: o });
            });

        link.transition()
                .attr("d", _diagonal);
    }

    //#region Action/Icon functions

    function iconLink(d) {
        location.assign(d.Url);
    }

    function iconRelationships(d) {
        $(this).qtip({
            content: {
                title: 'Related Items',
                text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                ajax: {
                    url: '/diagrams/DiagramRelationshipsTooltip?type=' + d.ObjectType + '&id=' + d.ObjectID
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

    function iconContexts(d) {
        var content = "<div style='max-height: 300px; overflow-y: scroll'><table style='width:99%' class='striped hoverable responsive-table'>";
        content += "<thead><tr><th>Domain</th><th>Code</th><th>Name</th></tr></thead>";
        if (d.Contexts) {
            content += "<tbody>";
            d.Contexts.forEach(function (c) {
                content += "<tr>";
                content += "<td>" + c.Lookup + "</td>";
                content += "<td>" + c.Code + "</td>";
                content += "<td>" + c.Name + "</td>";
                content += "</tr>";
            });
            content += "</tbody>";
        }
        content += "</table></div>";

        $(this).qtip({
            content: {
                title: 'Source Applicable When...',
                text: content,
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
                width: '400',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
        });
    }

    function iconTechnicalRelationships(d) {
        var content = "<div style='max-height: 300px; overflow-y: scroll'><table style='width:99%' class='striped hoverable responsive-table'>";
        content += "<thead><tr><th>Type</th><th>Fusion</th><th>Name</th></tr></thead>";
        if (d.Relationships) {
            content += "<tbody>";
            d.Relationships.forEach(function (c) {
                content += "<tr>";
                content += "<td>" + c.Attribute + "</td>";
                content += "<td>" + c.Fusion + "</td>";
                content += "<td>" + c.Name + "</td>";
                content += "</tr>";
            });
            content += "</tbody>";
        }
        content += "</table></div>";

        $(this).qtip({
            content: {
                title: 'Technical Relationships',
                text: content,
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
                width: '500',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
        });
    }

    function iconTransformations(d) {
        var content = "<div style='max-height: 300px; overflow-y: scroll'>";

        if (d.MappingGroups || d.Mappings) {
            content += "<h3>Source To Target</h3>";
        }

        if (d.MappingGroups) {
            d.MappingGroups.forEach(function (c) {
                content += "<div class='FieldNameRequired'>Definition:</div>";
                content += "<div>" + c.Definition + "</div>";
                content += "<div class='FieldNameRequired'>Formula:</div>";
                content += "<div>" + c.Formula + "</div>";
            });
        }

        if (d.Mappings) {
            content += "<table style='width:99%' class='striped hoverable responsive-table'>";
            content += "<thead>";
            content += "<tr><th colspan='3' style='font-weight: 600; font-size: 125%;text-align:center;' class='blue lighten-4'>Source</th><th></th><th colspan='3' style='font-weight: 600; font-size: 125%;text-align:center;' class='blue lighten-4'>Target</th></tr>";
            content += "<tr><th>System</th><th>Object</th><th>Fusion</th><th></th><th>System</th><th>Object</th><th>Fusion</th></tr>";
            content += "</thead>";
            content += "<tbody>";
            d.Mappings.forEach(function (c) {
                content += "<tr style='vertical-align: top'>";
                content += "<td>" + c.SourceSystem + "</td>";
                content += "<td>" + c.SourceObject + "</td>";
                content += "<td>" + c.SourceFusionAttribute + "</td>";
                content += "<td></td>";
                content += "<td>" + c.TargetSystem + "</td>";
                content += "<td>" + c.TargetObject + "</td>";
                content += "<td>" + c.TargetFusionAttribute + "</td>";
                content += "</tr>";
            });
            content += "</tbody>";
            content += "</table>";
        }

        if (d.Transformations) {
            content += "<h3>Transformations</h3>";
            content += "<table style='width:99%' class='striped hoverable responsive-table'>";
            content += "<thead><tr><th>Type</th><th>Description</th></tr></thead>";
            content += "<tbody>";
            d.Transformations.forEach(function (c) {
                content += "<tr style='vertical-align: top'>";
                content += "<td>" + c.Type + "</td>";
                content += "<td>" + c.Description + "</td>";
                content += "</tr>";
            });
            content += "</tbody>";
            content += "</table>";
        }

        content += "</div>";

        $(this).qtip({
            content: {
                title: 'Transformations and Mappings',
                text: content,
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
                width: '1200',
                //height: '250',
                classes: 'qtip-light qtip-rounded'
            }
        });
    }

    //#endregion

    //#region Properties

    _chart.width = function (w) {
        if (!arguments.length) return _width;
        _width = w;
        return _chart;
    };

    _chart.height = function (h) {
        if (!arguments.length) return _height;
        _height = h;
        return _chart;
    };

    _chart.nodes = function (n) {
        if (!arguments.length) return _nodes;
        _nodes = n;
        return _chart;
    };

    //#endregion

    //#region Zoom/ Redraw functions

    //handle the zoom buttons
    function zoom() {
        _bodyG.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
    }

    //Redraw for zoom
    function redraw() {
        _svg.attr("transform",
            "translate(" + d3.event.translate + ")"
            + " scale(" + d3.event.scale + ")");
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

    //#endregion

    return _chart;
}