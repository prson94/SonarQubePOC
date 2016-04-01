function SearchResultsGrid(contextList, defaultItemsPerPage, initialPhrase) {
    var phrase;    
    var loadCategories;
    var searchVm;
    var self = this;
    var advSearchText;

    mainCtrlId = 'SearchArea';
    categoriesCtrlId = 'CategoryResults';
    resultsCtrlId = 'SearchResults';
    if (defaultItemsPerPage === undefined) defaultItemsPerPage = 10;
    if (initialPhrase !== undefined) phrase = initialPhrase;


    var resultsctrl = '#' + resultsCtrlId;
    var categoryctrl = '#' + categoriesCtrlId;

    searchVm = new SearchViewModel();
    try {
        ko.applyBindings(searchVm, document.getElementById(mainCtrlId));
    }
    catch (e) {
        console.log(e);
    }

    //#region Event Registration


    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion

    self.loadCategories = true;

    if ($("#SearchString").val().length == 0 && phrase !== undefined && phrase.length > 0)
        $("#SearchString").val(phrase);

    phrase = $("#SearchString").val();

    if (phrase.length > 0)
    {
        var searchSource = getSource(phrase, '', '');

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable(
        {
            pageable: true,
            pagerButtonsCount: 10,
            serverProcessing: true,
            pagerMode: 'default',
            source: dataAdapter,
            theme: 'transparent',
            width: '98%',
            enableHover: false,
            showHeader: false,
            columns: [
                { text: ' ', dataField: 'Merged', width: '99%' }
            ]
        });
    }
        

    //region Event Handlers

    function unsubscribe(data) {
        searchVm = null;
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    //#endregion
        

    self.doSearch = function (val) {
        phrase = val;
        advSearchText = '';

        $(resultsctrl).show();
        self.loadCategories = true;

        var searchSource = getSource(phrase, '', '');

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable('goToPage', 0);

        $(resultsctrl).jqxDataTable(
        {
            pageable: true,
            pagerButtonsCount: 10,
            serverProcessing: true,
            pagerMode: 'default',
            source: dataAdapter,
            theme: 'transparent',
            width: '98%',
            enableHover: false,
            showHeader: false,
            columns: [
                { text: ' ', dataField: 'Merged', width: '99%' }
            ]
        });
    }

    self.doAdvancedSearch = function () {
        advSearchText = searchVm.advancedFilterJSON();
        phrase = '';

        $(resultsctrl).show();
        self.loadCategories = true;

        var searchSource = getSource(phrase, '', '', advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable('goToPage', 0);

        $(resultsctrl).jqxDataTable(
        {
            pageable: true,
            pagerButtonsCount: 10,
            serverProcessing: true,
            pagerMode: 'default',
            source: dataAdapter,
            theme: 'transparent',
            width: '98%',
            enableHover: false,
            showHeader: false,
            columns: [
                { text: ' ', dataField: 'Merged', width: '99%' }
            ]
        });
    }

    self.showAdvanced = function (text) {
        searchVm.showAdvanced(text);
    }

    var showOnlyRelevantType = function (categoryType, e) {
        $(categoryctrl + ' a').removeClass('selected');
        $(e.target).addClass('selected');

        var searchSource = getSource(phrase, '', categoryType == 'All' ? '' : categoryType, advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    var showOnlyRelevantCategory = function (category, e) {
        $(categoryctrl + ' a').removeClass('selected');
        $(e.target).addClass('selected');

        var searchSource = getSource(phrase, category, '', advSearchText);

        var dataAdapter = getDataAdapter(searchSource);

        $(resultsctrl).jqxDataTable({ source: dataAdapter });
    }

    function getSource(term, selGroup, selType, advCriteria) {
        return {
            datatype: "json",
            pagesize: defaultItemsPerPage,
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
            data: { search: term, from: 0, size: defaultItemsPerPage, group: selGroup, type: selType, adv: (advCriteria === undefined ? '' : advCriteria) },
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
                                $(resultsctrl).hide();
                                searchVm.elapsedTime("No search results found for the specified search term.");
                            }
                        }
                        
                        if (self.loadCategories) {
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

                            self.loadCategories = false;
                        }
                    },
                    loadError: function (xhr, status, error) {
                        throw new Error(error.toString());
                    },
                    beforeLoadComplete: function (records) {
                        var data = new Array();
                        for (var i = 0; i < records.length; i++) {
                            var row = records[i];
                            row.Merged = "<div class='search-res-container'><h4 class='search-result-name'><a href='/" + row.Url + "' class='search-result-link'>" + row.Name + "</a></h4><p class='search-result-desc'>" + (row.Description != null ? row.Description : "") + "</p><h5 class='search-result-attributes'>Category: <em class='result-category'>" + row.Type + "</em> &nbsp;&nbsp;Type: <em class='result-type'>" + row.Group + "</em></h5></div>";
                            data.push(row);
                        }

                        return data;
                    }
                }
            );
    }
}