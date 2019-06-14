import { Input, Component, EventEmitter, Output, OnInit, } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { QualifierService } from '../../services/qualifier.service';
import { MessagesService } from '../../services/messages.service';
import { QualifierType, ResolutionObjectType } from '../../models/qualifier.model';
import { Subject, Observable } from 'rxjs';

@Component({
    selector: 'd3s-rule-qualifier-editor',
    template: ` 
                <div>
                    <header *ngIf="isAdding">Add a Qualifier</header>
                    <header *ngIf="!isAdding">Edit Qualifier</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <form *ngIf="!isLoading" #qualifierForm="ngForm" (ngSubmit)="save()">
                        <div class="row">
                            <div class="col s12 m6 l4">
                                <div class="FieldName" style="display:block;">Name</div>
                                <input type="text" [(ngModel)]="qualifier.Name" name="name" required style="width: 95%" (keyup)="updateQualifierName($event)"/>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12 m6 l4">
                                <div class="FieldName" style="display:block;">Resolution Object</div>
                                <select name="resObject" [(ngModel)]="resolutionObject" style="width:95%" (ngModelChange)="changeResolutionObject()">
                                    <option value="">Please choose...</option>
                                    <option *ngFor="let i of resolutionObjects" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="row" *ngIf="resolutionObject != ''">
                            <div class="col s12 m6 l4">
                                <div class="FieldName" style="display:block;">Resolution Field</div>
                                <select name="resType" [(ngModel)]="qualifier.ResolutionFieldTypeID" style="width:95%" required>
                                    <option></option>
                                    <option *ngFor="let i of resolutionFields" [value]="i.ID">{{i.FriendlyName}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="row" style="margin-top: 10px">
                            <div class="col s12 m6 l4">
                                <button pButton type="submit" [disabled]="isLoading || !qualifierForm.form.valid" label="Save"></button>
                                <button pButton type="button" label="Close" (click)="onClose.emit()"></button>
                            </div>
                        </div>
                    </form>
                </div>

          `,
    providers: [QualifierService],
})

export class RuleQualifierEditorComponent extends BaseComponent implements OnInit {
    @Input() qualifier: QualifierType = null;
    @Input() implementationId: number = null;
    @Output() onClose  = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private isAdding: boolean = false;
    private resolutionObject: string = '';
    private resolutionObjects: ResolutionObjectType[] = [];

    private resolutionFields = [];


    constructor(private qualifierService: QualifierService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        if (this.qualifier == null)
            this.isAdding = true;
        this.load();
    }

    private load() {
        this.isLoading = true;
        if (this.isAdding) {
            this.qualifier = new QualifierType();
            this.qualifier.RuleImplementationID = this.implementationId;
        }
        return this.qualifierService.getQualifierResolutionObjects()
            .subscribe(r => {
                this.resolutionObjects = r;
                if (this.qualifier && this.qualifier.ResolutionObject != null && this.qualifier.ResolutionObjectID != null) {
                    this.resolutionObject = this.qualifier.ResolutionObject + '|' + this.qualifier.ResolutionObjectID.toString();
                }
                this.loadResolutionFields();
                this.isLoading = false;
            });
        
    }
    
    private loadResolutionFields(){
        if (this.resolutionObject.indexOf('|') == -1) {
            this.resolutionFields = [];
            this.qualifier.ResolutionFieldTypeID = null;
            return Observable.create();
        }
        else    
            this.qualifierService.getQualifierResolutionFields(+this.resolutionObject.split('|')[1], this.resolutionObject.split('|')[0])
                .subscribe(r => {
                    this.resolutionFields = r;
                    this.resolutionFields.push({ ID: 0, FriendlyName: 'Name' });
                    this.resolutionFields.push({ ID: -2, FriendlyName: 'ParentID' });
            });
    }

    private changeResolutionObject() {
        this.qualifier.ResolutionFieldTypeID = null;
        this.loadResolutionFields();
    }

    private updateQualifierName(event) {
        this.qualifier.Name = event.target.value.replace(/[^a-zA-Z0-9_-\s]/g, '');
    }

    private save() {
        if (this.resolutionObject.indexOf('|') != -1) {
            this.qualifier.ResolutionObject = this.resolutionObject.split('|')[0];
            this.qualifier.ResolutionObjectID = +this.resolutionObject.split('|')[1];
        } else {
            this.qualifier.ResolutionObject = null;
            this.qualifier.ResolutionObjectID = null;
        }

        if (this.qualifier.ResolutionFieldTypeID != null) {
            let field = this.resolutionFields.find(f => f.ID == this.qualifier.ResolutionFieldTypeID);
            if (field != null)
                this.qualifier.ResolutionFieldTypeName = field.FriendlyName;
        }

        if (this.qualifier.ID == null || this.qualifier.ID < 1) {
            this.qualifierService.postAddRuleQualifierType(this.qualifier)
                .subscribe(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit();
                });
        } else {
            this.qualifierService.putEditRuleQualifierType(this.qualifier)
                .subscribe(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit();
                });
        }
    }
}