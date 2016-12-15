import { Input, Component, EventEmitter, Output, OnInit, } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { QualifierService, MessagesService } from '../../services/index';
import { QualifierType, ResolutionObjectType } from '../../models/qualifier.model';

@Component({
    selector: 'd3s-rule-qualifier-editor',
    template: ` 
                <div>
                    <header *ngIf="isAdding">Add a Qualifier</header>
                    <header *ngIf="!isAdding">Edit Qualifier</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <form *ngIf="!isLoading" #qualifierForm="ngForm" (ngSubmit)="save()">
                        <div class="row">
                            <div class="col s4 offset-s4">
                                <div class="FieldName" style="display:block;">Name</div>
                                <input type="text" [(ngModel)]="qualifier.Name" name="name" required style="width: 95%"/>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s4 offset-s4">
                                <div class="FieldName" style="display:block;">Resolution Object</div>
                                <select name="resObject" [(ngModel)]="resolutionObject" style="width:95%" (ngModelChange)="changeResolutionObject()">
                                    <option value="">Please choose...</option>
                                    <option *ngFor="let i of resolutionObjects" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="row" *ngIf="resolutionObject != ''">
                            <div class="col s4 offset-s4">
                                <div class="FieldName" style="display:block;">Resolution Field</div>
                                <select name="resType" [(ngModel)]="qualifier.ResolutionFieldTypeID" style="width:95%" required>
                                    <option *ngFor="let i of resolutionFields" [value]="i.ID">{{i.FriendlyName}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="row" style="margin-top: 10px">
                            <div class="col s12">
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
    @Input() ruleID: number = null;
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
            this.qualifier.RuleID = this.ruleID;
        }
        this.loadResolutionObjects()
            .then(() => this.loadResolutionFields())
            .then(() => {
                this.isLoading = false;
            });
    }

    private loadResolutionObjects(): Promise<any> {
        return this.qualifierService.getQualifierResolutionObjects()
            .then(r => {
                this.resolutionObjects = r;
                if (this.qualifier && this.qualifier.ResolutionObject != null && this.qualifier.ResolutionObjectID != null) {
                    this.resolutionObject = this.qualifier.ResolutionObject + '|' + this.qualifier.ResolutionObjectID.toString();
                }
            });
    }

    private loadResolutionFields(): Promise<any> {
        if (this.resolutionObject.indexOf('|') == -1) {
            this.resolutionFields = [];
            this.qualifier.ResolutionFieldTypeID = null;
            return Promise.resolve();
        }
        else    
            return this.qualifierService.getQualifierResolutionFields(+this.resolutionObject.split('|')[1], this.resolutionObject.split('|')[0])
                .then(r => {
                    this.resolutionFields = r;
                    this.resolutionFields.push({ ID: 0, FriendlyName: 'Name' });
                    this.resolutionFields.push({ ID: -2, FriendlyName: 'ParentID' });
                });
    }

    private changeResolutionObject() {
        this.qualifier.ResolutionFieldTypeID = null;
        this.loadResolutionFields();
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
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit();
                });
        } else {
            this.qualifierService.putEditRuleQualifierType(this.qualifier)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit();
                });
        }
    }
}