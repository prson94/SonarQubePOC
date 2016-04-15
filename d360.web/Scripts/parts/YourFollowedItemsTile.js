function YourFollowedItemsTile(controlID, resourceID, title) {
        
    var chartsExist;
    var parent;    
    var gridControlID = controlID + "_grid";

    try {        
        parent = $('#' + controlID);

        var html = "";
        html += "<header>" + title +"</header>";
        html += "<div style='margin-left: 5px' id='" + gridControlID + "'></div>";

        gridControlID = '#' + gridControlID;

        parent.html(html);
        
        var clickKpiTitle = function () {
            var kpi = $(this);            
            var url = '/parts/Following?resourceID=' + resourceID + '&type=' + kpi.data("t") + '&id=' + kpi.data("i");
            openTileOverlay(url);
        }

        $.ajax({
            url: '/tiles/FollowingBreakdownByResource',
            method: 'GET',
            data: {
                id: resourceID
            },
            dataType: 'json'
        }).fail(function (xhr, status, error) {

        }).done(function (data, status, xhr) {            
            var collectionHtml = '<div class="kpi-grid">';
            $.each(data, function () {
                collectionHtml += '<div class="kpi-grid-item" style="background-color: ' + this.IconBackColor + '; color: ' + this.IconForeColor + '" data-t="' + this.Type + '" data-i="' + this.TypeID + '">' +
                        '<div class="icon">' + this.IconText + '</div>' +
                        '<div class="value">' + this.Count + '</div>' +
                        '<div class="title">' + this.TypeName + '</div>' +
                        '</div>';
            });

            collectionHtml += '</div>';
            
            $(gridControlID).html(collectionHtml);
            $('.kpi-grid').isotope({
                // options
                itemSelector: '.kpi-grid-item',
                layoutMode: 'fitRows',
                fitRows: {
                    gutter: 10
                }
            });
            $('#' + controlID + ' .kpi-grid-item').on('click', clickKpiTitle);
        });

    } catch (e) {
        console.log(e);
    }
}