function load(app, pageViewModel, templatePath, contextList) {
    app.get('#/load', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);
        
        var type = 'Load';

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: 'Administration' });
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var permissions = new PermissionsModel();

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

        function loadsTileRowSelect(event) {
            var data = $("#LoadsTile").jqxGrid('getrowdata', event.args.rowindex);

            permissions.GetPermissionsForObject(type, data.ID);

            amplify.publish(AmplifyActions.TileUnsubscribe, {});

            $('#SideIcons').PageTools("reload", type, data.ID);

            DetailTile('DetailTile', contextList, permissions, type, data.ID);

            $.getJSON('/api/loads/' + data.ID + '/columns', {}, function (columnData) {
                $('#LoadItemsTile').jqxGrid('clearselection');
                sourceLoadItems.datafields = [{ name: 'LoadID' }, { name: 'RowIndex' }, { name: 'Status' }, { name: 'StatusMessage' }];
                $.each(columnData, function () {
                    sourceLoadItems.datafields.push({ name: this.datafield });
                });
                columnData.push({ datafield: 'RowIndex', text: 'Row' });
                columnData.push({ datafield: 'Status', text: 'Status' });
                columnData.push({ datafield: 'StatusMessage', text: 'Message' });
                $("#LoadItemsTile").jqxGrid({ columns: columnData });
                sourceLoadItems.url = '/api/loads/' + data.ID + '/items';
                $("#LoadItemsTile").jqxGrid('updatebounddata');
            });
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Load:
                        $('#LoadsTile').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            sourceLoads = null;
            adapterLoads = null;
            sourceLoadItems = null;
            adapterLoadItems = null;

            $("#LoadsTile").off("rowselect", loadsTileRowSelect);
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
                        tools.push({ icon: 'plus', uri: '/form/AddLoad', context: contextList.Load, title: 'Add bulk load' });
                    }
                    TileTools('#LoadTools', tools);

                    //#region Load Grid

                    sourceLoads = {
                        datatype: 'json',
                        url: '/api/loads',
                        datafields:
                        [
                            { name: 'ID', type: 'number' },
                            { name: 'ObjectName', type: 'string' },
                            { name: 'DateStarted', type: 'date' },
                            { name: 'DateCompleted', type: 'date' },
                            { name: 'Action', type: 'string' },
                            { name: 'Notes', type: 'string' }
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
                            { datafield: "Action", text: "Action" },
                            { datafield: "ObjectName", text: "Target" },
                            { datafield: "DateCompleted", text: "Date Completed", cellsformat: 'f' }
                        ]
                    });

                    //#endregion

                    //#region LoadItems Grid

                    sourceLoadItems = {
                        datatype: 'json',
                        url: null,
                        datafields: []
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

                    $("#LoadsTile").on("rowselect", loadsTileRowSelect);
                    amplify.subscribe("SaveAction", saveAction);
                    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                    //#endregion
                }

                permissions.GetPermissionsForObject(type, 0).then(loadAfterPermissionsRetrieved);
            });
    });
}