function ChallengeNotificationTile(controlID, contextList, id) {
    controlID = '#' + controlID;
    var buttonControlID = controlID + "_button";

    var getWorkflowForItem = function () {
        $.getJSON('/workflow/ChallengeNotification?id=' + id, function (data) {

            $(controlID).html('');

            if (data.WorkflowID) {
                
                var challengeContent = '<article>';
                challengeContent += '<header><i class="fa fa-warning error"></i> Outstanding Challenge</header>';
                challengeContent += '<div class="row" style="padding:2px">';
                challengeContent += '<div class="col s3 FieldName">Challenger</div>';
                challengeContent += '<div class="col s9"><a data-context="Preview" data-type="Resource" data-id="' + data.ResourceID + '" href="' + data.ResourceUrl + '">' + data.ResourceName + '</a></div>';
                challengeContent += '</div><div class="row" style="padding:2px">';
                challengeContent += '<div class="col s3 FieldName">Issued</div>';
                challengeContent += '<div class="col s9">' +(moment.utc(data.DateStarted).local().format('dddd, MMMM Do YYYY, h:mm:ss a')) + '</div>';
                challengeContent += '</div><div class="row" style="padding:2px">';
                challengeContent += '<div class="col s3 FieldName">Reason</div>';
                challengeContent += '<div class="col s9 imageWrapper FieldDisplayContent">' + data.Reason + '</div>';                
                challengeContent += '</div>';
                challengeContent += '<div class="row"><div class="col s12"><div id="ChallengConvoTile"></div></div></div>'
                //challengeContent += '<div class="row"><div class="col s12"><div title="Status" class="tile-clickable" data-tile data-uri="/workflow/' + data.WorkflowID + '/status" data-context="overlayContext">Status</div></div>';
                challengeContent += '</article>';

                $(controlID).append(challengeContent);

                CollapsibleConversationTile('ChallengConvoTile', contextList, data.CommentID);

                //if the current user can approve deny show the buttons 
                if (data.AssignedResourceID > 0) {
                    $(controlID).append('<div class="row"><div class="col s12 FieldName">Your Response</div></div><div class="row" id="challenge-actions" style="padding:10px"><div id="ActionArea" class="row"><div class="col s6" id="Action1Wrapper"><input type="radio" id="Action1" name="Action" value="accept" checked="checked" /><label for="Action1">Confirm</label></div><div class="col s6" id="Action3Wrapper"><input type="radio" id="Action3" name="Action" value="close" /><label for="Action3">Close</label></div></div><div id="CommentArea"><div class="FieldName">Comment</div><textarea id="Comment"></textarea></div><div></div><button type="button" id="SaveButton" class="btn waves-effect waves-light brown lighten-1 saveButton right">Save</button></div>');

                    $('#SaveButton').on('click', function () {
                        $(this).val('Please wait ...').attr('disabled', 'disabled');
                        $.ajax('/services/workflow/tasks/' + data.WorkflowID, {
                            dataType: 'json',
                            contentType: "application/json; charset=utf-8",
                            type: 'POST',
                            data: JSON.stringify({
                                Approved: $('input[name=Action]:checked', '#ActionArea').val() == 'accept',
                                Notes: $('#Comment').val()
                            })
                        }).fail(function (xhr, status, error) {
                            var response = {
                                type: 'error',
                                title: 'Error occured',
                                message: error,
                                context: 'Workflow'
                            };
                            OnFailed(response, status, xhr);
                        })
                          .done(function (data, status, xhr) {
                              OnSuccess(data, status, xhr);
                              $(controlID).hide();
                          }).always(function () {
                              $('#challenge-actions').html('');
                          });
                    });
                }

                
                $(controlID).fadeIn(250);
            }
            else {
                $(controlID).fadeOut(250);
            }

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
        $('#SaveButton').off('click');
    }

    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

}
