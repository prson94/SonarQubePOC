function GlobalTypeahead() {
    var $vartypeahead = $("#SearchString");
    var engine = new Bloodhound({
        name: 'typeaheads',
        remote: {
            "url": '/search/typeahead?q=',
            prepare: function (query, settings) {
                settings.url += encodeURIComponent(query) + '&t=&num=10';
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
        limit: 10,
        templates: {
            suggestion: Handlebars.compile('<div><span class="type">{{Type}}:</span> {{{DisplayName}}}</div>'),            
         //   header: "<div class='header'>Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>"
        }
    }).on('typeahead:selected typeahead:autocompleted', function (e, datum) {
        document.location.assign(datum.Url);
    });
}