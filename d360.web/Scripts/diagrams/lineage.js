function lineage_diagram(controlID, intersectID) {
    var self = this;

    var diagramControlID = controlID + "_diagram";
    controlID = '#' + controlID;
    
    var html = '';
    html += '<div id="' + diagramControlID + '"></div>';
    $(controlID).html(html);
    diagramControlID = '#' + diagramControlID;

    self.load = function () {
        d3.json("/diagrams/LineageDiagramData?id=" + intersectID, function (error, data) {
            var customIcons = function (nodeEnter) { };
            lineage(diagramControlID, 500, customIcons).nodes(data).render();
        });
    }

    self.load();
}