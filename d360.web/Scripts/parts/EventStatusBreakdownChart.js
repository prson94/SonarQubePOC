function EventStatusBreakdownChart(controlID, contextList, type, id, timescale) {

    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + chartControlID + '" style="width: 100%; height: 225px;"></div>')
    chartControlID = '#' + chartControlID;

    var src = {
        datatype: 'json',
        type: 'get',
        url: '/queries/' + type + '/' + id + '/EventStatusBreakdown' + ((timescale != '' && timescale) ? "?maxHistoryDays=" + timescale : ""),
        datafields:
        [
            { name: 'Status', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(src);

    $(chartControlID).jqxChart({
        title: "By Status",
        description: "",
        enableAnimations: true,
        showLegend: true,
        showBorderLine: false,
        //padding: { left: 0, top: 25, right: 75, bottom: 0 },
        //titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
        //legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
        source: adapter,
        colorScheme: chartDefaultTheme,
        seriesGroups: [{
            type: 'pie',
            useGradientColors: false,
            showLabels: true,
            series: [
                {
                    useGradient: false,
                    dataField: 'Count',
                    displayText: 'Status',
                    labelRadius: 50,
                    initialAngle: 15,
                    radius: 75,
                    centerOffset: 0
                }
            ]
        }]
    });
}