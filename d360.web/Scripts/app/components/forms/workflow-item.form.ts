///<reference path="../../es6-shim.d.ts"/>
import {Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { SelectItem, FormMessage } from '../../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';
import { CompanySettings as cs } from '../../models/company-settings.model';
import { Dropdown, Calendar, Checkbox, Button } from 'primeng/primeng';


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
    private dateScheduleCalculation: string; // = new Date().toISOString().slice(0, 16);

    private isLoading = false;
    private isSaving = false;
    private isLoadingResponsibility = false;
    
    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.load();
        this.initialItem = JSON.parse(JSON.stringify(this.item));
        //console.log(this.dateScheduleCalculation);
        //console.log($.isArray([]));
        //$.datepicker
    }

    private load(): void {

        if (this.item == null)
            return;
        this.isLoading = true;

        this.ObjectType = this.item.Object + '|' + this.item.ObjectID;
        this.ParentType = this.item.Parent + '|' + this.item.ParentID;

        this.http.get(`form/EditWorkflowAllocationEditor?id=${this.item.ID}`)
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
            this.revert();
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            return;
        }

        this.http.post('form/EditWorkflowAllocationEditor', JSON.stringify(this.item), { headers: headers })
            .map(data => data.json())
            .subscribe(data => {
                this.isSaving = false;
                this.message.Success("Save completed successfully.");
                this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
        });
    }

    private cancel(): void {
        this.onCancel.emit(null);
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
    }

    private revert(): void {

    }

}
