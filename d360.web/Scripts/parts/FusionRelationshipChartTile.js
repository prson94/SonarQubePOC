function FusionRelationshipChartTile(controlID, type, id, parentAttributeID) {
    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<header>Relationships</header><table style="width: 100%"><tr><td><div id="' + chartControlID + '" style="margin: auto; width: 95%; height: 300px"></div></td></tr></table>')
    chartControlID = '#' + chartControlID;

    $.ajax({
        url: '/internal/fusion/RelationshipAggregates?type=' + type + '&id=' + id + '&parentAttributeID=' + (parentAttributeID ? parentAttributeID : 0),
        method: 'GET'
    })
    .done(function (data, status, xhr) {
        if (data.length > 0) {
            var source = {
                datatype: 'json',
                localdata: data,//url: '/fusion/RelationshipAggregates?id=' + id,
                datafields:
                [
                    { name: 'ObjectType' },
                    { name: 'ObjectID' },
                    { name: 'TypeID' },
                    { name: 'Type' },
                    { name: 'TypeName' },
                    { name: 'IconBackColor' },
                    { name: 'IconForeColor' },
                    { name: 'IconText' },
                    { name: 'Count' }
                ]
            };

            var adapter = new $.jqx.dataAdapter(source);

            $(chartControlID).jqxChart({
                title: "",
                description: "",
                enableAnimations: true,
                showLegend: true,
                showBorderLine: false,
                //legendLayout: { left: (tileWidth / 2) + 10, top: 50, width: 175, height: 200, flow: 'vertical' }, //legendLayout: { left: 250, top: 75, width: 175, height: 200, flow: 'vertical' },
                //padding: { left: 10, right: (tileWidth / 2) - 10, top: 0, bottom: 10 },//padding: { left: 10, right: 150, top: 10, bottom: 10 },
                source: adapter,
                colorScheme: chartDefaultTheme,
                seriesGroups: [{
                    type: 'donut',
                    useGradientColors: false,
                    series: [
                        {
                            showLabels: true,
                            useGradient: false,
                            dataField: 'Count',
                            displayText: 'TypeName',
                            labelRadius: 80,
                            initialAngle: 15,
                            radius: 100,
                            innerRadius: 50,
                            centerOffset: 0
                        }
                    ],
                    click: function (e) {
                        var data = adapter.records[e.elementIndex];
                        var url = '/internal/fusion/RelationshipAggregatesOverlay?type=' + type + '&id=' + id + '&targetType=' + data.Type + '&targetID=' + data.TypeID + '&parentAttributeID=' + (parentAttributeID ? parentAttributeID : 0);
                        openTileOverlay(url);
                    }
                }]
            });

            $(document).on('resize', function () {
                $(chartControlID).jqxChart('refresh');
            });
        }
        else {
            $(chartControlID).text('No information available.');
        }
    })
    .fail(function (xhr, status, error) {
        $(chartControlID).text(error);
    });
}