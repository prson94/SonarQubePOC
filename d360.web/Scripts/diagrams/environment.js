function environment_diagram(controlID, permissions, type, id) {
    var self = this;

    var toolsControlID = controlID + "_tools";
    var diagramControlID = controlID + "_diagram";
    controlID = '#' + controlID;

    html = '<header>Lifecycle<div id="' + toolsControlID + '"></div></header>';
    html += '<div id="' + diagramControlID + '"></div>';
    $(controlID).html(html);
    diagramControlID = '#' + diagramControlID;
    toolsControlID = '#' + toolsControlID;

    if (permissions.HasPermission("Root", "Update")) {
        TileTools(toolsControlID, [
            { icon: 'plus', uri: "/form/AddSourcingResponsibility?type=" + type + "&id=" + id, context: contextList.SourcingResponsibility, title: 'Add source' }
        ]);
    }

    self.load = function () {
        d3.json("/diagrams/EnvironmentDetailsDiagramData?type=" + type + "&id=" + id, function (error, data) {
            $(diagramControlID).html('');

            if (data.children) {
                $(controlID).show();
                var customIcons = function (nodeEnter) {
                    var iconSize = 20,
                        h = 100,
                        w = 150,
                        p = w / 2;

                    if (permissions.HasPermission("Root", "Update")) {
                        nodeEnter.append('svg:text')
                            .attr("width", iconSize)
                            .attr("height", iconSize)
                            .attr("class", 'diagram-icon')
                            .style("fill", function (d) { return d.ForeColor; })
                            .style("display", function (d) { return ((d.AssigningItemType == type) && (d.AssigningItemID == id) ? 'inline-block' : 'none'); })
                            .attr("x", function (d) { return w - 40; })
                            .attr("y", function (d) { return h - 10; })
                            .text(function (d) { return "\uf040"; }) //fa-pencil
                            .on("click", function (d) {
                                amplify.publish("ToolAction", {
                                    uri: '/form/EditSourcingResponsibility?id=' + d.ID,
                                    context: 'responsibilityform'
                                });
                            });

                        nodeEnter.append('svg:text')
                            .attr("width", iconSize)
                            .attr("height", iconSize)
                            .attr("class", 'diagram-icon')
                            .style("fill", function (d) { return d.ForeColor; })
                            .style("display", function (d) { return ((d.AssigningItemType == type) && (d.AssigningItemID == id) ? 'inline-block' : 'none'); })
                            .attr("x", function (d) { return w - 20; })
                            .attr("y", function (d) { return h - 10; })
                            .text(function (d) { return "\uf014"; }) //fa-trash
                            .on("click", function (d) {
                                amplify.publish("ToolAction", {
                                    uri: '/form/DeleteResponsibility?id=' + d.ID,
                                    context: 'responsibilityform'
                                });
                            });
                    }

                };
                lineage(diagramControlID, 350, customIcons).nodes(data).render();
            }
            else {
                $(diagramControlID).hide();
            }
        });
    }

    self.load();

    amplify.subscribe("SaveAction", function (data) {
        try {
            switch (data.context) {
                case contextList.Artifact:
                case contextList.DomainList:
                //case contextList.Responsibility:
                case contextList.SourcingResponsibility:
                case contextList.Taxonomy:
                    self.load();
                    break;
            }
        } catch (e) { }
    });
}