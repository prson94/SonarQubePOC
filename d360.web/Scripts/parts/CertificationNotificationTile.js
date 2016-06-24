function CertificationNotificationTile(controlID, id) {
    controlID = '#' + controlID;
    var buttonControlID = controlID + "_button";

    var getWorkflowForItem = function () {
        $.getJSON('/workflow/CertificationNotification?id=' + id, function (data) {

            $(controlID).html('');

            if (data.WorkflowID) {
                $(controlID).append('<article>');
                $(controlID).append('<header>Certification Is Due</header>');
                $(controlID).append('<div style="text-align: center; margin-bottom: 15px">You need to certify this item.</div>');
                $(controlID).append('<div style="text-align: center; margin-bottom: 15px"><button id="' + buttonControlID + '" type="button" class="btn btn-success" onclick="ClickGridTool(event)" data-context="Workflow" data-uri="/workflow/' + data.WorkflowID + '/overlay/true">Certify Now!</button></div>');
                $(controlID).append('</article>');
                $(controlID).fadeIn(250);
            }
            else {
                $(controlID).fadeOut(250);
            }

            //$('#' + buttonControlID).on('click', function () {
            //    //'workflow/' + data.WorkflowID + '/overlay'
            //});
        });
    }

    getWorkflowForItem();

    function saveAction(data) {
        try {
            switch (data.context) {
                case "Workflow":
                case "RequestCertification":
                    getWorkflowForItem();
                    break;
            }
        } catch (e) { }
    }

    function unsubscribe(data) {
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

}
