function roles_admin(app, pageViewModel, templatePath, contextList) {
    app.get('#/roles/administration', function (context) {
        context.app.swap('');
        context.title(pageViewModel.Title);

        var RolesSource;
        var RolesAdapter;

        //#region Event Handlers

        function listBindingComplete(event) {
            var rowCount = $('#List').jqxGrid('getdisplayrows').length;
            if (rowCount > 0) {
                $('#List').jqxGrid('selectrow', 0);
            }
        }

        function listRowSelect(event) {
            var args = event.args;
            var row = args.rowindex;
            var data = $("#List").jqxGrid('getrowdata', row);
            $('#ContentArea').PageNavigation("reload", 'Role', data.ID);
            //$('#SideIcons').PageTools("reload", 'Role', data.ID);
        }

        function saveAction(data) {
            try {
                switch (data.context) {
                    case contextList.Role:
                        $('#List').jqxGrid('updatebounddata');
                        break;
                }
            } catch (e) { }
        }

        function unsubscribe(data) {
            RolesAdapter = null;
            RolesSource = null;

            $("#List").off("bindingcomplete", listBindingComplete);
            $("#List").off("rowselect", listRowSelect);
            amplify.unsubscribe("SaveAction", saveAction);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'roles.admin.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                //#region Grid

                RolesSource = {
                    datatype: 'json',
                    url: '/security/_Roles',
                    datafields: [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'IsGlobal' }
                    ]
                };

                RolesAdapter = new $.jqx.dataAdapter(RolesSource);

                $("#List").jqxGrid({
                    width: grid_width,
                    altrows: true,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    source: adapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Name", text: "Name" },
                        { datafield: "IsGlobal", text: "Global?", cellsrenderer: booleanrenderer, width: '25%' }
                        //{ 
                        // text: '', 
                        // dataField: 'ID', 
                        // width: 120, 
                        // filterable: false, 
                        // cellsrenderer: function (row, column, value) {
                        //  var data = $("#List").jqxGrid('getrowdata', row);
                        //  var tools = [
                        //   { icon: 'pencil', urlprefix: '/form/roles/{0}/edit' },
                        //   { icon: 'trash-o', urlprefix: '/form/roles/{0}/delete' }
                        //  ];
                        //  return renderToolsHtml(value, tools, contextList.Role);
                        // }
                        //}
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $("#List").on("bindingcomplete", listBindingComplete);
                $("#List").on("rowselect", listRowSelect);
                amplify.subscribe("SaveAction", saveAction);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    });
}