function FusionAttributeDetailTile(controlID, type, id) {
    var detailControlID = controlID + "_fus_det";
    controlID = '#' + controlID;
    $(controlID).hide();

    $.ajax({
        url: '/fusion/details/' + type + '/' + id,
        method: 'GET'
    })
    .done(function (data, status, xhr) {
        if (data.Fields.length > 0) {
            $(controlID).html('<header>Details</header><div id="' + detailControlID + '" style="margin: auto; width: 95%;" class="form"></div>');
            detailControlID = '#' + detailControlID;
            var itemCnt = 0;
            var ended = false;

            var row = $("<div class='row'>");
            $(detailControlID).append(row);

            var col = $("<div class='col l6 m6'>");
            $(row).append(col);

            col.append("<div class='FieldName FieldDisplayName'>Name</div>");
            col.append("<div class='FieldContent wrapword'>" + data.Name + "</div>");

            col = $("<div class='col l6 m6'>");
            $(row).append(col);
            col.append("<div class='FieldName FieldDisplayName'>Path</div>");
            col.append("<div class='FieldContent wrapword'>" + data.TextPath + "</div>");

            row = $("<div class='row'>");
            $(detailControlID).append(row);

            data.Fields.forEach(function (item) {
                if (itemCnt % 2 == 0 && itemCnt > 0) {
                    row = $("<div class='row'>");
                    $(detailControlID).append(row);
                }
                col = $("<div class='col l6 m6'>");
                $(row).append(col);
                col.append("<div class='FieldName FieldDisplayName'>" + item.Name + "</div>");
                col.append("<div class='FieldContent wrapword'>" + item.Value + "</div>");
            });

            $(controlID).show();
        }
    });
}