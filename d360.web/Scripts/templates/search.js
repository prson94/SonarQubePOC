function SearchResult(data) {
    var self = this;
    data = data || {};
    self.Name = data.Name;
    self.Url = data.Url;
    self.Description = data.Description;
    self.Type = data.Type;
}

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

    self.shouldShowSpinner = ko.observable(false);
    //self.shouldShowSpinner = function () {
    //    return (self.results().length <= 0);
    //};

    self.getResults = function (phrase) {

        //try {
            self.categories.removeAll();
            self.results.removeAll();
            self.elapsedTime('');

            self.shouldShowSpinner(true);

            $.ajax({
                data: 'search=' + phrase,
                url: '/search/results',
                dataType: 'json',
                type: 'POST',
                error: function () {
                    amplify.publish("ShowMessage", { title: 'Error When Searching', message: 'An error occurred while attempting to get search results.', type: 'error' });
                    self.shouldShowSpinner(false);
                },
                success: function (data) {
                    try {
                        if (data.Results.length > 0) {
                            var results = $.map(data.Results, function (item) { return new SearchResult(item); });
                            self.results(results);
                        }

                        if (data.Categories.length > 0) {
                            var cats = $.map(data.Categories, function (item) { return new SearchResultCategory(item); });
                            self.categories(cats);
                        }
                    } catch (e) {

                    }

                    self.shouldShowSpinner(false);
                    self.elapsedTime(data.ElapsedTime);
                }
            });
        //}
        //catch (e) {
        //    console.log(e);
        //}
    };

    return self;
}

function search(app, pageViewModel, templatePath, contextList) {
    var searchRoute = function(context) {
        context.app.swap('');

        //var phrase = context.params['phrase'];

        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        var searchVm;

        var showOnlyRelevantCategory = function (category) {

            try {
                $('#SearchResults .entry').each(function () {
                    if (category == '' || $(this).data("category") == category) {
                        $(this).show(300);
                    }
                    else {
                        $(this).hide(300);
                    }
                });
            }
            catch (e) {
                console.log(e);
            }
        }

        //#region Event Handlers

        function documentAjaxComplete() {
            try {
                $('#CategoryResults .entry a[data-category]').each(function () {
                    $(this).click(function () {
                        var c = $(this).data("category");
                        showOnlyRelevantCategory(c);
                    });
                });
            }
            catch (e) {
                console.log(e);
            }
        }

        function searchStringKeyPress(e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key
                searchVm.getResults($("#SearchString").val());
            }
        }

        function unsubscribe(data) {
            searchVm = null;

            $("#SearchString").off('keypress', searchStringKeyPress);
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        //#endregion

        context
            .render(templatePath + 'search.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: '', id: 0 });
                $('#SideIcons').PageTools("clear");

                searchVm = new SearchViewModel();
                try {
                    ko.applyBindings(searchVm, document.getElementById("SearchArea"));
                }
                catch (e) {
                    console.log(e);
                }

                //#region Event Registration

                $(document).ajaxComplete(documentAjaxComplete);
                $("#SearchString").on('keypress', searchStringKeyPress);
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

                //#endregion

                var phrase = $("#SearchString").val();
                if (phrase != '') {
                    searchVm.getResults(phrase);
                }

            });
    }
    app.get('#/search/:phrase', searchRoute);
    app.get('#/search', searchRoute);
}