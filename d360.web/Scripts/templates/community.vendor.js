function community_vendor(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/community/vendor', function (context) {
        context.app.swap('');

        var type = "Resource";
        var id = currentResourceID;

        pageViewModel.Title += ': Vendors : Bloomberg'

        context.title(pageViewModel.Title);

        //#region Event Handlers

        function unsubscribe(data) {
            //AuditGridAdapter = null;
            //AuditGridSource = null;

            //amplify.unsubscribe('CancelAction', cancelAction);
            //amplify.unsubscribe('SaveAction', saveAction);

            //$("#RelationshipContextsGrid").off("bindingcomplete", relationshipContextsGridBindingComplete);
            //$("#TechnicalRelationshipsGrid").off("bindingcomplete", technicalRelationshipsGridBindingComplete);
            //$('#TreeGrid').off('rowselect', treeGridRowSelect);
            //$('#TreeGrid').off('rowdoubleclick', treeGridRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'community.vendor.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                //#region Alerts Tile

                var AlertTileData = [
                    { Issue: 'Issue With Bloomberg Terminal', ConfirmCount: 25, VendorAware: true, Impact: 'Critical' },
                    { Issue: 'MSFT end of day price invalid for 9/16/2014', ConfirmCount: 14, VendorAware: false, Impact: 'Medium' }
                ];

                var AlertTileSource = {
                    datatype: "json",
                    datafields: [
                        { name: 'Impact', type: 'string' },
                        { name: 'Issue', type: 'string' },
                        { name: 'VendorAware', type: 'bool' },
                        { name: 'ConfirmCount', type: 'int' }
                    ],
                    localdata: AlertTileData
                };

                var AlertTileAdapter = new $.jqx.dataAdapter(AlertTileSource);

                $("#AlertsTile").jqxGrid({
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    altrows: true,
                    filterable: true,
                    showfilterrow: true,
                    virtualmode: false,
                    pageable: true,
                    source: AlertTileAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Issue", text: "Issue" },
                        { datafield: "ConfirmCount", filtertype: 'number', text: "# Confirmed", width: 100 },
                        {
                            datafield: "Impact", text: "Impact", width: 75, filtertype: 'checkedlist',
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                return "<i class='Impact Impact-" + value + "' title='" + value + "'></i>";
                            }
                        },
                        { datafield: "VendorAware", text: "Vendor Knows?", width: 110, columntype: 'checkbox', filtertype: 'bool'  }
                    ]
                });

                //#endregion

                //#region Products Tile

                var ProductsTileData = [
                    { Name: 'Back Office Data License', NumberOfClients: 8326 },
                    { Name: 'Extended Back Office', NumberOfClients: 7543 },
                    { Name: 'Per Security Request', NumberOfClients: 6534 },
                    { Name: 'Back Office Data', NumberOfClients: 8521 },
                    { Name: 'Bloomberg Data License', NumberOfClients: 5221 },
                    { Name: 'Front Office', NumberOfClients: 5210 }
                ];

                var ProductsTileSource = {
                    datatype: "json",
                    datafields: [
                        { name: 'Name', type: 'string' },
                        { name: 'NumberOfClients', type: 'int' }
                    ],
                    localdata: ProductsTileData
                };

                var ProductsTileAdapter = new $.jqx.dataAdapter(ProductsTileSource);

                $("#ProductsTile").jqxGrid({
                    width: grid_width,
                    pagermode: 'simple',
                    autoheight: true,
                    rowsheight: 50,
                    sortable: true,
                    altrows: true,
                    filterable: false,
                    //showfilterrow: true,
                    virtualmode: false,
                    pageable: false,
                    source: ProductsTileAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        { datafield: "NumberOfClients", filtertype: 'number', text: "# Clients", width: 75 }
                    ]
                });

                //#endregion

                //#region AlertHistory Tile

                $("#AlertHistoryChart").kendoSparkline({
                    type: "column",
                    data: [7, 3, 4, 10, 5, 8, 2],
                    tooltip: {
                        format: "{0} alerts"
                    },
                    chartArea: {
                        height: 75,
                        width: '98%'
                    }
                });

                $("#AlertHistoryChart").kendoChart({
                    legend: {
                        position: "top"
                    },
                    seriesDefaults: {
                        type: "column"
                    },
                    series: [{
                        name: "Critical",
                        data: [4, 2, 15, 10, 4, 2, 3, 1]
                    }, {
                        name: "High",
                        data: [1, 2, 3, 1, 5, 0, 1, 0]
                    }, {
                        name: "Medium",
                        data: [1, 2, 3, 1, 5, 0, 2, 1]
                    }, {
                        name: "Low",
                        data: [10, 12, 13, 6, 4, 8, 3, 0]
                    }],
                    valueAxis: {
                        labels: {
                            format: "{0} alerts"
                        },
                        line: {
                            visible: false
                        },
                        axisCrossingValue: 0
                    },
                    categoryAxis: {
                        categories: ['T-7', 'T-6', 'T-5', 'T-4', 'T-3', 'T-2', 'T-1', 'T'],
                        line: {
                            visible: false
                        },
                        labels: {
                            padding: { top: 15 }
                        }
                    },
                    tooltip: {
                        visible: true,
                        format: "{0} alerts",
                        template: "#= series.name #: #= value #"
                    }
                });

                //#endregion

                //#region Sentiment Tile

                $("#SentimentChart").kendoSparkline({
                    type: "column",
                    data: [80, 71, 65, 73, 84, 88, 92],
                    tooltip: {
                        format: "{0}%"
                    },
                    chartArea: {
                        height: 75
                    }
                });

                //#endregion

                //#region Quality Tile

                $("#QualityChart").kendoSparkline({
                    type: "column",
                    data: [65, 67, 70, 73, 75, 79, 78],
                    tooltip: {
                        format: "{0}%"
                    },
                    chartArea: {
                        height: 75
                    }
                });

                //#endregion

                //#region Timeliness Tile

                $("#TimelinessChart").kendoSparkline({
                    type: "column",
                    data: [63, 71, 72, 59, 68, 63, 64],
                    tooltip: {
                        format: "{0}%"
                    },
                    chartArea: {
                        height: 75
                    }
                });

                //#endregion

                //#region Completeness Tile

                $("#CompletenessChart").kendoSparkline({
                    type: "column",
                    data: [91, 90, 97, 94, 92, 95, 96],
                    tooltip: {
                        format: "{0}%"
                    },
                    chartArea: {
                        height: 75
                    }
                });

                //#endregion

                //#region Threads Tile

                var ThreadsTileData = [
                    { Title: 'Who having issues with Bloomberg Terminal?', Group: 'Chief Data Officers', ReplyCount: 65 }
                ];

                var ThreadsTileSource = {
                    datatype: "json",
                    datafields: [
                        { name: 'Title', type: 'string' },
                        { name: 'Group', type: 'string' },
                        { name: 'ReplyCount', type: 'int' }
                    ],
                    localdata: ThreadsTileData
                };

                var ThreadsTileAdapter = new $.jqx.dataAdapter(ThreadsTileSource);

                $("#ThreadsTile").jqxGrid({
                    width: grid_width,
                    pagermode: 'simple',
                    autoheight: true,
                    sortable: true,
                    autorowheight: true,
                    rowsheight: 50,
                    altrows: true,
                    filterable: false,
                    //showfilterrow: true,
                    virtualmode: false,
                    pageable: false,
                    source: ThreadsTileAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Title", text: "Title" },
                        { datafield: "Group", text: "Group", width: 125 },
                        { datafield: "ReplyCount", filtertype: 'number', text: "Replies", width: 50 }
                    ]
                });

                //#endregion

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}