function CollapsibleTypeHierarchyTile(controlID, contextList, permissions, type, id) {
    var controlID_sub = controlID + '_Sub';

    //#region Event Handlers

    function expanded() {
        HierarchyTile(controlID_sub, contextList, permissions, type, id, 3);
    }

    function unsubscribe(data) {
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        $('#' + controlID).off('expanded', expanded);
    }

    //#endregion

    //#region Clean up previous control logic before re-creating

    try { unsubscribe({}); } catch (e) { }
    var exists = false;
    try {
        var exp = $('#' + controlID).jqxExpander('animationType');
        if (exp) {
            exists = true;
        }
    } catch (e) { }

    //#endregion

    if (!exists) {
        $('#' + controlID).css('margin', '10px');
        $('#' + controlID).html('<div>Structure</div><div style="min-height: 150px"><div style="width:99%" id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false });
    }

    HierarchyTile(controlID_sub, contextList, permissions, type, id, 3);

    //#region Register Events

    $('#' + controlID).on('expanded', expanded);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
}