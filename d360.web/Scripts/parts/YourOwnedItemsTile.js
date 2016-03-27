function YourOwnedItemsTile(controlID, resourceID, title) {
    try {

        var chart = $(controlID);
        var parentWidth = chart.parent().innerWidth();

        chart.css('width', '100%');
        chart.css('height', '400px');

        try {
            chart.jqxChart('destroy');
        } catch (e) { }

        try {
            var source = {
                datatype: 'json',
                url: '/queries/ResponsibilityBreakdownByResource?id=' + resourceID,
                datafields:
                [
                    { name: 'ObjectType' },
                    { name: 'ObjectTypeID' },
                    { name: 'ObjectTypeName' },
                    { name: 'Count' }
                ]
            };

            var adapter = new $.jqx.dataAdapter(source);

            chart.jqxChart({
                title: title,
                description: "",
                enableAnimations: true,
                showLegend: true,
                showBorderLine: false,
                legendLayout: { left: 0, top: 250, width: parentWidth - 25, height: 150, flow: 'vertical' },
                padding: { left: 0, right: 0, top: 0, bottom: 150 },
                source: adapter,
                colorScheme: chartDefaultTheme,
                seriesGroups: [{
                    useGradientColors: false,
                    type: 'pie',
                    series: [
                        {
                            showLabels: true,
                            useGradient: false,
                            dataField: 'Count',
                            displayText: 'ObjectTypeName',
                            labelRadius: 50,
                            initialAngle: 15,
                            radius: 100,
                            centerOffset: 0
                        }
                    ],
                    click: function (e) {
                        var data = adapter.records[e.elementIndex];
                        var url = '/parts/resources/' + resourceID + '/ownership/' + data.ObjectType + '/' + data.ObjectTypeID;
                        openTileOverlay(url);
                    }
                }]
            });
            //chart.jqxChart('addColorScheme', 'myScheme', colorScheme);
            //chart.jqxChart('colorScheme', 'myScheme');
            //chart.jqxChart('refresh');
        } catch (e) { }

        function pageResized() {
            chart.jqxChart('refresh');
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;

            amplify.unsubscribe("PageResized", pageResized);
            amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        amplify.subscribe("PageResized", pageResized);
        amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }
    catch (e) {
    }
}