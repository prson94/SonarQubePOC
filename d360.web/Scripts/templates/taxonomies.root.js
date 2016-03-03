function taxonomies_root(app, pageViewModel, templatePath, contextList) {
    var r = function (context) {
        context.app.swap('');

        var selectedClassification = context.params['classification'];

        if (selectedClassification) {
            pageViewModel.Title = selectedClassification + ' Models';
        }
        else {
            pageViewModel.Title = 'Models';
        }

        context.title(pageViewModel.Title);
        pageViewModel.breadcrumbs = [];

        var TaxonomyTypeSource;
        var TaxonomyTypeAdapter;

        //#region Event Handlers

        function listRowDoubleClick(event) {
            var args = event.args;
            var row = args.rowindex;
            var data = $("#List").jqxGrid('getrowdata', row);
            location.assign('#/catalogs/' + data.ID);
        }

        function unsubscribe(data) {
            TaxonomyTypeAdapter = null;
            TaxonomyTypeSource = null;

            $('#List').off('rowdoubleclick', listRowDoubleClick);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'taxonomies.root.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                TaxonomyTypeSource = {
                    datatype: 'json',
                    url: '/api/catalogs',
                    datafields: [
                        { name: 'ID' },
                        { name: 'Name' },
                        { name: 'Description' },
                        { name: 'TaxonomyTypeClass' },
                        { name: 'MaximumDepth' }
                    ]
                };

                TaxonomyTypeAdapter = new $.jqx.dataAdapter(TaxonomyTypeSource);

                $("#List").jqxGrid({
                    altrows: true,
                    width: grid_width,
                    pagesizeoptions: ['10', '20', '50'],
                    pagesize: 20,
                    autoheight: true,
                    autorowheight: true,
                    sortable: true,
                    filterable: true,
                    showfilterrow: true,
                    pageable: true,
                    source: TaxonomyTypeAdapter,
                    theme: list_theme,
                    columns: [
                        { datafield: "Name", text: "Name", width: 200 },
                        { datafield: "TaxonomyTypeClass", text: "Classification", filtertype: 'checkedlist', width: 120 },
                        { datafield: "Description", text: "Description" },
                        { datafield: "MaximumDepth", text: "Maximum Depth", filtertype: 'checkedlist', width: 125 },
                        {
                            text: '',
                            dataField: 'ID',
                            width: 40,
                            filterable: false,
                            cellsrenderer: function (row, column, value) {
                                var tools = [
                                    { isitemlink: true, urlprefix: '#/catalogs/{0}' }
                                ];
                                return renderToolsHtml(value, tools, contextList.TaxonomyType);
                            }
                        }
                    ],
                    ready: function () {
                        if (selectedClassification) {
                            var filtergroup = new $.jqx.filter();
                            var filter = filtergroup.createfilter('stringfilter', selectedClassification, 'EQUAL');
                            filtergroup.addfilter(0, filter);
                            $('#List').jqxGrid('addfilter', 'TaxonomyTypeClass', filtergroup, true);
                            $('#List').jqxGrid('hidecolumn', 'TaxonomyTypeClass');
                        }
                    }
                });

                //#region Event Subscriptions

                $('#List').on('rowdoubleclick', listRowDoubleClick);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
            });
    };

    app.get('#/catalogs', r);
    app.get('#/catalogs?classification=:classification', r);
}