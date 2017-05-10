import { Component, EventEmitter, Output, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionRule } from '../../../models/fusion.model';

export class FusioRuleStepBaseComponent extends BaseComponent {    
    protected rule: FusionRule;

    //This is generally overloaded to show hide in your own class.
    protected removeIrrelevantSettings(settings: any, action: string) {
        switch (action) {
            case "Find":
                //#region
                delete settings.IntersectType;
                delete settings.Search;
                delete settings.ID;
                delete settings.SubjectSearch;
                delete settings.Subject;
                delete settings.SubjectID;
                delete settings.TechnicalSubjectSearch;
                delete settings.TechnicalSubject;
                delete settings.TechnicalSubjectID;
                delete settings.TechnicalObjectSearch;
                delete settings.TechnicalObject;
                delete settings.TechnicalObjectID;
                delete settings.Role;
                delete settings.ParentObjectSearch;
                delete settings.ParentObject;
                delete settings.ParentObjectID;
                break;
                //#endregion
            case "FindRelation":
                //#region
                delete settings.FilterField;
                delete settings.TargetField;
                delete settings.FindParent;
                delete settings.SubjectSearch;
                delete settings.Subject;
                delete settings.SubjectID;
                delete settings.ObjectSearch;
                delete settings.Object;
                delete settings.ObjectID;
                delete settings.TechnicalSubjectSearch;
                delete settings.TechnicalSubject;
                delete settings.TechnicalSubjectID;
                delete settings.TechnicalObjectSearch;
                delete settings.TechnicalObject;
                delete settings.TechnicalObjectID;
                delete settings.Role;
                delete settings.Object;
                delete settings.ObjectID;
                delete settings.ParentObjectSearch;
                delete settings.ParentObject;
                delete settings.ParentObjectID;
                break;
                //#endregion
            case "Lineage":
                //#region
                delete settings.FilterField;
                delete settings.TargetField;
                delete settings.FindParent;
                delete settings.IntersectType;
                delete settings.SubjectSearch;
                delete settings.Subject;
                delete settings.ObjectSearch;
                delete settings.Object;
                delete settings.Search;
                delete settings.ID;
                delete settings.ParentObjectSearch;
                delete settings.ParentObject;
                delete settings.ParentObjectID;
                break;
                //#endregion
            case "Promote":
                //#region
                delete settings.FilterField;
                delete settings.TargetField;
                delete settings.FindParent;
                delete settings.IntersectType;
                delete settings.Search;
                delete settings.ID;
                delete settings.SubjectSearch;
                delete settings.Subject;
                delete settings.SubjectID;
                delete settings.ObjectSearch;
                delete settings.TechnicalSubjectSearch;
                delete settings.TechnicalSubject;
                delete settings.TechnicalSubjectID;
                delete settings.TechnicalObjectSearch;
                delete settings.TechnicalObject;
                delete settings.TechnicalObjectID;
                delete settings.Role;
                break;
                //#endregion
            case "Relate":
                //#region
                delete settings.FilterField;
                delete settings.TargetField;
                delete settings.FindParent;
                delete settings.Search;
                delete settings.ID;
                delete settings.TechnicalSubjectSearch;
                delete settings.TechnicalSubject;
                delete settings.TechnicalSubjectID;
                delete settings.TechnicalObjectSearch;
                delete settings.TechnicalObject;
                delete settings.TechnicalObjectID;
                delete settings.Role;
                delete settings.ParentObjectSearch;
                delete settings.ParentObject;
                delete settings.ParentObjectID;
                break;
            case "Update":
                delete settings.FilterField;
                delete settings.FilterField;
                delete settings.FindParent;
                delete settings.ID;
                delete settings.IntersectType;
                delete settings.Object;
                delete settings.ObjectID;
                delete settings.ObjectSearch;
                delete settings.ParentObject;
                delete settings.ParentObjectID;
                delete settings.ParentObjectSearch;
                delete settings.Role;
                delete settings.Search;
                delete settings.Subject;
                //delete settings.SubjectID;
                delete settings.SubjectSearch;
                delete settings.TargetField;
                delete settings.TechnicalObject;
                delete settings.TechnicalObjectID;
                delete settings.TechnicalObjectSearch;
                delete settings.TechnicalSubject;
                delete settings.TechnicalSubjectID;
                delete settings.TechnicalSubjectSearch;
                break;
                //#endregion
        }
    }
}