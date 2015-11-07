function lineage_diagram(controlID, intersectID) {
    var self = this;

    //var toolsControlID = controlID + "_tools";
    var diagramControlID = controlID + "_diagram";
    controlID = '#' + controlID;
    
    var html = '';//'<header><div id="' + toolsControlID + '"></div></header>';
    html += '<div id="' + diagramControlID + '"></div>';
    $(controlID).html(html);
    diagramControlID = '#' + diagramControlID;
    //toolsControlID = '#' + toolsControlID;

    self.load = function () {
        d3.json("/diagrams/LineageDiagramData?id=" + intersectID, function (error, data) {
            var customIcons = function (nodeEnter) { };
            lineage(diagramControlID, 500, customIcons).nodes(data).render();
        });
    }

    self.load();

    //amplify.subscribe("SaveAction", function (data) {
    //    try {
    //        switch (data.context) {
    //            case contextList.IntersectSourcingResponsibility:
    //                self.load();
    //                break;
    //        }
    //    } catch (e) { }
    //});
}