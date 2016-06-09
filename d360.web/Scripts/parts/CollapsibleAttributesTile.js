function CollapsibleAttributesTile(controlID, contextList, permissions, type, id) {
    var controlID_count = controlID + '_Count';
    var controlID_sub = controlID + '_Sub';

    //#region Event Handlers

    function attributeCountNotice(data) {
        $('#' + controlID_count).html("&#160;(<b>" + data.count + "</b>)");
    }

    function expanded() {
        AttributesTile(controlID_sub, contextList, permissions, type, id, '', false);
    }

    function unsubscribe(data) {
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
        amplify.unsubscribe(AmplifyActions.TileUnsubscribe, unsubscribe);
        amplify.unsubscribe("AttributeCount", attributeCountNotice);
        //$('#' + controlID).off('expanded', expanded);
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
    try { unsubscribe({}); } catch (e) { }

    //#endregion


    function initAttributes(){
        AttributesTile(controlID_sub, contextList, permissions, type, id, '', false);
    }
    if (!exists) {
        $('#' + controlID).css('margin', '10px');
        $('#' + controlID).html('<div>Attributes<span id="' + controlID_count + '"></span></div><div style="min-height: 150px"><div id="' + controlID_sub + '"></div></div>');
        $('#' + controlID).jqxExpander({ theme: theme, expanded: false, initContent: initAttributes });
    }
    //AttributesTile(controlID_sub, contextList, permissions, type, id, '', false);

    //#region Register Events

    amplify.subscribe("AttributeCount", attributeCountNotice);
    //$('#' + controlID).on('expanded', expanded);
    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);
    amplify.subscribe(AmplifyActions.TileUnsubscribe, unsubscribe);

    //#endregion
}