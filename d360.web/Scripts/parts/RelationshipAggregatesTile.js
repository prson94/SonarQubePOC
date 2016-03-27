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
        //html += "<table style='width: 100%'>";//"<div class='row'>";
        //html += "<tr>";
        //html += "<td style='width: 50%'><div id='AggregateTileChart1' class='col s6' style='margin: auto; width: 100%'></div></td>";
        //html += "<td style='width: 50%'><div id='AggregateTileChart2' class='col s6' style='margin: auto; width: 100%'></div></td>";
        //html += "</tr>";
        //html += "<tr>";
        //html += "<td colspan='2' style='margin: auto; width: 100%'><div id='AggregateTileChart3' class='col s12' style='width: 60%'></div></td>";
        //html += "</tr>";
        //html += "</table>";//"</div>";

        parent.html(html);

        toolsControlID = '#' + toolsControlID;
        gridControlID = '#' + gridControlID;

        if (permissions.HasPermission("Relationship", "Update")) {
            TileTools(toolsControlID, [
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



                //    var cht = $('#AggregateTileChart' + this);
                //    if (nodes.length <= 0) {
                //        cht.css('height', '40px');
                //        cht.html('No data to display');
                //    }
                //    else {
                //        var groupName = nodes[0].GroupName;
                //        var critical = (nodes[0].Group == "2" && nodes[0].Type == "ArtifactType");
                //        cht.css('height', '300px');
                //        cht.jqxChart({
                //            source: nodes,
                //            title: groupName,
                //            description: '',
                //            enableAnimations: false,
                //            showLegend: true,
                //            showBorderLine: false,
                //            legendLayout : { flow: 'horizontal' },
                //            //padding: { left: 5, top: 5, right: 5, bottom: 5 },
                //            //titlePadding: { left: 0, top: 0, right: 0, bottom: 10 },
                //            seriesGroups: [
                //                {
                //                    useGradientColors: false,
                //                    type: 'pie',
                //                    showLegend: true,
                //                    enableSeriesToggle: true,
                //                    series: [
                //                        {
                //                            dataField: 'Count',
                //                            displayText: 'TypeName',
                //                            showLabels: true,
                //                            //labelRadius: 125,
                //                            labelLinesEnabled: true,
                //                            labelLinesAngles: true,
                //                            labelsAutoRotate: false
                //                            //initialAngle: 0,
                //                            //radius: 100,
                //                            //minAngle: 0,
                //                            //maxAngle: 180,
                //                            //centerOffset: 0,
                //                            //offsetY: 180,
                //                            //formatFunction: function (value, itemIndex, serie, group) {
                //                            //    return value;
                //                            //}
                //                        }
                //                    ],
                //                    click: function (e) {
                //                        var clickBaseUri = '/Relations/AggregateRelationOverlay?criticalOnly=' + (critical ? 'true' : 'false') + '&';
                //                        var data = nodes[e.elementIndex];                                    
                //                        var url = clickBaseUri + 'type=' + type + '&id=' + id + '&targetType=' + data.Type + '&targetID=' + data.TypeID + '&intersectTypeID=' + data.IntersectTypeID;
                //                        openTileOverlay(url);
                //                    }
                //                }
                //            ]
                //        });
                //        cht.jqxChart('addColorScheme', 'myScheme', colors);
                //        cht.jqxChart('colorScheme', 'myScheme');
                //        cht.jqxChart('refresh');

                //        $(document).on('resize', function () {
                //            cht.jqxChart('refresh');
                //        });
                //    }
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