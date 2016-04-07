function CollapsibleConversationTile(controlID, contextList, commentid) {
    //var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';    
    var CommentsOverlaySocial = null;

    //#region Event Handlers
        
    function saveAction(data) {
       
    }

    function expanded() {
        if (CommentsOverlaySocial == null) {
            CommentsOverlaySocial = new BoardViewModel();
            CommentsOverlaySocial.ShowDateFilter = false;
            CommentsOverlaySocial.ShowTypeFilter = false;
            CommentsOverlaySocial.ShowSearch = false;
            CommentsOverlaySocial.ShowAddCommentControls = false;
            ko.applyBindings(CommentsOverlaySocial, document.getElementById(controlID_sub));            
            CommentsOverlaySocial.changeObject('Comment', commentid);
        }
    }

    function unsubscribe(data) {
        CommentsOverlaySocial = null;
                
        amplify.unsubscribe("SaveAction", saveAction);
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        $('#' + controlID).off('expanded', expanded);
    }

    //#endregion

    //#region Clean up previous control logic before re-creating
        

    var exists = false;
    try {
        var exp = $('#' + controlID).jqxExpander('animationType');
        if (exp) {
            exists = true;
        }
    } catch (e) { }

    //#endregion
    if (!exists) {            
        $('#' + controlID).html('<div>Conversation</div><div><div id="' + controlID_sub + '"  data-bind="template: {name: \'boardTmpl\' }"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });
    }
        
    //#region Register Events
        
    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe("SaveAction", saveAction);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
}