///<reference path="../../es6-shim.d.ts"/>
import {Input, Output, Component, OnInit, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { SelectItem, FormMessage } from '../../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';
import { CompanySettings as cs } from '../../models/company-settings.model';
import { Dropdown, Calendar, Checkbox, Button } from 'primeng/primeng';
import * as _ from 'lodash';


//will use CompanySettings js object globally declared on page
//not sure if this will cause collisions in other components yet
declare var CompanySettings: cs;

@Component({
    selector: 'workflow-item-form',
    templateUrl: 'scripts/app/components/forms/workflow-item.form.html',
    viewProviders: [ HTTP_PROVIDERS ],
    directives: [FormMessagePart, Dropdown, Calendar, Checkbox, Button ]
})

export class WorkflowItemForm implements OnInit {
    @Input() item: WorkflowItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();


    private ObjectTypes = new Array<SelectItem>();
    private ParentTypes = new Array<SelectItem>();
    private ResponsibilityTypes = new Array<SelectItem>();
    private message: FormMessage = new FormMessage();
    private initialItem: WorkflowItem;

    private ObjectType: string;
    private ParentType: string;
    private ResponsibilityType: string;

    private workflowType = WorkflowType;
    

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }


    private numDays: number = 14;
    private numMonths: number = 12;
    private dateScheduleCalculation: string;

    private isLoading = false;
    private isSaving = false;
    private isLoadingResponsibility = false;
    
    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.load();
        this.initialItem = _.cloneDeep(this.item);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'item') {
                this.initialItem = _.cloneDeep(this.item);
                this.load();
            }
        }
    }

    private load(): void {

        if (this.item == null)
            return;
        this.isLoading = true;

        this.ObjectType = this.item.Object + '|' + this.item.ObjectID;
        this.ParentType = this.item.Parent + '|' + this.item.ParentID;

        //console.log(this.item.ID);
        //console.log(this.item.WorkflowType);

        this.http.get(`form/WorkflowAllocation?id=${this.item.ID}&workflowType=${this.item.WorkflowType}`)
            .map(data => data.json())
            .subscribe(data => {

                this.ObjectTypes = data.ObjectTypes;
                this.ObjectTypes.map(o => { o.label = o.Text; o.value = o.Value });
                this.ParentTypes = data.ParentTypes;
                this.ParentTypes.map(o => { o.label = o.Text; o.value = o.Value });
                this.ResponsibilityTypes = data.ResponsibilityTypes
                this.ResponsibilityTypes.map(o => { o.label = o.Text; o.value = o.Value });

                this.numDays = data.WorkflowTypeRelation.Fields.DaysGivenToCompleteCertification || this.numDays;
                this.numMonths = data.WorkflowTypeRelation.Fields.MonthsUntilCertification || this.numMonths;
                this.dateScheduleCalculation = data.WorkflowTypeRelation.Fields.DateForScheduleCalculation || this.dateScheduleCalculation;

                //console.log(this.item);
                //console.log(data);

                this.isLoading = false;
                this.onLoadComplete.emit({ item: this.item });
            });
    }

    private save(): void {

        this.isSaving = true;

        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        try {
            this.item.Object = this.ObjectType.split('|')[0];
            this.item.ObjectID = parseInt(this.ObjectType.split('|')[1]);
            this.item.Parent = this.ParentType.split('|')[0];
            this.item.ParentID = parseInt(this.ParentType.split('|')[1]);
        } catch (exception) {
            this.isSaving = false;
            this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            return;
        }

        this.item.Fields = [];
        this.item.Fields.push({ key: 'DateForScheduleCalculation', value: this.dateScheduleCalculation });
        this.item.Fields.push({ key: 'DaysGivenToCompleteCertification', value: this.numDays });
        this.item.Fields.push({ key: 'MonthsUntilCertification', value: this.numMonths });

        console.log(this.item);

        this.http.post('form/WorkflowAllocation', JSON.stringify(this.item), { headers: headers })
            .map(data => data.json())
            .subscribe(data => {
                this.isSaving = false;
                this.message.Success("Save completed successfully.");
                this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
        });
    }

    private cancel(): void {
        this.onCancel.emit({ initialItem: this.initialItem });
    }

    private objectTypeChange(val: any) {
        this.isLoadingResponsibility = true;

        val = val.value;

        var obj;
        var id;

        try {
            obj = val.split('|')[0];
            id = val.split('|')[1];
        } catch (exception) {
            this.isLoadingResponsibility = false;
            return;
        }

        this.http.get(`/workflow/WorkflowResponsibilityTypeOptions?type=${obj}&id=${id}`)
            .map(data => data.json())
            .subscribe(data => {
                //console.log(data);
                this.ResponsibilityType = null;
                this.ResponsibilityTypes = data;
                this.ResponsibilityTypes.map(r => { r.label = r.Text; r.value = r.Value }); 
                this.isLoadingResponsibility = false; 
            });

        this.http.get(`/workflow/WorkflowParentTypeOptions?workflowType=${this.item.WorkflowType}&type=${obj}&id=${id}`)
            .map(data => data.json())
            .subscribe(data => {
                this.ParentTypes = data;
                this.ParentTypes.map(p => { p.label = p.Text; p.value = p.Value });
            });
    }

}
