function home(app, pageViewModel, templatePath, contextList, currentResourceID) {
    app.get('#/', function (context) {
        context.app.swap('');

        pageViewModel.breadcrumbs = [];
        pageViewModel.breadcrumbs.push({ Name: pageViewModel.Title, Active: true });
                
        var assignmentsTile;
        var socialTile;
        var activityTile;
        var daysToLookBack = 7;
        var searchCtrl = null;

        //#region Event Handlers

        function unsubscribe(data) {
            assignmentsTile = null;
            socialTile = null;
            activityTile = null;
            searchCtrl = null;

            $("#home-search-btn").off('click', simpleSearch);
            $("#home-search-btn").off('click', showAdvancedSearch);
            $("#home-search-text").off('keypress', searchTextKeyPress);
            
            amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
            amplify.unsubscribe("SaveAction", saveAction);
        }

        function saveAction(data) {                        
            try {
                loadAssignments();
            } catch (e) {
                logError("Home : SaveAction", e);
            }
        }

        function simpleSearch() {
            var items = $("#SearchTypesDropdown").jqxDropDownList('getCheckedItems');
            var searchTypes = '';

            for (var i = 0; i < items.length; i++) {
                if (searchTypes.length > 0) searchTypes += ",";
                searchTypes += items[i].value;
            }
            if (searchTypes.length == 0) return;
            searchCtrl.doSearch($("#home-search-text").val(), $('#search-exact-chk').is(':checked'), searchTypes);
            $("#SearchArea").show();
        }

        function showAdvancedSearch() {            
            $("#SearchString").val($("#home-search-text").val());
            location.href = '#/search/mode/advanced';
        }

        function searchTextKeyPress(e) {
            if (e.which == 13) {
                simpleSearch();
            }
        }
                
        //#endregion

        function loadAssignments() {
            assignmentsTile.LookBackDays = daysToLookBack;
            $.getJSON("/api/Count/Assignments/" + daysToLookBack, function (data) {
                assignmentsTile.Rows([]);
                assignmentsTile.Rows(data);
            });
        }

        function loadSocial() {            
            socialTile.LookBackDays = daysToLookBack;
            $.getJSON("/api/Count/Social/" + daysToLookBack, function (data) {
                socialTile.Rows(data);
            });
        }

        function loadActivity() {
            activityTile.LookBackDays = daysToLookBack;
            $.getJSON("/api/Count/Activity/" + daysToLookBack, function (data) {
                activityTile.Rows(data);
            });
        }

        function loadTileData() {
            loadAssignments();
            loadSocial();
            loadActivity();
        }

        context.title(pageViewModel.Title);
        context
            .render(templatePath + 'home.html', pageViewModel)
            .appendTo(context.$element())
            .then(function () {
                context.contentHeader(pageViewModel);

                $('#SideIcons').PageTools({ type: 'Resource', id: currentResourceID });
                $('#SideIcons').PageTools("clear");
                $("#SearchArea").hide();

                //#region Tiles
                                
                assignmentsTile = new HomePageCountTileModel('Your Assignments', daysToLookBack);
                assignmentsTile.NoDataMessage('');
                ko.applyBindings(assignmentsTile, document.getElementById('AssignmentsTile'));

                socialTile = new HomePageCountTileModel('Board', daysToLookBack);
                socialTile.NoDataMessage('');
                ko.applyBindings(socialTile, document.getElementById('SocialTile'));
                                
                activityTile = new HomePageCountTileModel('Activity', daysToLookBack);
                ko.applyBindings(activityTile, document.getElementById('ActivityTile'));

                //#endregion

                $("#dropDownButton").jqxDropDownButton({ width: 250, height: 25, autoOpen: true });
                $('#jqxTree').on('select', function (event) {
                    var args = event.args;
                    var item = $('#jqxTree').jqxTree('getItem', args.element);                    
                    daysToLookBack = $(args.element).data('days');
                    loadTileData()
                    var dropDownContent = '<div style="position: relative; margin-left: 3px; margin-top: 5px;">' + item.label + '</div>';
                    $("#dropDownButton").jqxDropDownButton('setContent', dropDownContent);
                });
                $("#jqxTree").jqxTree({ width: 200 });

                searchCtrl = new SearchResultsGrid(contextList, 5);

                $("#home-search-btn").click(simpleSearch);
                    
                amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
                amplify.subscribe("SaveAction", saveAction);

                $("#home-adv-btn").click(showAdvancedSearch);

                $("#home-search-text").on("keypress", searchTextKeyPress);
                
                var source = [
                    { val: "Attribute", display: "Attribute" },
                    { val: "FusionAttributes", display: "Fusion" },
                    { val: "FusionType", display: "Fusion Type" },
                    { val: "Artifact", display: "Glossary" },
                    { val: "Group", display: "Group" },
                    { val: "Taxonomy", display: "Model" },
                    { val: "Domain", display: "Reference" },
                    { val: "User", display: "User" }
                ];
                // Create a jqxDropDownList
                $("#SearchTypesDropdown").jqxDropDownList({ source: source, width: 200, height: 23, checkboxes: true, placeHolder: 'Search Types', displayMember: 'display', valueMember: 'val' });
                $("#SearchTypesDropdown").jqxDropDownList('checkAll');
                
                $("#home-search-text").focus();
            });
    });
}