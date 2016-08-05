var Survey = function(controlID, type, id, parentType, parentTypeId) {
    var self = this;
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

    self.ChangeObject = function (type, id, parentType, parentTypeId) {
        surveyVm.Clear();
        surveyVm.Type(type);
        surveyVm.Id(id);
        surveyVm.ParentType(parentType);
        surveyVm.ParentTypeId(parentTypeId);

        surveyVm.Load();
    }

    //#endregion
    
    //#region Event Registration

    amplify.subscribe(AmplifyActions.Unsubscribe, unsubscribe);

    //#endregion

    return self;
}