function load(app, pageViewModel, templatePath, contextList) {
    app.get('#/load', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
        
        var type = 'LoadType';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

        var source;
        var adapter;
        var sourceFields;
        var adapterFields;
        var sourceRules;
        var adapterRules;
        var sourceRuleItems;
        var adapterRuleItems;
        var sourceLoads;
        var adapterLoads;
        var sourceLoadItems;
        var adapterLoadItems;

        //#region Event Handlers

        function historyRefreshButtonClick() {
            $('#LoadsTile').jqxGrid('clearselection');
            $("#LoadsTile").jqxGrid('updatebounddata');
            sourceLoadItems.url = null;
            $("#LoadItemsTile").jqxGrid('updatebounddata');
        }

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var data = $("#List").jqxGrid('getrowdata', event.args.rowindex);

            permissions.GetPermissionsForObject(type, data.ID);

            amplify.publish(AmplifyActions.TileUnsubscribe, {});

            $('#SideIcons').PageTools("reload", type, data.ID);

            DetailTile('DetailTile', contextList, permissions, type, data.ID);

            $('#FieldsTile').jqxGrid('clearselection');
            sourceFields.url = '/api/loadtypes/' + data.ID + '/fields';
            $("#FieldsTile").jqxGrid('updatebounddata');

            TileTools('#FieldsTileContainerTools', [
                { icon: 'plus', uri: '/form/AddLoadTypeField?id=' + data.ID, context: contextList.LoadTypeField, title: 'Add field' }
            ]);

            $('#RulesTile').jqxGrid('clearselection');
            sourceRules.url = '/api/loadtypes/' + data.ID + '/rules';
            $("#RulesTile").jqxGrid('updatebounddata');

            TileTools('#RulesTileContainerTools', [
                { icon: 'plus', uri: '/form/AddLoadTypeRule?id=' + data.ID, context: contextList.LoadTypeRule, title: 'Add rule' }
            ]);

            $('#RuleItemsTile').jqxGrid('clearselection');
            sourceRuleItems.url = null;
            $("#RuleItemsTile").jqxGrid('updatebounddata');

            $('#LoadsTile').jqxGrid('clearselection');
            sourceLoads.url = '/api/loadtypes/' + data.ID + '/history';
            $("#LoadsTile").jqxGrid('updatebounddata');

            $('#LoadItemsTile').jqxGrid('clearselection');
            sourceLoadItems.url = null;
            $("#LoadItemsTile").jqxGrid('updatebounddata');
        }

        function loadsTileRowSelect(event) {
            var data = $("#LoadsTile").jqxGrid('getrowdata', event.args.rowindex);

            $('#LoadItemsTile').jqxGrid('clearselection');
            sourceLoadItems.url = '/api/loadtypes/' + data.LoadTypeID + '/history/' + data.ID + '/results';
            $("#LoadItemsTile").jqxGrid('updatebounddata');
        }

        function rulesTileRowSelect(event) {
            var data = $("#RulesTile").jqxGrid('getrowdata', event.args.rowindex);

            $('#RuleItemsTile').jqxGrid('clearselection');
            sourceRuleItems.url = '/api/loadtypes/' + data.LoadTypeID + '/rules/' + data.ID + '/items';
            $("#RuleItemsTile").jqxGrid('updatebounddata');
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.LoadType:

                        DetailTile('DetailTile', contextList, permissions, type, data.id);

                        sourceFields.url = null;
                        $("#FieldsTile").jqxGrid('updatebounddata');

                        sourceRules.url = null;
                        $("#RulesTile").jqxGrid('updatebounddata');
                        sourceRuleItems.url = null;
                        $("#RuleItemsTile").jqxGrid('updatebounddata');

                        sourceLoads.url = null;
                        $("#LoadsTile").jqxGrid('updatebounddata');
                        sourceLoadItems.url = null;
                        $("#LoadItemsTile").jqxGrid('updatebounddata');

                        $('#List').jqxGrid('updatebounddata');

                        amplify.publish("RefreshNavigation");
                        break;
                    case contextList.Load:
                        $('#LoadsTile').jqxGrid('updatebounddata');
                        break;
                    case contextList.LoadTypeField:
                        $('#FieldsTile').jqxGrid('updatebounddata');
                        $('#SideIcons').PageTools("refresh");
                        break;
                    case contextList.LoadTypeRule:
                        $('#RulesTile').jqxGrid('updatebounddata');
                        $('#SideIcons').PageTools("refresh");
                        break;
                    case contextList.LoadTypeRuleItem:
                        $('#RuleItemsTile').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            source = null;
            adapter = null;
            sourceFields = null;
            adapterFields = null;
            sourceRules = null;
            adapterRules = null;
            sourceRuleItems = null;
            adapterRuleItems = null;
            sourceLoads = null;
            adapterLoads = null;
            sourceLoadItems = null;
            adapterLoadItems = null;

            $('#history-refresh-button').off('click', historyRefreshButtonClick);
            $("#List").off("rowselect", listRowSelect);
            $("#List").off("bindingcomplete", listBindingComplete);
            $("#LoadsTile").off("rowselect", loadsTileRowSelect);
            $("#RulesTile").off("rowselect", rulesTileRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'load.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: type, id: 0 });

                var loadAfterPermissionsRetrieved = function () {
                    var tools = [];
                    if (permissions.HasPermission("Root", "Create")) {
                        tools.push({ icon: 'plus', uri: '/form/AddLoadType', context: contextList.LoadType, title: 'Add bulk load type' });
                    }
                    TileTools('#ListTools', tools);

                    //#region Grid

                    source = {
                        datatype: 'json',
                        url: '/api/LoadTypes',
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Name' }
                        ]
                    };

                    adapter = new $.jqx.dataAdapter(source);

                    $("#List").jqxGrid({
                        altrows: true,
                        pagermode: 'simple',
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: adapter,
                        theme: theme,
                        columns: [
                            { datafield: "Name", text: "Name" },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                sortable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    return renderToolsHtml(
                                        value,
                                        [
                                            { icon: 'pencil', urlprefix: '/form/EditLoadType?id={0}' },
                                            { icon: 'trash-o', urlprefix: '/form/DeleteLoadType?id={0}' }
                                        ],
                                        contextList.LoadType
                                   );
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Field Grid

                    sourceFields = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'Name' },
                            { name: 'LookupType' },
                            { name: 'LookupName' },
                            { name: 'LookupField' },
                            { name: 'SortOrder' }
                        ]
                    };

                    adapterFields = new $.jqx.dataAdapter(sourceFields);

                    $("#FieldsTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        filtermode: 'excel',
                        pageable: true,
                        source: adapterFields,
                        theme: list_theme,
                        columns: [
                            { datafield: "SortOrder", text: "", width: '25px', filterable: false },
                            { datafield: "Name", text: "Name" },
                            { datafield: "LookupType", text: "Lookup Type", width: '15%' },
                            { datafield: "LookupName", text: "Lookup", width: '15%' },
                            { datafield: "LookupField", text: "Lookup Field", width: '15%' },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    if (permissions.HasPermission('Root', 'Update')) {
                                        return renderToolsHtml(
                                            value,
                                            [
                                                { icon: 'pencil', urlprefix: '/form/EditLoadTypeField?id={0}' },
                                                { icon: 'trash-o', urlprefix: '/form/DeleteLoadTypeField?id={0}' }
                                            ],
                                            contextList.LoadTypeField
                                       );
                                    }
                                    else {
                                        return '';
                                    }
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Rules Grid

                    sourceRules = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'LoadTypeID' },
                            { name: 'LoadTypeRuleGroupName' },
                            { name: 'ObjectType' },
                            { name: 'ObjectName' },
                            { name: 'UniqueLoadTypeField' },
                            { name: 'RuleItemCount' },
                            { name: 'SortOrder' }
                        ]
                    };

                    adapterRules = new $.jqx.dataAdapter(sourceRules);

                    $("#RulesTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        filtermode: 'excel',
                        pageable: true,
                        source: adapterRules,
                        theme: list_theme,
                        columns: [
                            { datafield: "SortOrder", text: "", width: '25px', filterable: false },
                            { datafield: "LoadTypeRuleGroupName", text: "Rule Type" },
                            { datafield: "ObjectType", text: "Type", width: '15%' },
                            { datafield: "ObjectName", text: "Object", width: '15%' },
                            { datafield: "UniqueLoadTypeField", text: "Unique Field", width: '15%' },
                            { datafield: "RuleItemCount", text: "# Items", width: '50px', filterable: false },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 120,
                                filterable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    if (permissions.HasPermission('Root', 'Update')) {
                                        return renderToolsHtml(
                                            value,
                                            [
                                                { icon: 'plus', urlprefix: '/form/AddLoadTypeRuleItem?id={0}', context: contextList.LoadTypeRuleItem },
                                                { icon: 'pencil', urlprefix: '/form/EditLoadTypeRule?id={0}' },
                                                { icon: 'trash-o', urlprefix: '/form/DeleteLoadTypeRule?id={0}' }
                                            ],
                                            contextList.LoadTypeRule
                                       );
                                    }
                                    else {
                                        return '';
                                    }
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region RuleItems Grid

                    sourceRuleItems = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID' },
                            { name: 'IsCustomField' },
                            { name: 'LoadTypeRuleID' },
                            { name: 'TargetFieldName' },
                            { name: 'SourceLoadTypeField' }
                        ]
                    };

                    adapterRuleItems = new $.jqx.dataAdapter(sourceRuleItems);

                    $("#RuleItemsTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        filtermode: 'excel',
                        pageable: true,
                        source: adapterRuleItems,
                        theme: list_theme,
                        columns: [
                            { datafield: "SourceLoadTypeField", text: "Source Field" },
                            { datafield: "TargetFieldName", text: "Target Field" },
                            { datafield: "IsCustomField", text: "Custom?", width: '20%', cellsrenderer: booleanrenderer },
                            {
                                text: '',
                                dataField: 'ID',
                                width: 80,
                                filterable: false,
                                cellsrenderer: function (index, datafield, value, defaultvalue, column, data) {
                                    if (permissions.HasPermission('Root', 'Update')) {
                                        return renderToolsHtml(
                                            value,
                                            [
                                                { icon: 'pencil', urlprefix: '/form/EditLoadTypeRuleItem?id={0}' },
                                                { icon: 'trash-o', urlprefix: '/form/DeleteLoadTypeRuleItem?id={0}' }
                                            ],
                                            contextList.LoadTypeRuleItem
                                       );
                                    }
                                    else {
                                        return '';
                                    }
                                }
                            }
                        ]
                    });

                    //#endregion

                    //#region Load Grid

                    sourceLoads = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID', type: 'number' },
                            { name: 'LoadTypeID', type: 'number' },
                            { name: 'Date', type: 'date' },
                            { name: 'ItemCount', type: 'number' }
                        ]
                    };

                    adapterLoads = new $.jqx.dataAdapter(sourceLoads);

                    $("#LoadsTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: adapterLoads,
                        theme: list_theme,
                        columns: [
                            { datafield: "Date", text: "Date", cellsformat: 'f' },
                            { datafield: "ItemCount", text: "# Items", width: '75px' }
                        ]
                    });

                    //#endregion

                    //#region LoadItems Grid

                    sourceLoadItems = {
                        datatype: 'json',
                        url: null,
                        datafields:
                        [
                            { name: 'ID', type: 'number' },
                            { name: 'RowIndex', type: 'number' },
                            { name: 'LoadTypeRuleGroup', type: 'string' },
                            { name: 'SortOrder', type: 'number' },
                            { name: 'Value', type: 'string' },
                            { name: 'Message', type: 'string' }
                        ]
                    };

                    adapterLoadItems = new $.jqx.dataAdapter(sourceLoadItems);

                    $("#LoadItemsTile").jqxGrid({
                        altrows: true,
                        width: grid_width,
                        pagesizeoptions: ['10', '20', '50'],
                        pagesize: 20,
                        autorowheight: true,
                        autoheight: true,
                        sortable: true,
                        filterable: true,
                        showfilterrow: true,
                        pageable: true,
                        source: adapterLoadItems,
                        theme: list_theme,
                        columns: [
                            { datafield: "RowIndex", text: "Row #", width: '75px' },
                            { datafield: "LoadTypeRuleGroup", text: "Type" },
                            { datafield: "Value", text: "Result", width: '150px' },
                            { datafield: "Message", text: "Message" }
                        ]
                    });

                    //#endregion

                    //#region Event Subscriptions

                    $('#history-refresh-button').on('click', historyRefreshButtonClick);
                    $("#List").on("rowselect", listRowSelect);
                    $("#List").one("bindingcomplete", listBindingComplete);
                    $("#LoadsTile").on("rowselect", loadsTileRowSelect);
                    $("#RulesTile").on("rowselect", rulesTileRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}