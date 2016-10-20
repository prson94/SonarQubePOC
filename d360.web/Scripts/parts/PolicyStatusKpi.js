function PolicyStatusKpi(controlID, contextList, permissions, id) {

    var calendarControlID = controlID + "_calendar";
    var graphicControlID = controlID + "_graphic";
    controlID = '#' + controlID;

    var date = '10-20-2015';

    //#region Grid

    var calendarChange = function (event) {
        var dt = moment(event.args.date);
        date = dt.toISOString();
        loadStatus();
    }

    var loadStatus = function () {
        $.getJSON('/internal/monitor/PolicyStatusForDate', { id: id, date: date }, function (data) {
            var html = '';

            if (data.status) {
                html = "<i class='fa fa-thumbs-o-up' style='font-size: 40px; color: green' title='All good'></i>";
            }
            else {
                html = "<i class='fa fa-thumbs-o-down' style='font-size: 40px; color: red' title='Not so good'></i>";
            }

            $(graphicControlID).html(html);
        });
    }

    try {
        $(controlID).html('<header>Health Status</header><div style="padding: 10px"><table><tr style="vertical-align: middle"><td><div id="' + calendarControlID + '"></div></td><td><div id="' + graphicControlID + '"></div></td></tr></table></div>');
        calendarControlID = '#' + calendarControlID;
        graphicControlID = '#' + graphicControlID;

        $(calendarControlID).jqxDateTimeInput({ width: '220px', height: '25px', theme: theme, formatString: "MM-dd-yy" });
        loadStatus();
    } catch (e) {
        console.log(e);
    }

    //#endregion

    //#region Event Subscriptions

    function unsubscribe(data) {
        $(calendarControlID).off('change', calendarChange);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    $(calendarControlID).on('change', calendarChange);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}