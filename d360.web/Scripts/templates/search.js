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

        function simpleSearch() {
            searchCtrl.doSearch($("#home-search-text").val(), $('#search-exact-chk').is(':checked'));
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

                setInitialSearchMode();

                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
            });
    }
    app.get('#/search/:phrase', searchRoute);
    app.get('#/search', searchRoute);
    app.get('#/search/mode/:mode', searchRoute);
}