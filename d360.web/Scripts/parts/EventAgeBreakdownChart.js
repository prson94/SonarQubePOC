function EventAgeBreakdownChart(controlID, contextList, type, id, timescale) {

    var chartControlID = controlID + "_chart";

    controlID = '#' + controlID;
    $(controlID).html('<div id="' + chartControlID + '" style="width: 100%; height: 225px;"></div>')
    chartControlID = '#' + chartControlID;

    var sUrl = '/queries/' + type + '/' + id + '/EventAgeBreakdown';

    var src = {
        datatype: 'json',
        type: 'get',
        url: '/queries/' + type + '/' + id + '/EventAgeBreakdown' + ((timescale !== '' && timescale) ? "?maxHistoryDays=" + timescale : ""),
        datafields:
        [
            { name: 'Date', type: 'date' },//{ name: 'Status', type: 'string' },
            { name: 'Count', type: 'number' }
        ]
    };

    var adapter = new $.jqx.dataAdapter(src);

    $(chartControlID).jqxChart({
        title: "By Age",
        description: "",
        enableAnimations: true,
        showLegend: true,
        showBorderLine: false,
        //padding: { left: 0, top: 25, right: 75, bottom: 0 },
        //titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
        //legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
        source: adapter,
        colorScheme: chartDefaultTheme,
        xAxis: {
            dataField: 'Date',
            type: 'date',
            baseUnit: 'day',
            visible: false,
            valuesOnTicks: false,
            tickMarks: {
                visible: false,
                interval: 1,
                color: '#BCBCBC'
            },
            unitInterval: 1,
            gridLines: {
                visible: false,
                interval: 3,
                color: '#BCBCBC'
            },
            labels: {
                angle: -45,
                rotationPoint: 'topright',
                offset: { x: 0, y: -25 }
            }
        },
        valueAxis:
        {
            visible: true,
            minValue: 0,
            //unitInterval: 1,
            title: { text: 'Total Events By Day<br>' },
            tickMarks: { color: '#BCBCBC' }
        },
        colorScheme: 'scheme04',
        seriesGroups:
            [
                {
                    type: 'line',
                    series: [
                            { dataField: 'Count', displayText: '# Events' }
                    ]
                }
            ]

        //xAxis: {
        //    dataField: 'Status',
        //    showGridLines: true
        //},
        //seriesGroups: [{
        //    useGradientColors: false,
        //    type: 'column',
        //    columnsGapPercent: 50,
        //    valueAxis:
        //    {
        //        unitInterval: 10,
        //        displayValueAxis: true,
        //        description: '# Events'
        //    },
        //    series: [
        //            { dataField: 'Count', displayText: 'Age In Days'}
        //    ]
        //}]
    });
}