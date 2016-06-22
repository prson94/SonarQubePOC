function search(app, pageViewModel, templatePath, contextList) {
    var searchRoute = function(context) {
        context.app.swap('');

        var searchCtrl;
        var initialmode = context.params['mode'] != undefined ? context.params['mode'].toUpperCase() : '';
                                
        //#region Event Handlers

        function unsubscribe(data) {
            searchCtrl = null;
                    
            $("#home-search-btn").off('click', simpleSearch);            
            $("#home-search-text").off('keypress', searchTextKeyPress);
            $(".adv-search-btn").off('click', toggleAdvancedSearch);
            $(".simple-search-btn").off('click', toggleAdvancedSearch);
            $("#do-adv-search-btn").off('click', advancedSearch);
            $("#SearchString").off('keypress', profileSearchKeyPress);

            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        }

        function searchTypeString() {
            var items = $("#SearchTypesDropdown").jqxDropDownList('getCheckedItems');
            var searchTypes = '';

            for (var i = 0; i < items.length; i++) {
                if (searchTypes.length > 0) searchTypes += ",";
                searchTypes += items[i].value;
            }

            if (searchTypes.length == 0) searchTypes = CompanySettings.DefaultSearchTypes;
            return searchTypes;
        }

        function simpleSearch(closeTypeahead) {
            var searchTypes = searchTypeString();            
            if(closeTypeahead === undefined || closeTypeahead) $('.tt-input').typeahead('close');

            if (searchTypes.length == 0) return;
            searchCtrl.doSearch($("#home-search-text").val(), $('#search-exact-chk').is(':checked'),searchTypes);
            $("#SearchString").val('');
        }

        function advancedSearch() {
            searchCtrl.doAdvancedSearch();
        }

        function searchTextKeyPress(e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key 
                simpleSearch();
            }
        }

        function toggleAdvancedSearch() {
            $(".searchinput").toggle();
            if ($("#advancedSearch").is(":visible"))
                searchCtrl.showAdvanced($("#home-search-text").val());
        }

        function profileSearchKeyPress(e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13) { //Enter key 
                $("#home-search-text").val($("#SearchString").val());
                simpleSearch();                
            }
        }

        //#endregion

        function setInitialSearchMode() {
            if (initialmode == 'ADVANCED')
            {
                toggleAdvancedSearch();
            }            
        }
                        
        context.title(pageViewModel.Title);

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });

        
        context
            .render(templatePath + 'search.html', pageViewModel)
            .appendTo(context.$element())
            .then(function (content) {                
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: '', id: 0 });
                $('#SideIcons').PageTools("clear");

                $("#advancedSearch").hide();
                                
                searchCtrl = new SearchResultsGrid(contextList, 10, context.params['phrase']);
                                
                $("#SearchString").on('keypress', profileSearchKeyPress);

                if ($("#SearchString").val().length != 0) {
                    $("#home-search-text").val($("#SearchString").val())
                    $("#SearchString").val('');
                }

                $("#home-search-text").focus();

                $("#home-search-text").on("keypress", searchTextKeyPress);

                $("#home-search-btn").click(simpleSearch);

                $(".simple-search-btn").click(toggleAdvancedSearch);
                $(".adv-search-btn").click(toggleAdvancedSearch);

                $("#do-adv-search-btn").click(advancedSearch);
                
                renderSearchTypesDropdown("SearchTypesDropdown");
                                
                setInitialSearchMode();

                SearchBarTypeahead('home-search-text', simpleSearch, searchTypeString, 15);

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    }
    app.get('#/search/:phrase', searchRoute);
    app.get('#/search', searchRoute);
    app.get('#/search/mode/:mode', searchRoute);
}