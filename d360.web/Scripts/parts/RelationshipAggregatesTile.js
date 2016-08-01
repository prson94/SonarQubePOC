function RelationshipAggregatesTile(controlID, type, id, permissions) {

    var chartsExist;
    var parent;
    var toolsControlID = controlID + "_tools";
    var gridControlID = controlID + "_grid";

    try {
        controlID = '#' + controlID;

        parent = $(controlID);

        var html = "";
        html += "<header>Relationships<div id='" + toolsControlID + "'></div></header>";
        html += "<div style='margin-left: 5px' id='" + gridControlID + "'></div>";

        parent.html(html);

        toolsControlID = '#' + toolsControlID;
        gridControlID = '#' + gridControlID;

        if (permissions.HasPermission("Relationship", "Update")) {
            TileTools(toolsControlID, [
                { icon: 'level-up', uri: '/relations/ImpactAnalysisOverlay?type=' + type + '&id=' + id, context: contextList.Intersect, title: 'See Impact' },
                { icon: 'pencil', uri: '/relations/RelationOverlay?type=' + type + '&id=' + id, context: contextList.Intersect, title: 'Manage Relationships' }
            ]);
        }

        var clickRelationshipKpiTitle = function () {
            var kpi = $(this);
            var critical = kpi.data("critical");
            var clickBaseUri = '/Relations/AggregateRelationOverlay?criticalOnly=' + (critical ? 'true' : 'false') + '&';
            var url = clickBaseUri + 'type=' + type + '&id=' + id + '&targetType=' + kpi.data("t") + '&targetID=' + kpi.data("i") + '&intersectTypeID=' + kpi.data("intersecttypeid");
            openTileOverlay(url);
        }

        $.ajax({
            url: '/tiles/RelationshipAggregates',
            method: 'GET',
            data: {
                type: type,
                id: id
            },
            dataType: 'json'
        }).fail(function (xhr, status, error) {

        }).done(function (data, status, xhr) {

            var groups = [];
            // Load unique group names
            $.each(data, function () {
                var groupName = this.Group;
                if ($.inArray(groupName, groups) == -1) {
                    groups.push(groupName);
                }
            });
            var collectionHtml = '';
            $.each(groups, function () {
                var selectedGroupName = this;
                var nodes = [];
                var colors = [];
                $.each(data, function () {
                    if (this.Group == selectedGroupName) {
                        nodes.push(this);
                        colors.push(this.IconBackColor);
                    }
                });
                var gridHtml = '<h5>' + nodes[0].GroupName + '</h5><div class="kpi-grid">';
                $.each(nodes, function () {
                    gridHtml += '<div class="kpi-grid-item" style="background-color: ' + this.IconBackColor + '; color: ' + this.IconForeColor + '" data-critical="' + (this.Group == "2" && this.Type == "ArtifactType") + '" data-t="' + this.Type + '" data-i="' + this.TypeID + '"data-intersecttypeid="' + this.IntersectTypeID + '">' +
                        '<div class="icon">' + this.IconText + '</div>' +
                        '<div class="value">' + this.Count + '</div>' +
                        '<div class="title">' + this.TypeName + '</div>' +
                        '</div>';
                });
                gridHtml += '</div>';
                collectionHtml += gridHtml;
            });
            $(gridControlID).html(collectionHtml);
            $('.kpi-grid').isotope({
                // options
                itemSelector: '.kpi-grid-item',
                layoutMode: 'fitRows',
                fitRows: {
                    gutter: 10
                }
            });
            $('.kpi-grid-item').on('click', clickRelationshipKpiTitle);
        });

    } catch (e) {
        console.log(e);
    }
}