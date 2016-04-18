function rules_list(app, pageViewModel, templatePath, contextList) {

    var getRuleRoute = function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
                
        var permissions = new PermissionsModel();
        var type = 'Rule';        
        pageViewModel.Title = 'Rules';
        pageViewModel.Directions = '';

        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Policies' });
        pageViewModel.breadcrumbs.push({ Name: 'Rules', Active: true });
                
        var RuleGridSource;
        var RuleGridAdapter;

        //#region Event Handlers
 

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Intersect:                        
                        break;
                    case contextList.Rule:
                        $('#RuleGrid').jqxGrid('updatebounddata');
                        break;                    
                }
            } catch (e) {
                logError("Children : SaveAction", e);
            }
        }

        function unsubscribe(data) {
            RuleGridAdapter = null;
            RuleGridSource = null;
                        
            $('#RuleGrid').off('rowdoubleclick', listRowDoubleClick);                   
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        function listRowDoubleClick(event) {
            var args = event.args;
            var row = args.rowindex;
            var data = $("#RuleGrid").jqxGrid('getrowdata', row);
            location.assign('#/rules/' + data.ID);
        }

        //#endregion

        context
            .render(templatePath + 'rules.list.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });
                
                //#region RuleGrid

                RuleGridSource = {
                    dataType: "json",
                    url: '/api/rules?$orderby=Name',
                    dataFields: [
                        { name: 'ID' },
                        { name: 'Name', type: 'string' },
                        { name: 'RuleType', type: 'int' },
                        { name: 'TypeName', type: 'string' }
                      //  { name: 'Actions' },
                    ],
                    id: 'ID'
                };
                                
                RuleGridAdapter = new $.jqx.dataAdapter(RuleGridSource, {
                    beforeLoadComplete: function (records) {
                        var data = new Array();
                        // update the loaded records. Dynamically add EmployeeName and EmployeeID fields. 
                        for (var i = 0; i < records.length; i++) {
                            var rule = records[i];
                            switch (rule.RuleType) {
                                case 1:
                                    rule.TypeName = 'Informational';
                                    break;
                                case 2:
                                    rule.TypeName = 'Quality Check';
                                    break;
                                case 3:
                                    rule.TypeName = 'Metric';
                                    break;
                                case 4:
                                    rule.TypeName = 'Profile';
                                    break;
                                default:
                                    rule.TypeName = 'Unknown';
                                    break;
                            }
                            //rule.Actions = rule.ID;
                            data.push(rule);
                        }
                        return data;
                    }
                });


                $("#RuleGrid").jqxGrid({
                    theme: list_theme,
                    width: grid_width,
                    pagesizeoptions: ['5', '10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    altrows: true,
                    source: RuleGridAdapter,
                    filterable: true,
                    showfilterrow: true,
                    columns: [
                        { text: 'ID', dataField: 'ID', width: '7%' },
                        { text: 'Name', dataField: 'Name', width: '78%' },
                        { text: 'Type', dataField: 'TypeName', width: '10%', filtertype: 'checkedlist' },
                        {
                            text: '', width: '80px',
                            cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {                                
                                var tools = [];
                                tools.push({ icon: 'pencil', urlprefix: 'form/editrule?id=' + data.ID });
                                tools.push({ icon: 'trash-o', urlprefix: 'form/deleterule?id=' + data.ID });

                                return renderToolsHtml(value, tools, contextList.Rule, data);
                            }
                        },
                    ]                    
                });

                //#endregion

                //#region Event Subscriptions                                
                $('#RuleGrid').on('rowdoubleclick', listRowDoubleClick);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);                
                //#endregion
            });
    }

    app.get('#/rules', getRuleRoute);    
}