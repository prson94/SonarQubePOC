function SearchBarTypeahead(searchInputID,simpleSearchCallback,seachTypesCallback,numItems) {

    var $vartypeahead = $("#" + searchInputID);
    var engine = new Bloodhound({
        name: 'typeaheads',
        remote: {
            "url": '/search/typeahead?q=',
            prepare: function (query, settings) {
                settings.url += encodeURIComponent(query) + '&t=' + encodeURIComponent(seachTypesCallback());
                return settings;
            }
        },
        datumTokenizer: function (d) { return d; },
        queryTokenizer: function (d) { return d; }
    });
    engine.initialize();

    $vartypeahead.typeahead({
        "minLength": 2,
        "highlight": true
    },
    {
        "source": engine.ttAdapter(),
        display: 'Name',
        limit: numItems,
        templates: {
            suggestion: Handlebars.compile('<div><span class="type">{{Type}}:</span> {{{DisplayName}}}{{#if Desc}} <p class="desc">{{{Desc}}}</p>{{/if}}</div>'),
            //        footer: Handlebars.compile("<div class='search'><span class='type'>Search:</span> '{{query}}'</div>")                        
            header: "<div class='header'>Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>"
        }
    }).on('typeahead:selected typeahead:autocompleted', function (e, datum) {
        document.location.assign(datum.Url);
    }).on('typeahead:cursorchange', function (e, datum) {
        simpleSearchCallback(false);
    });
}