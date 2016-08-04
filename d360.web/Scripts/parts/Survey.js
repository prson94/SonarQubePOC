function Survey(controlID, type, id, parentType, parentTypeId) {    
    var surveyVm;
        
    surveyVm = new SurveyViewModel(type, id, parentType, parentTypeId);
    try {        
        ko.applyBindings(surveyVm, document.getElementById(controlID));
    }
    catch (e) {
        console.log(e);
    }

    //region Event Handlers

    function unsubscribe(data) {        
        surveyVm = null;
        amplify.unsubscribe(AmplifyActions.Unsubscribe, unsubscribe);
    }

    //#endregion
    
    //#region Event Registration

    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion
}