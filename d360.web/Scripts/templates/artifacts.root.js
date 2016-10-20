function artifacts_root(app, pageViewModel, templatePath, contextList) {
    app.get('#/artifacts', function (context) {
        context.app.swap('');

        var type = 'ArtifactType';
        var permissions = new PermissionsModel();

        pageViewModel.breadcrumbs = [];
        //pageViewModel.breadcrumbs.push({ Name: 'Business Glossary', Active: true });

        context.title(pageViewModel.Title);

        //#region Event Handlers

        function treeRowDoubleClick(event) {
            // row data.
            var row = event.args.row;
            location.assign('#/artifacts/' + row.ID);
        }

        function unsubscribe(data) {
            $("#Tree").off("rowDoubleClick", treeRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'artifacts.root.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                //$('#SideIcons').PageTools({ type: type, id: 0 });

                var ArtifactTreeGridSource =
                {
                    dataType: 'json',
                    url: '/internal/artifacts/typeswithstatistics',
                    dataFields: [
                        { name: 'ID', type: 'number' },
                        { name: 'ParentID', type: 'number' },
                        { name: 'Name', type: 'string' },
                        { name: 'Description', type: 'string' },
                        { name: 'Total', type: 'number' },
                        { name: 'Draft', type: 'number' },
                        { name: 'UnderReview', type: 'number' },
                        { name: 'Certified', type: 'number' }//,{ name: 'expanded', type: 'bool' }
                    ],
                    hierarchy: {
                        keyDataField: { name: 'ID' },
                        parentDataField: { name: 'ParentID' }
                    },
                    id: 'ID'
                };

                var ArtifactTreeGridAdapter = new $.jqx.dataAdapter(ArtifactTreeGridSource);

                //#region TreeGrid

                $("#Tree").jqxTreeGrid(
                {
                    width: '100%',
                    autoRowHeight: true,
                    source: ArtifactTreeGridAdapter,
                    filterable: false,
                    theme: theme,
                    altRows: true,
                    showHeader: true,
                    columns: [
                      { text: 'Name', dataField: 'Name', width: '200px' },
                      { text: 'Description', dataField: 'Description' },
                      { text: 'Draft', dataField: 'Draft', width: '75px' },
                      { text: 'Under Review', dataField: 'UnderReview', width: '100px' },
                      { text: 'Certified', dataField: 'Certified', width: '75px' },
                      { text: 'Total', dataField: 'Total', width: '75px' },
                      {
                          text: '', dataField: 'ID', width: '40px', filterable: false,
                          cellsRenderer: function (row, column, value, data) {
                              var tools = [];

                              tools.push({ isitemlink: true, urlprefix: '#/artifacts/{0}', type: 'ArtifactType', context: 'Preview' });

                              return renderToolsHtml(value, tools, contextList.ArtifactType, data);
                          }
                      }
                    ]
                });

                //#endregion

                //#region Event Subscriptions

                $("#Tree").on("rowDoubleClick", treeRowDoubleClick);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion

            });
    });
}