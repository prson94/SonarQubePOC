function community_home(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/community', function (context) {
        context.app.swap('');

        var type = "Resource";
        var id = currentResourceID;

        context.title(pageViewModel.Title);

        var AlertTileData;
        var AlertTileSource;
        var AlertTileAdapter;
        var GroupsTileData;
        var GroupsTileSource;
        var GroupsTileAdapter;
        var ProductsTileData;
        var ProductsTileSource;
        var ProductsTileAdapter;
        var ThreadsTileData;
        var ThreadsTileSource;
        var ThreadsTileAdapter;
        var VendorsTileData;
        var VendorsTileSource;
        var VendorsTileAdapter;

        //#region Event Handlers

        function activeGroupsTileRowDoubleClick(event) {
            var args = event.args;
            var boundIndex = args.rowindex;

            location.assign('#/community/group')
        }

        function activeThreadsTileRowDoubleClick(event) {
            var args = event.args;
            var boundIndex = args.rowindex;

            location.assign('#/community/discussion')
        }

        function alertsTileRowDoubleClick(event) {
            var args = event.args;

            // row's bound index.
            var boundIndex = args.rowindex;
            // row's visible index.     var visibleIndex = args.visibleindex;
            // right click.             var rightclick = args.rightclick;
            // original event.          var ev = args.originalEvent;

            location.assign('#/community/alert')
        }

        function newProductsTileRowDoubleClick(event) {
            var args = event.args;
            var boundIndex = args.rowindex;

            location.assign('#/community/product')
        }

        function newVendorsTileRowDoubleClick(event) {
            var args = event.args;
            var boundIndex = args.rowindex;

            location.assign('#/community/vendor')
        }

        function unsubscribe(data) {
            AlertTileData = null;
            AlertTileSource = null;
            AlertTileAdapter = null;
            GroupsTileData = null;
            GroupsTileSource = null;
            GroupsTileAdapter = null;
            ProductsTileData = null;
            ProductsTileSource = null;
            ProductsTileAdapter = null;
            ThreadsTileData = null;
            ThreadsTileSource = null;
            ThreadsTileAdapter = null;
            VendorsTileData = null;
            VendorsTileSource = null;
            VendorsTileAdapter = null;

            $('#ActiveGroupsTile').off('rowdoubleclick', activeGroupsTileRowDoubleClick);
            $('#ActiveThreadsTile').off('rowdoubleclick', activeThreadsTileRowDoubleClick);
            $('#AlertsTile').off('rowdoubleclick', alertsTileRowDoubleClick);
            $('#NewVendorsTile').off('rowdoubleclick', newVendorsTileRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'community.home.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                //#region Alerts Tile

                //AlertTileData = [
                //    { Issue: 'Issue With Bloomberg Terminal', ConfirmCount: 25, Vendor: 'Bloomberg', VendorAware: true, Impact: 'Critical' },
                //    { Issue: 'API failure when submitting events with no fields', ConfirmCount: 2, Vendor: 'Data3Sixty', VendorAware: true, Impact: 'Low' },
                //    { Issue: 'MSFT end of day price invalid for 9/16/2014', ConfirmCount: 14, Vendor: 'Bloomberg', VendorAware: false, Impact: 'Medium' }
                //];

                //AlertTileSource = {
                //    datatype: "json",
                //    datafields: [
                //        { name: 'Vendor', type: 'string' },
                //        { name: 'Impact', type: 'string' },
                //        { name: 'Issue', type: 'string' },
                //        { name: 'VendorAware', type: 'bool' },
                //        { name: 'ConfirmCount', type: 'int' }
                //    ],
                //    localdata: AlertTileData
                //};

                //AlertTileAdapter = new $.jqx.dataAdapter(AlertTileSource);

                //$("#AlertsTile").jqxGrid({
                //    width: grid_width,
                //    pagesizeoptions: ['10', '20', '50'],
                //    pagesize: 20,
                //    autoheight: true,
                //    sortable: true,
                //    altrows: true,
                //    filterable: true,
                //    showfilterrow: true,
                //    virtualmode: false,
                //    pageable: true,
                //    source: AlertTileAdapter,
                //    theme: list_theme,
                //    columns: [
                //        { datafield: "Vendor", text: "Vendor", filtertype: 'checkedlist', width: 100 },
                //        { datafield: "Issue", text: "Issue" },
                //        { datafield: "ConfirmCount", filtertype: 'number', text: "# Confirmed", width: 100 },
                //        {
                //            datafield: "Impact", text: "Impact", width: 75, filtertype: 'checkedlist',
                //            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                //                return "<i class='Impact Impact-" + value + "' title='" + value + "'></i>";
                //            }
                //        },
                //        { datafield: "VendorAware", text: "Vendor Knows?", width: 110, columntype: 'checkbox', filtertype: 'bool'  }
                //    ]
                //});

                //#endregion

                //#region Vendors Tile

                //VendorsTileData = [
                //    { Name: 'Markit', Rating: 9 },
                //    { Name: 'Exchange Data International', Rating: 7 }
                //];

                //VendorsTileSource = {
                //    datatype: "json",
                //    datafields: [
                //        { name: 'Name', type: 'string' },
                //        { name: 'Rating', type: 'int' }
                //    ],
                //    localdata: VendorsTileData
                //};

                //VendorsTileAdapter = new $.jqx.dataAdapter(VendorsTileSource);

                //$("#NewVendorsTile").jqxGrid({
                //    width: grid_width,
                //    pagermode: 'simple',
                //    autoheight: true,
                //    rowsheight: 50,
                //    sortable: true,
                //    altrows: true,
                //    filterable: false,
                //    //showfilterrow: true,
                //    virtualmode: false,
                //    pageable: false,
                //    source: VendorsTileAdapter,
                //    theme: list_theme,
                //    columns: [
                //        { datafield: "Name", text: "Name" },
                //        {
                //            datafield: "Rating", filtertype: 'number', text: "Rating", width: 50,
                //            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                //                var style = "";

                //                if (value < 5) {
                //                    style = "LowRating";
                //                }
                //                else if (value > 7) {
                //                    style = "HighRating";
                //                }
                //                else {
                //                    style = "MediumRating";
                //                }

                //                return "<span class='Rating " + style + "'>" + value + "</span>";
                //            }
                //        }
                //    ]
                //});

                //#endregion

                //#region Products Tile

                //ProductsTileData = [
                //    { Name: 'Markit EDM', Vendor: 'Markit', NumberOfClients: 326 },
                //    { Name: 'Product Master', Vendor: 'Data3Sixty', NumberOfClients: 521 }
                //];

                //ProductsTileSource = {
                //    datatype: "json",
                //    datafields: [
                //        { name: 'Name', type: 'string' },
                //        { name: 'Vendor', type: 'string' },
                //        { name: 'NumberOfClients', type: 'int' }
                //    ],
                //    localdata: ProductsTileData
                //};

                //ProductsTileAdapter = new $.jqx.dataAdapter(ProductsTileSource);

                //$("#NewProductsTile").jqxGrid({
                //    width: grid_width,
                //    pagermode: 'simple',
                //    autoheight: true,
                //    rowsheight: 50,
                //    sortable: true,
                //    altrows: true,
                //    filterable: false,
                //    //showfilterrow: true,
                //    virtualmode: false,
                //    pageable: false,
                //    source: ProductsTileAdapter,
                //    theme: list_theme,
                //    columns: [
                //        { datafield: "Name", text: "Name" },
                //        { datafield: "Vendor", text: "Vendor", filtertype: 'checkedlist', width: 100 },
                //        { datafield: "NumberOfClients", filtertype: 'number', text: "# Clients", width: 75 }
                //    ]
                //});

                //$('#NewProductsTile').on('rowdoubleclick', newProductsTileRowDoubleClick);

                //#endregion

                //#region Groups Tile

                //GroupsTileData = [
                //    { Name: 'Markit EDM Stewards', MemberCount: 863, RecentDiscussionCount: 342, RecentActivity: [15, 15, 16, 19, 20, 20, 21] },
                //    { Name: 'Chief Data Officers', MemberCount: 521, RecentDiscussionCount: 229, RecentActivity: [15, 15, 16, 19, 20, 20, 21] }
                //];

                //GroupsTileSource = {
                //    datatype: "json",
                //    datafields: [
                //        { name: 'Name', type: 'string' },
                //        { name: 'MemberCount', type: 'int' },
                //        { name: 'RecentDiscussionCount', type: 'int' },
                //        { name: 'RecentActivity', type: 'array' }
                //    ],
                //    localdata: GroupsTileData
                //};

                //GroupsTileAdapter = new $.jqx.dataAdapter(GroupsTileSource);

                //$("#ActiveGroupsTile").jqxGrid({
                //    width: grid_width,
                //    pagermode: 'simple',
                //    autoheight: true,
                //    sortable: true,
                //    rowsheight: 50,
                //    altrows: true,
                //    filterable: false,
                //    //showfilterrow: true,
                //    virtualmode: false,
                //    pageable: false,
                //    source: GroupsTileAdapter,
                //    theme: list_theme,
                //    columns: [
                //        { datafield: "Name", text: "Name" },
                //        { datafield: "MemberCount", filtertype: 'number', text: "Members", width: 75 },
                //        //{ datafield: "RecentDiscussionCount", filtertype: 'number', text: "Recent Threads", width: 75 },
                //        {
                //            datafield: "RecentDiscussionCount", filterable: false, text: "Activity", width: 75,
                //            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                //                return "<div style='vertical-align: middle'><div class='spark spark-line' style='height: 50px' data-values='" + value + "'></div></div>";
                //            }
                //        }
                //    ],
                //    ready: function () {
                //        $(".spark-line").kendoSparkline({
                //            type: "area",
                //            data: [25, 23, 28, 15, 24, 35, 54],//$(this).data('values'),
                //            tooltip: {
                //                format: "{0} threads"
                //            },
                //            chartArea: {
                //                height: 50
                //            }
                //        });
                //    }
                //});

                //#endregion

                //#region Threads Tile

                //ThreadsTileData = [
                //    { Title: 'Any thoughts on best uses for D3S Domains?', Group: 'MDM Today', ReplyCount: 67 },
                //    { Title: 'Who has used Markit\'s new data product line?', Group: 'Chief Data Officers', ReplyCount: 65 }
                //];

                //ThreadsTileSource = {
                //    datatype: "json",
                //    datafields: [
                //        { name: 'Title', type: 'string' },
                //        { name: 'Group', type: 'string' },
                //        { name: 'ReplyCount', type: 'int' }
                //    ],
                //    localdata: ThreadsTileData
                //};

                //ThreadsTileAdapter = new $.jqx.dataAdapter(ThreadsTileSource);

                //$("#ActiveThreadsTile").jqxGrid({
                //    width: grid_width,
                //    pagermode: 'simple',
                //    autoheight: true,
                //    sortable: true,
                //    autorowheight: true,
                //    rowsheight: 50,
                //    altrows: true,
                //    filterable: false,
                //    //showfilterrow: true,
                //    virtualmode: false,
                //    pageable: false,
                //    source: ThreadsTileAdapter,
                //    theme: list_theme,
                //    columns: [
                //        { datafield: "Title", text: "Title" },
                //        { datafield: "Group", text: "Group", width: 100 },
                //        { datafield: "ReplyCount", filtertype: 'number', text: "Replies", width: 50 }
                //    ]
                //});

                //#endregion

                //$('#ActiveGroupsTile').on('rowdoubleclick', activeGroupsTileRowDoubleClick);
                //$('#ActiveThreadsTile').on('rowdoubleclick', activeThreadsTileRowDoubleClick);
                //$('#AlertsTile').on('rowdoubleclick', alertsTileRowDoubleClick);
                //$('#NewVendorsTile').on('rowdoubleclick', newVendorsTileRowDoubleClick);
                //amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    });
}