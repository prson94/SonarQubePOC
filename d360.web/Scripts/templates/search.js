
function SearchResultCategory(data) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.DisplayName = data.DisplayName;
    self.ResultCount = data.ResultCount;
    self.Categories = data.Categories;
    self.showRow = ko.observable(data.Name == 'Artifact'? true : false);
    self.toggleVisibility = function () {
        self.showRow(!self.showRow());        
    };
    self.showToggle = data.Categories != null;
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

        var showOnlyRelevantType = function (categoryType, e) {
            $('#CategoryResults a').removeClass('selected');
            $(e.target).addClass('selected');

            var searchSource = getSource(phrase, '', categoryType == 'All' ? '' : categoryType);

            var dataAdapter = getDataAdapter(searchSource);

            $('#SearchResults').jqxDataTable({ source: dataAdapter });
        }

        var showOnlyRelevantCategory = function (category,e) {
            $('#CategoryResults a').removeClass('selected');
            $(e.target).addClass('selected');
                        
            var searchSource = getSource(phrase, category, '');

            var dataAdapter = getDataAdapter(searchSource);

            $('#SearchResults').jqxDataTable({ source: dataAdapter });
        }

        //region Event Handlers

        function searchStringKeyPress(e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key 
                phrase = $("#SearchString").val();                
                $('#SearchResults').show();
                loadCategories = true;
                
                var searchSource = getSource(phrase,'','');
                
                var dataAdapter = getDataAdapter(searchSource);

                $('#SearchResults').jqxDataTable('goToPage', 0);
                                
                $('#SearchResults').jqxDataTable({ source: dataAdapter });
                
            }
        }

        function resultSelect(e) {            
            $('#ContentHeader').show();
            var el = $(e.target).closest('a');            
            document.location.href = el.attr('data-url');
        }

        function unsubscribe(data) {
            searchVm = null;
            $('#ContentHeader').show();
            $("#SearchString").off('keypress', searchStringKeyPress);
            $('#SearchResults').off('click', '.search-result-link', resultSelect);        
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        function getSource(term, selGroup, selType) {
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
                data: { search: term, from: 0, size: 10, group: selGroup, type: selType },
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
                            data.from = data.pagenum * data.pagesize;
                            data.size = data.pagesize;
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
                                if (data.Result.Matches == 0) {
                                    $('#SearchResults').hide();
                                    searchVm.elapsedTime("No search results found for the specified search term.");
                                }
                            }                            

                            if (loadCategories) {
                                msg = 'Search found ' + data.Result.Matches.toLocaleString() + ' matches in (' + (data.Result.ElapsedMS / 1000) + ' seconds)' + (data.Result.Matches > 10000 ? '  results limited to first 10,000 items.' : '');
                                searchVm.elapsedTime(msg);

                                data.Categories.unshift({ Name: 'All', ResultCount: data.Result.Matches, DisplayName: 'All' });
                                var cats = $.map(data.Categories, function (item) { return new SearchResultCategory(item); });
                                searchVm.categories(cats);

                                $('.search-category-link').each(function () {
                                    $(this).click(function (e) {
                                        var c = $(this).data("category");
                                        showOnlyRelevantCategory(c, e);
                                    });                                    
                                });

                                $('.search-type-link').each(function () {
                                    $(this).click(function (e) {
                                        var c = $(this).data("category-type");
                                        showOnlyRelevantType(c, e);
                                    });
                                    if ($(this).data("category-type") == "All") $(this).addClass('selected');
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
                                row.Merged = "<h4 class='search-result-name'><a data-url='/" + row.Url + "' class='search-result-link'>" + row.Name + "</a></h4><h5 class='search-result-attributes'>Category: <em class='result-category'>" + row.Type + "</em> &nbsp;&nbsp;Type: <em class='result-type'>" + row.Group + "</em></h5><p class='search-result-desc'>" + (row.Description != null ? row.Description : "") + "</p>";
                                data.push(row);
                            }

                            return data;
                        }
                    }
                );
        }

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

                $('#SearchResults').on('click', '.search-result-link', resultSelect);

                
                //#endregion
                phrase = $("#SearchString").val();
                             
                var source = getSource(phrase, '','');

                var dataAdapter = getDataAdapter(source);
                                
                $("#SearchResults").jqxDataTable(
                {                    
                    pageable: true,
                    pagerButtonsCount: 10,
                    serverProcessing: true,
                    pagerMode: 'default',
                    source: dataAdapter,                    
                    theme: 'transparent',
                    width: '98%',                    
                    pageSizeOptions: ['10', '20', '50'],
                    enableHover: false,
                    columns: [
                        { text: ' ', dataField: 'Merged', width: '100%'}                        
                    ]
                });

            });
    }
    app.get('#/search/:phrase', searchRoute);
    app.get('#/search', searchRoute);
}