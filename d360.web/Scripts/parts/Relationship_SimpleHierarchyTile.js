function Relationship_SimpleHierarchyTile(controlID, contextList, permissions, type, id) {
    var headerControlID = controlID + "Header";
    var treeControlID = controlID + "SimpleHierarchyTree";

    var headerTitle = "";

    //#region Build HTML

    var html = '';
    html += '<header>' + headerTitle + '</header>';
    html += '<div class="row">';
    html += '<div class="col s12">';
    html += '<div id="' + treeControlID + '"></div>';
    html += '</div>';
    html += '</div>';

    //#endregion

    //#region Set proper jquery prefix on controls

    controlID = '#' + controlID;
    headerControlID = '#' + headerControlID;
    treeControlID = '#' + treeControlID;

    //#endregion

    //#region Clean up previous control logic before re-creating

    $(controlID).html('');
    $(controlID).html(html);

    //#endregion

    var loadHierarchy = function (node, html) {
        try {
            if (node) {

                html = "<ul>";
                html += "<li item-expanded='true'>";

                html += (node.ObjectType == type && node.ObjectID == id) ? "" : "<a data-context='Preview' data-type='" + node.ObjectType + "' data-id='" + node.ObjectID + "' href='" + node.ObjectUrl + "'>";
                html += (node.ObjectType == type && node.ObjectID == id) ? "<b>" + node.ObjectName + "</b>" : node.ObjectName;
                html += (node.ObjectType == type && node.ObjectID == id) ? "" : "</a>";

                if (node.Items.length > 0) {
                    $.each(node.Items, function () {
                        html += loadHierarchy(this);
                    });
                }

                html += "</li>";
                html += "</ul>";
            }
        } catch (e) {
            logError("Relationship_SimpleHierarchyTile : loadHierarchy", e);
        }

        return html;
    }

    $.getJSON('/relations/SimpleHierarchies', { type: type, id: id }, function (data) {
        if (data) {
            // Loop through each top-level flow hierarchy.  There could be multiple.
            $.each(data, function () {
                $(treeControlID).append("<h4>" + this.FlowTypeName + "</h4>");

                var tree = $("<div style='border: none !important;'></div>");
                tree.append(loadHierarchy(this, ""));
                $(treeControlID).append(tree);
                //tree.jqxTree({ theme: theme });
            });
        }
    });

    //#region Event Subscriptions

    function unsubscribe(data) {

        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}