function home(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/', function (context) {
        context.app.swap('');

        var type = "Resource";
        var id = currentResourceID;

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var homeSocialTile;
        var HomeSocial;
        var ResponsibilityAdapter;
        var ResponsibilitySource;

        //#region Event Handlers

        function unsubscribe(data) {
            HomeSocial = null;
            homeSocialTile = null;
            ResponsibilityAdapter = null;
            ResponsibilitySource = null;

            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context.title(pageViewModel.Title);
        context
            .render(templatePath + 'home.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'Resource', id: currentResourceID });
                $('#SideIcons').PageTools("clear");

                //#region Red Flag Summary Chart

                var rf = $("#RedFlagChartTile");

                ResponsibilitySource = {
                    datatype: 'json',
                    type: 'get',
                    url: '/Queries/CriticalNonCriticalRedFlagAggregates',
                    datafields:
                    [
                        { name: 'Type', type: 'string' },
                        { name: 'TypeName', type: 'string' },
                        { name: 'TypeID', type: 'number' },
                        { name: 'Count', type: 'number' },
                        { name: 'CriticalCount', type: 'number' }
                    ]
                };

                ResponsibilityAdapter = new $.jqx.dataAdapter(ResponsibilitySource);

                rf.jqxChart({
                    title: "",
                    description: "Total vs. critical red-flagged items",
                    enableAnimations: true,
                    showLegend: true,
                    borderLineWidth: 0,
                    showBorderLine: false,
                    colorScheme: chartDefaultTheme,
                    padding: { left: 5, top: 5, right: 5, bottom: 5 },
                    titlePadding: { left: 90, top: 0, right: 0, bottom: 10 },
                    xAxis: {
                        dataField: 'TypeName',
                        showTickMarks: true,
                        tickMarksInterval: 1,
                        tickMarksColor: '#bababa',
                        unitInterval: 1,
                        showGridLines: false,
                        gridLinesInterval: 1,
                        gridLinesColor: '#bababa',
                        textRotationAngle: 15,
                        textRotationPoint: 'center',
                        axisSize: 'auto'
                    },
                    source: ResponsibilityAdapter,
                    seriesGroups:
                    [
                        {
                            useGradientColors: false,
                            type: 'stackedcolumn',
                            columnsGapPercent: 100,
                            seriesGapPercent: 5,
                            valueAxis:
                            {
                                displayValueAxis: true,
                                description: '# Items',
                                tickMarksColor: '#bababa'
                            },
                            series: [
                                    { dataField: 'Count', displayText: 'Total' },
                                    { dataField: 'CriticalCount', displayText: 'Critical' }
                            ],
                            click: function (e) {
                                var data = ResponsibilityAdapter.records[e.elementIndex];
                                var uri = '/overlays/' + data.Type + '/' + data.TypeID + '/RedFlags';
                                openTileOverlay(uri);
                                //$('#PersonGridTitle').text('People Assigned as ' + data.ResponsibilityType);
                                //$('#OwnedItemGridTitle').text('');
                                //pgSrc.url = '/queries/' + data.ResponsibilityTypeID + '/ResourcesByResponsibilityType';
                                //pg.jqxGrid('updatebounddata');
                                //oigSrc.url = null;
                                //oig.jqxGrid('updatebounddata');
                            }
                        }
                    ]
                });
                rf.jqxChart('addColorScheme', 'myScheme', chartYesNoColorScheme);
                rf.jqxChart('colorScheme', 'myScheme');
                rf.jqxChart('refresh');

                //#endregion

                //#region Tiles

                HomeSocial = new BoardViewModel();
                ko.applyBindings(HomeSocial, document.getElementById('HomeBoard'));
                HomeSocial.getMoreComments();

                YourFollowedItemsTile('#FollowingTile', id, 'Items You Follow');
                YourOwnedItemsTile('#OwnedTile', id, 'Items You Own');

                //homeSocialTile = new HomeSocialMicroTileModel(id);
                //ko.applyBindings(homeSocialTile, document.getElementById('HomeSocialTile'));
                //homeSocialTile.GetStatistics();

                YourWorkflowTasks('WorkflowTasksTile', 'Your Assigned Tasks', true);

                //#endregion

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}