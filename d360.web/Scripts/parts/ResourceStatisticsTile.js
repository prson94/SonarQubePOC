function ResourceStatisticsTile(controlID, type, id) {
    var source = $("#resourceStatisticsTile").html();
    var template = Handlebars.compile(source);

    controlID = '#' + controlID;

    $.getJSON(
        '/api/' + type + '/' + id + '/object/statistics',
        function (data) {
            $(controlID).html(
                template(data)
            );
            if ($(controlID).find('.ScoreKpi').length) {
                drawKpi($(controlID).find('.ScoreKpi'), 'Governance score', data.Score, 100 - data.Score, true);
            }
        }
    );
}