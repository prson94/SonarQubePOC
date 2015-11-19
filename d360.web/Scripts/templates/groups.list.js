function groups_list(app, pageViewModel, templatePath, contextList) {
    app.get('#/groups', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var GroupsSource;
        var GroupsAdapter;
        var PeopleSource;
        var PeopleAdapter;
        var OwnedItemSource;
        var OwnedItemAdapter;
        var PersonSource;
        var PersonAdapter;
        var ResponsibilitySource;
        var ResponsibilityAdapter;

        //#region Event Handlers

        function groupSearchResultsRowDoubleClick(event) {
            //var args = event.args;
            var row = $('#GroupSearchResults').jqxGrid('getrowdata', event.args.rowindex);
            location.assign('#/groups/' + row.ID);
        }

        function groupListPageResize() {
            $("#ResponsibilityGrid").jqxChart('refresh');
        }

        function personGridRowClick(event) {
            var args = event.args;
            var row = args.rowindex;
            var data = $("#PersonGrid").jqxGrid('getrowdata', row);
            $('#OwnedItemGridTitle').text('Items Owned by ' + data.FirstName + ' ' + data.LastName);
            OwnedItemSource.url = '/queries/' + data.ResourceID + '/' + data.ResponsibilityTypeID + '/ResponsibilitiesByResource';
            $("#OwnedItemGrid").jqxGrid('clearselection');
            $("#OwnedItemGrid").jqxGrid('updatebounddata');
        }

        function personSearchResultsRowDoubleClick(event) {
            //var args = event.args;
            var row = $('#PersonSearchResults').jqxGrid('getrowdata', event.args.rowindex);
            location.assign('#/resources/' + row.ID);
        }

        function refreshActionMenu(data) {
            $('#SideIcons').PageTools('refresh');
        }

        function unsubscribe(data) {
            GroupsAdapter = null;
            GroupsSource = null;
            PeopleAdapter = null;
            PeopleSource = null;
            OwnedItemAdapter = null;
            OwnedItemSource = null;
            PersonAdapter = null;
            PersonSource = null;
            ResponsibilityAdapter = null;
            ResponsibilitySource = null;

            $(document).off('resize', groupListPageResize);
            $("#PersonGrid").off('rowclick', personGridRowClick);
            $('#PersonSearchResults').off('rowdoubleclick', personSearchResultsRowDoubleClick);
            $('#GroupSearchResults').off('rowdoubleclick', groupSearchResultsRowDoubleClick);
            amplify.unsubscribe("RefreshActionMenu", refreshActionMenu);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'groups.list.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                //$('#SideIcons').PageTools({ type: 'ArtifactType', id: typeID, context: 'list' });

                $('#SideIcons').PageTools({ type: 'Group', id: 0, context: 'list' });
                //$('#SideIcons').PageTools('clear');

                //#region Group Grid

                GroupsSource = {
                    datatype: 'json',
                    type: 'get',
                    url: '/api/groups',
                    datafields:
                    [
                        { name: 'ID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'NumberOfMembers', type: 'number' },
                        { name: 'IsMember', type: 'boolean' }
                    ]
                };

                GroupsAdapter = new $.jqx.dataAdapter(GroupsSource);

                $("#GroupGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    virtualmode: false,
                    pageable: true,
                    filterable: true,
                    showfilterrow: true,
                    source: GroupsAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        { datafield: "NumberOfMembers", text: "# Members", width: 100, columntype: 'numberinput', filtertype: 'number' },
                        {
                            datafield: "ID",
                            text: "",
                            width: 40,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                var tools;
                                if (data.IsMember) {
                                    tools = [
                                        { isitemlink: true, urlprefix: '#/groups/{0}', type: 'Group', context: 'Preview' }
                                    ];
                                }
                                else {
                                    tools = [
                                        { isitemlink: true, urlprefix: '#/groups/{0}', type: 'Group', context: 'Preview' },
                                        //{ icon: 'group', context: contextList.ActionCommand, urlprefix: '/groups/{0}/join' }
                                    ];
                                }
                                var context = "";
                                return renderToolsHtml(value, tools, context, data);
                            },
                            filterable: false,
                            sortable: false
                        }
                    ]
                });

                //#endregion

                //#region Person Grid

                PeopleSource = {
                    datatype: 'json',
                    type: 'get',
                    url: '/api/resources/1',
                    datafields:
                    [
                        { name: 'ResourceID', type: 'number' },
                        { name: 'FirstName', type: 'string' },
                        { name: 'LastName', type: 'string' }
                    ]
                };

                PeopleAdapter = new $.jqx.dataAdapter(PeopleSource);

                $("#ResourceGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 10,
                    autoheight: true,
                    sortable: true,
                    virtualmode: false,
                    filterable: true,
                    showfilterrow: true,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 10,
                    pageable: true,
                    source: PeopleAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "LastName", text: "Last Name" },
                        { datafield: "FirstName", text: "First Name" },
                        {
                            datafield: "ResourceID",
                            text: "",
                            width: 40,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {

                                var tools = [
                                    { isitemlink: true, urlprefix: '#/resources/{0}', type: 'Resource', context: 'Preview' }
                                ];
                                var context = "";
                                return renderToolsHtml(value, tools, context, data);
                            },
                            filterable: false,
                            sortable: false
                        }
                    ]
                });

                //#endregion

                //#region ResponsibilityChart

                ResponsibilitySource = {
                    datatype: 'json',
                    type: 'get',
                    url: '/queries/ResponsibilityTypeBreakdown',
                    datafields:
                    [
                        { name: 'ResponsibilityTypeID', type: 'number' },
                        { name: 'ResponsibilityType', type: 'string' },
                        { name: 'Count', type: 'number' }
                    ]
                };

                ResponsibilityAdapter = new $.jqx.dataAdapter(ResponsibilitySource);

                $("#ResponsibilityGrid").jqxChart({
                    title: "",
                    description: "Select a slice below to view details.",
                    enableAnimations: true,
                    showLegend: true,
                    showBorderLine: false,
                    padding: { left: 0, top: 25, right: 75, bottom: 0 },
                    titlePadding: { left: 0, top: 0, right: 125, bottom: 0 },
                    legendLayout: { left: 370, top: 75, width: 250, height: 200, flow: 'vertical' },
                    source: ResponsibilityAdapter,
                    colorScheme: chartDefaultTheme,
                    seriesGroups: [{
                        useGradientColors: false,
                        type: 'pie',
                        showLabels: true,
                        series:
                            [
                                {
                                    useGradient: false,
                                    dataField: 'Count',
                                    displayText: 'ResponsibilityType',
                                    labelRadius: 115,
                                    initialAngle: 15,
                                    radius: 150,
                                    centerOffset: 0
                                }
                            ],
                        click: function (e) {
                            var data = ResponsibilityAdapter.records[e.elementIndex];
                            $('#PersonGridTitle').text('People Assigned as ' + data.ResponsibilityType);
                            $('#OwnedItemGridTitle').text('');
                            PersonSource.url = '/queries/' + data.ResponsibilityTypeID + '/ResourcesByResponsibilityType';
                            $("#PersonGrid").jqxGrid('clearselection');
                            $("#PersonGrid").jqxGrid('updatebounddata');
                            OwnedItemSource.url = null;
                            $("#OwnedItemGrid").jqxGrid('clearselection');
                            $("#OwnedItemGrid").jqxGrid('updatebounddata');
                        }
                    }]
                });

                //#endregion

                //#region PersonGrid

                PersonSource = {
                    datatype: 'json',
                    url: null,
                    type: 'get',
                    datafields:
                    [
                        { name: 'ResourceID', type: 'number' },
                        { name: 'ResponsibilityTypeID', type: 'number' },
                        { name: 'FirstName', type: 'string' },
                        { name: 'LastName', type: 'string' },
                        { name: 'OwnedItemCount', type: 'number' }
                    ]
                };

                PersonAdapter = new $.jqx.dataAdapter(PersonSource);

                $("#PersonGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    virtualmode: false,
                    filterable: true,
                    showfilterrow: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    pageable: true,
                    source: PersonAdapter,
                    theme: list_theme,
                    columns: [
                        {
                            datafield: "ResourceID",
                            text: "Resource",
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                return previewLinkRenderer('Resource', data.ResourceID, "#/resources/" + data.ResourceID, data.FirstName + ' ' + data.LastName);
                            }
                        },
                        { datafield: "OwnedItemCount", text: "# Owned Items", width: '125px', columntype: 'numberinput', filtertype: 'number' }
                    ]
                });

                //#endregion

                //#region OwnedItemGrid

                OwnedItemSource = {
                    datatype: 'json',
                    url: null,
                    type: 'get',
                    datafields:
                    [
                        { name: 'ObjectType', type: 'string' },
                        { name: 'ObjectID', type: 'number' },
                        { name: 'ObjectName', type: 'string' },
                        { name: 'ObjectTypeName', type: 'string' },
                        { name: 'RedFlagged', type: 'boolean' },
                        { name: 'ObjectUrl', type: 'string' },
                        { name: 'ContextItems', type: 'string' },
                        { name: 'CurrentScore', type: 'number' }
                    ]
                };

                OwnedItemAdapter = new $.jqx.dataAdapter(OwnedItemSource);

                $("#OwnedItemGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    virtualmode: false,
                    filterable: true,
                    showfilterrow: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    pageable: true,
                    source: OwnedItemAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "ObjectTypeName", text: "Type", width: '150px', columntype: 'dropdownlist', filtertype: 'checkedlist' },
                        {
                            datafield: "ObjectID",
                            width: '300px',
                            text: "Item",
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                return previewLinkRenderer(data.ObjectType, data.ObjectID, data.ObjectUrl, data.ObjectName);
                            }
                        },
                        { datafield: "ContextItems", text: "Context" },
                        { datafield: "RedFlagged", text: "Red-flagged", width: '125px', columntype: 'checkbox', filtertype: 'bool' }//,
                        //{ datafield: "CurrentScore", text: "Score", cellsrenderer: currentScoreRenderer, width: '100px' }
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $(document).on('resize', groupListPageResize);
                $("#PersonGrid").on('rowclick', personGridRowClick);
                $('#PersonSearchResults').on('rowdoubleclick', personSearchResultsRowDoubleClick);
                $('#GroupSearchResults').on('rowdoubleclick', groupSearchResultsRowDoubleClick);
                amplify.subscribe("RefreshActionMenu", refreshActionMenu);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}