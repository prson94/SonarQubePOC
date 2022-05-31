import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-responsibility-selector',
    providers: [ResponsibilityTypeService, WorkflowService],
    template: `
         <ng-container *ngIf="objectType == 'IntersectType'">
            <div class="FieldName" i18n>
                Responsible Party
            </div>
            <div>
                <select [ngModel]="step.settings.ResponsibilitySide" (ngModelChange)="changeResponsibilitySide($event)" style="width: 95%">
                    <option></option>
                    <option value="Object">Object</option>
                    <option value="Subject">Subject</option>
                </select>
            </div>
        </ng-container>
        <ng-container *ngIf="((step.settings.ResponsibilitySide != null && step.settings.ResponsibilitySide != '' && objectType == 'IntersectType') || objectType != 'IntersectType') && !isLoading">
            <div class="FieldName" i18n>
                Responsibility
            </div>
            <div *ngFor="let x of step.settings.ResponsibilityTypeID; let i = index; trackBy: trackRes">
                <select [ngModel]="step.settings.ResponsibilityTypeID[i]" (ngModelChange)="changeResponsibility($event, i)" [style.width]="step.settings.ResponsibilityTypeID.length > 1 ? '90%' : '95%'">
                    <option></option>
                    <option *ngFor="let r of responsibilities" [value]="r.ResponsibilityTypeID">{{r.Name}}</option>
                </select>
                <div *ngIf="step.settings.ResponsibilityTypeID != null && step.settings.ResponsibilityTypeID.length > 1" style="display: inline-block; font-size: 1.5em; padding: 0 0 10px 10px;">
                    <a style="cursor: pointer;" (click)="removeResponsibility(i)"><i class="fa fa-trash"></i></a>
                </div>
            </div>
            <div>
                <a style="cursor: pointer;" (click)="addResponsibility()"><i class="fa fa-plus"></i> <ng-container i18n>Add a backup responsibility</ng-container></a>
            </div>
        </ng-container>       
`
})

export class WorkflowResponsibilitySelectorComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() step: any;
    @Output() stepChange = new EventEmitter();

    isLoading = false;
    private responsibilities: any[] = [];
    private intersectType = null;
    private responsibleObject = null;
    private responsibleObjectId = null;

    constructor(private workflowService: WorkflowService, private responsibilityService: ResponsibilityTypeService) {
    }

    ngOnInit() {
        if (this.objectType != 'IntersectType') {
            this.responsibleObject = this.objectType;
            this.responsibleObjectId = this.objectId;
        }
        this.getResponsibilityTypes();
        this.initFields();


    }

    initFields() {
        if (this.step.settings.MessageRecipientType != null && this.step.settings.MessageRecipientType == 'Responsibility') {
            if (this.objectType == 'IntersectType') {
                this.changeResponsibilitySide(this.step.settings.ResponsibilitySide || 'Subject');
            } else {
                this.responsibleObject = this.objectType;
                this.responsibleObjectId = this.objectId;
                this.getResponsibilityTypes();
            }
        }

        //convert single value to array
        if (this.step.settings.ResponsibilityTypeID != null && !_.isArray(this.step.settings.ResponsibilityTypeID)) {
            let id = this.step.settings.ResponsibilityTypeID;
            delete this.step.settings.ResponsibilityTypeID;
            this.step.settings.ResponsibilityTypeID = [];
            this.step.settings.ResponsibilityTypeID.push(id);
        } else if (this.step.settings.ResponsibilityTypeID == null) {
            this.step.settings.ResponsibilityTypeID = [];
            this.step.settings.ResponsibilityTypeID.push(null);
        }
    }

    addResponsibility() {
        this.step.settings.ResponsibilityTypeID.push(null);
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step);
    }

    removeResponsibility(i: number) {
        this.step.settings.ResponsibilityTypeID.splice(i, 1);
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step);
    }

    changeResponsibilitySide(e: any) {
        //if we switch sides, clear the current values
        if (e != this.step.settings.ResponsibilitySide) {
            this.step.settings.ResponsibilityTypeID = [];
            this.addResponsibility();
        }

        this.step.settings.ResponsibilitySide = e;
        //console.log('changeResponsibilitySide', this.step, e, this.intersectType, this.responsibleObject, this.responsibleObjectId);
        let promises = [];
        this.isLoading = true;

        if (this.intersectType == null)
            promises.push(this.workflowService.getIntersectType(this.objectId).subscribe(r => {
                if (r == null || r.length < 1) {
                    this.intersectType = null;
                } else {
                    this.intersectType = r[0];
                }
                //console.log('changeResSide after inttype', this.intersectType);
            }));
        else
            promises.push(Promise.resolve());

        Promise.all(promises)
            .then(() => {
                if (this.intersectType == null || (e != 'Object' && e != 'Subject')) {
                    this.responsibleObjectId = null;
                    this.responsibleObject = null;
                    this.responsibilities = [];
                } else if (e == 'Object') {
                    this.responsibleObject = this.intersectType.Object;
                    this.responsibleObjectId = this.intersectType.ObjectID;
                } else if (e == 'Subject') {
                    this.responsibleObject = this.intersectType.Subject;
                    this.responsibleObjectId = this.intersectType.SubjectID;
                }
                //console.log('changeResSide after promises', this.intersectType, this.responsibleObject, this.responsibleObjectId, e);
            })
            .then(() => this.getResponsibilityTypes())
            .then(() => this.stepChange.emit(this.step))
            .then(() => this.isLoading = false);
    }

    getResponsibilityTypes() {
        //console.log('getResTypes', this.responsibleObject, this.responsibleObjectId);
        if (this.responsibleObject == null || this.responsibleObjectId == null || this.responsibleObjectId < 0 || this.objectType == 'IssueType') {
            this.responsibilities = [];
            return this.responsibilityService.getResponsibilityTypes()
                .subscribe(r => {
                    this.responsibilities = r;
                    this.responsibilities.forEach(r => {
                        r.ResponsibilityTypeID = r.ID;
                    })
                });
        }

        return this.responsibilityService.getResponsibilityTypesByObject(this.responsibleObject, this.responsibleObjectId)
            .subscribe(r => this.responsibilities = r);
    }

    changeResponsibility(e: any, i: number) {
        //console.log('changeResponsibility', e, i, this.responsibilities);
        this.step.settings.ResponsibilityTypeID[i] = e;
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step)
    }

    trackRes(index, item) {
        //not sure why this is required, but without it Angular has trouble keeping track of the index based responsibility types
        return index;
    }

}