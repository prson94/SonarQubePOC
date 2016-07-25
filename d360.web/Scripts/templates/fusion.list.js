function fusion_list(app, pageViewModel, templatePath, contextList) {
    app.get('#/fusion', function (context) {
        context.app.swap('');

        var permissions = new PermissionsModel();

        pageViewModel.Title = 'Fusion';
        pageViewModel.Directions = 'View all fusion configurations and high-level histories on execution results and errors.';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Fusion', Active: true });
        //pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        context.title(pageViewModel.Title);

        var AgentHistoryGridSource;
        var AgentHistoryGridAdapter;
        var ExecutionHistoryGridSource;
        var ExecutionHistoryGridAdapter;
        var FusionSource;
        var FusionAdapter;

        //#region Event Handlers

        function listRowDoubleClick(event) {
            var args = event.args;
            var row = args.rowindex;
            var data = $("#List").jqxGrid('getrowdata', row);
            location.assign('#/fusion/' + data.FusionTypeID + '/' + data.ID);
        }

        function FusionStatisticsTile(controlID) {
            var source = $("#fusionStatisticsTile").html();
            var template = Handlebars.compile(source);

            controlID = '#' + controlID;

            $.getJSON(
                '/api/fusion/statistics',
                function (data) {
                    $(controlID).html(
                        template(data)
                    );

                    if ($(controlID).find('.AgentKpi').length) {                        
                        var score = (data.AgentExecutions > 0 && (data.AgentExecutions - data.AgentErrors) > 0) ? (((data.AgentExecutions - data.AgentErrors) / data.AgentExecutions) * 100).toFixed(0) : 100;
                        drawKpi($(controlID).find('.AgentKpi'), 'Agent % Success', score, 100 - score, true);
                    }
                    if ($(controlID).find('.FusionKpi').length) {                        
                        var score = (data.FusionExecutions > 0 && (data.FusionExecutions - data.FusionErrors) > 0) ? (((data.FusionExecutions - data.FusionErrors) / data.FusionExecutions) * 100).toFixed(0) : 100;
                        drawKpi($(controlID).find('.FusionKpi'), 'Processing % Success', score, 100 - score, true);
                    }
                }
            );
        }

        function unsubscribe(data) {
            AgentHistoryGridAdapter = null;
            AgentHistoryGridSource = null;
            ExecutionHistoryGridAdapter = null;
            ExecutionHistoryGridSource = null;
            FusionAdapter = null;
            FusionSource = null;
            PromotionHistoryGridSource = null;
            PromotionHistoryGridAdapter = null;

            $('#List').off('rowdoubleclick', listRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'fusion.list.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'Fusion', id: 0 });

                FusionStatisticsTile('FusionStatistics');

                //#region Grid Logic

                FusionSource = {
                    datatype: 'json',
                    type: 'get',
                    url: '/services/fusion/configurations?$orderby=FusionType,Name',
                    datafields:
                    [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'Description' },
                        { name: 'FusionType' },
                        { name: 'FusionTypeID' },
                        { name: 'Enabled' },
                        { name: 'Manual' }
                    ]
                };

                FusionAdapter = new $.jqx.dataAdapter(FusionSource);

                $("#List").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    autorowheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 20,
                    columnsresize: true,
                    source: FusionAdapter,
                    theme: list_theme,
                    groupable: false,
                    columns: [
                        { datafield: "FusionType", text: "Type", filtertype: 'checkedlist', width: '20%' },
                        { datafield: "Name", text: "Configuration", filtertype: 'checkedlist', width: '20%' },
                        {
                            datafield: "Description",
                            text: "Description",
                            filtertype: 'textbox'//,
                            //cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                            //    return "<div style='padding: 10px'>" + value + "</div>";
                            //}
                        },
                        { datafield: "Enabled", text: "Enabled?", width: '10%', columntype: 'checkbox', filtertype: 'bool', },
                        {
                            datafield: "ID",
                            text: "",
                            width: '10%',
                            filterable: false,
                            sortable: false,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                var tools = [
                                    { isitemlink: true, urlprefix: '#/fusion/' + data.FusionTypeID + '/{0}', type: 'Fusion', context: 'Preview' },
                                    { icon: 'filter', urlprefix: '/overlays/FusionConfigurationFilters?fusionTypeID=' + data.FusionTypeID + '&fusionID={0}', title: 'View/modify synchronization filters' }
                                ];

                                return renderToolsHtml(value, tools, contextList.Fusion, data);
                            }
                        }
                    ]
                });

                //#endregion

                //#region AgentHistory Grid Configuration

                AgentHistoryGridSource = {
                    datatype: 'json',
                    url: '/services/fusion/agenthistory?$top=100&$orderby=DateStarted desc',
                    datafields: [
                        { name: "FusionID", type: "number" },
                        { name: "FusionType", type: "string" },
                        { name: "Fusion", type: "string" },
                        { name: "DateStarted", type: "date" },
                        { name: "DateCompleted", type: "date" },
                        { name: "MachineQueuedOn", type: "string" },
                        { name: "Success", type: "bool" },
                        { name: "Message", type: "string" }
                    ]
                };

                AgentHistoryGridAdapter = new $.jqx.dataAdapter(AgentHistoryGridSource);

                $("#AgentHistoryGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    columnsresize: true,
                    source: AgentHistoryGridAdapter,
                    theme: list_theme,
                    groupable: false,
                    columns: [
                        { text: 'Type', datafield: 'FusionType', filtertype: 'checkedlist', width: '20%' },
                        { text: 'Configuration', datafield: 'Fusion', filtertype: 'checkedlist', width: '20%' },
                        { text: 'Started On', datafield: 'DateStarted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '15%' },
                        { text: 'Completed On', datafield: 'DateCompleted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '15%' },
                        { text: 'Agent', datafield: 'MachineQueuedOn', filtertype: 'checkedlist', width: '20%' },
                        { text: 'Success?', datafield: 'Success', columntype: 'checkbox', filtertype: 'bool', width: '10%' }
                    ]
                });

                //#endregion

                //#region ExecutionHistory Grid Configuration

                ExecutionHistoryGridSource = {
                    datatype: 'json',
                    url: '/services/fusion/executionhistory?$top=100&$orderby=DateStarted desc',
                    datafields: [
                        { name: "ID", type: "number" },
                        { name: "FusionID", type: "number" },
                        { name: "FusionType", type: "string" },
                        { name: "Fusion", type: "string" },
                        { name: "RawLogFileName", type: "string" },
                        { name: "DateStarted", type: "date" },
                        { name: "DateCompleted", type: "date" },
                        { name: "Adds", type: "number" },
                        { name: "Updates", type: "number" },
                        { name: "Deletes", type: "number" },
                        { name: "ErrorCount", type: "number" },
                        { name: "ResultCount", type: "number" }
                    ]
                };

                ExecutionHistoryGridAdapter = new $.jqx.dataAdapter(ExecutionHistoryGridSource);

                $("#ExecutionHistoryGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    columnsresize: true,
                    source: ExecutionHistoryGridAdapter,
                    theme: list_theme,
                    groupable: false,
                    columns: [
                        { text: 'Type', datafield: 'FusionType', filtertype: 'checkedlist', width: '18%' },
                        { text: 'Configuration', datafield: 'Fusion', filtertype: 'checkedlist', width: '20%' },
                        { text: 'Started On', datafield: 'DateStarted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '19%' },
                        { text: 'Completed On', datafield: 'DateCompleted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '19%' },
                        { text: '# Errors', datafield: 'ErrorCount', columntype: 'numberinput', filtertype: 'number', width: '10%' },
                        { text: '# Results', datafield: 'ResultCount', columntype: 'numberinput', filtertype: 'number', width: '10%' },
                        {
                            text: '',
                            dataField: 'ID',
                            width: '4%',
                            filterable: false,
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                var tools = [];
                                tools.push({ title: 'More info on this execution', icon: 'info', urlprefix: '/fusion/FusionExecution?id=' + data.ID });                                                                
                                return renderToolsHtml(value, tools, "openexceptiondetailsdlg", data);
                            }
                        }
                    ]
                });

                //#endregion

                //#region PromotionHistory Grid Configuration

                PromotionHistoryGridSource = {
                    datatype: 'json',
                    url: '/services/fusion/promotionhistory?$top=100&$orderby=DateStarted desc',
                    datafields: [
                        { name: "ID", type: "number" },
                        { name: "DateStarted", type: "date" },
                        { name: "DateCompleted", type: "date" },
                        { name: "PromotedTaxonomies", type: "number" },                        
                        { name: "PromotedDomainItems", type: "number" },
                        { name: "PromotedDomains", type: "number" },
                        { name: "PromotedArtifacts", type: "number" },
                        { name: "TotalNewPromotions", type: "number" },
                        { name: "AttributesConsidered", type: "number" },
                        { name: "NumberOfRules", type: "number" },
                        { name: "RelationshipsAdded", type: "number" }
                    ]
                };

                PromotionHistoryGridAdapter = new $.jqx.dataAdapter(PromotionHistoryGridSource);

                $("#PromotionHistoryGrid").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    pagesizeoptions: ['5', '10', '20'],
                    pagesize: 5,
                    columnsresize: true,
                    source: PromotionHistoryGridAdapter,
                    theme: list_theme,
                    groupable: false,
                    columns: [                        
                        { text: 'Started On', datafield: 'DateStarted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '20%' },
                        { text: 'Completed On', datafield: 'DateCompleted', cellsformat: 'MM/dd/yy h:mm:ss tt', filtertype: 'range', width: '20%' },
                        { text: '# New Promotions', datafield: 'TotalNewPromotions', columntype: 'numberinput', filtertype: 'number', width: 125 },
                        { text: '# New Artifacts', datafield: 'PromotedArtifacts', columntype: 'numberinput', filtertype: 'number', width: 100 },
                        { text: '# New Domains', datafield: 'PromotedDomains', columntype: 'numberinput', filtertype: 'number', width: 100 },
                        { text: '# New Domain Items', datafield: 'PromotedDomainItems', columntype: 'numberinput', filtertype: 'number', width: 135 },
                        { text: '# New Taxonomies', datafield: 'PromotedTaxonomies', columntype: 'numberinput', filtertype: 'number', width: 125 },
                        { text: '# New Relationships', datafield: 'RelationshipsAdded', columntype: 'numberinput', filtertype: 'number', width: 135 },
                        { text: '# Rules', datafield: 'NumberOfRules', columntype: 'numberinput', filtertype: 'number', width: 100 },
                        { text: '# Attributes Considered', datafield: 'AttributesConsidered', columntype: 'numberinput', filtertype: 'number', width: 150 }                      
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $('#List').on('rowdoubleclick', listRowDoubleClick);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });

    });
}