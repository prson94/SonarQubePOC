
function SearchResultCategory(data) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.ResultCount = data.ResultCount;
}

function SearchViewModel() {
    var self = this;
    self.categories = ko.observableArray();
    self.results = ko.observableArray();
    self.elapsedTime = ko.observable();
        
    return self;
}

function search(app, pageViewModel, templatePath, contextList) {
    var searchRoute = function(context) {
        context.app.swap('');
                
        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var searchVm;
        var phrase;
        var searchSource;
        var loadCategories;

        var showOnlyRelevantCategory = function (category) {
            
            var searchSource = getSource($("#SearchString").val(), category == 'All' ? '':category);

            var dataAdapter = getDataAdapter(searchSource);

            $('#SearchResults').jqxDataTable({ source: dataAdapter });
        }

        //region Event Handlers

        function searchStringKeyPress(e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key 
                $('#SearchResults').show();
                loadCategories = true;
                var searchSource = getSource($("#SearchString").val(),'');
                
                var dataAdapter = getDataAdapter(searchSource);
                
                $('#SearchResults').jqxDataTable({ source: dataAdapter });                
            }
        }

        function unsubscribe(data) {
            searchVm = null;

            $("#SearchString").off('keypress', searchStringKeyPress);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        function getSource(term, selGroup) {
            return {
                datatype: "json",
                datafields: [
                    { name: 'NormalizedScore', type: 'float' },
                    { name: 'Name', type: 'string' },
                    { name: 'Type', type: 'string' },
                    { name: 'Group', type: 'string' },
                    { name: 'Description', type: 'string' },
                    { name: 'ID', type: 'number' },                        
                    { name: 'Url', type: 'string' },
                    { name: 'Merged', type: 'string' },
                ],
                type: 'POST',
                dataType: 'json',
                url: '/search/results',
                data: { search: term, from: 0, size: 10, group: selGroup },
                id: 'ID',
                sortcolumn: 'NormalizedScore',
                sortdirection: 'desc',
                root: "Results",
                };
        }

        function getDataAdapter(source) {
            return new $.jqx.dataAdapter(source,
                    {
                        formatData: function (data) {
                            // update the $skip and $top params of the OData service.
                            // data.pagenum - page number starting from 0.
                            // data.pagesize - page size
                            data.from = data.pagenum * data.pagesize;
                            data.size = data.pagesize;
                            //  data.$inlinecount = "allpages";
                            return data;
                        },
                        downloadComplete: function (data, status, xhr) {
                            if (!source.totalRecords) {
                                source.totalRecords = parseInt(data.Result.Matches);
                                if (source.totalRecords > 10000) source.totalRecords = 10000;
                            }
                        },
                        loadComplete: function (data) {
                            var msg = "";
                            
                            if (data) {
                                msg = 'Search found ' + data.Result.Matches + ' matches in (' + (data.Result.ElapsedMS / 1000) + ' seconds).' + (data.Result.Matches > 10000 ? '  Results limited to first 10000 items.' : '');
                                if (data.Result.Matches == 0) $('#SearchResults').hide();
                            }
                            searchVm.elapsedTime(msg);

                            if (loadCategories) {
                                data.Categories.unshift({ Name: 'All', ResultCount: 0 });
                                var cats = $.map(data.Categories, function (item) { return new SearchResultCategory(item); });
                                searchVm.categories(cats);
                                

                                $('#CategoryResults .entry a[data-category]').each(function () {
                                    $(this).click(function () {
                                        var c = $(this).data("category");
                                        showOnlyRelevantCategory(c);
                                    });
                                });
                                loadCategories = false;
                            }
                        },
                        loadError: function (xhr, status, error) {
                            throw new Error(error.toString());
                        },
                        beforeLoadComplete: function (records) {                    
                            var data = new Array();
                            for (var i = 0; i < records.length; i++) {
                                var row = records[i];                        
                                row.Merged = "<h4 class='search-result-name'><a href='/" + row.Url + "'>" + row.Name + "</a></h4><h5 class='search-result-attributes'>Category: " + row.Type + " &nbsp;&nbsp;Type: " + row.Group + "</h5><p class='search-result-desc'>" + (row.Description != null ? row.Description : "") + "</p>";
                                data.push(row);
                            }
                                                         
                            return data;
                        }
                    }
                );
        }

        //#endregion

        context
            .render(templatePath + 'search.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: '', id: 0 });
                $('#SideIcons').PageTools("clear");
                $('#ContentHeader').hide();

                loadCategories = true;

                searchVm = new SearchViewModel();
                try {
                    ko.applyBindings(searchVm, document.getElementById("SearchArea"));
                }
                catch (e) {
                    console.log(e);
                }

                //#region Event Registration
                                
                $("#SearchString").on('keypress', searchStringKeyPress);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion
                             
                var source = getSource($("#SearchString").val(),'');

                var dataAdapter = getDataAdapter(source);
                                
                $("#SearchResults").jqxDataTable(
                {                    
                    pageable: true,
                    pagerButtonsCount: 10,
                    serverProcessing: true,
                    pagerMode: 'default',
                    source: dataAdapter,
                    altRows: false,
                    theme: 'transparent',
                    width: '98%',
                    columnsResize: true,
                    pageSizeOptions: ['10', '20', '50'],
                    enableHover: false,
                    columns: [
                        { text: ' ', dataField: 'Merged' },
                        { text: 'Score', dataField: 'NormalizedScore', width: '15%', cellsformat: 'p2', cellClassName: 'search-score-cell' },
                        
                    ]
                });

            });
    }
    app.get('#/search/:phrase', searchRoute);
    app.get('#/search', searchRoute);
}