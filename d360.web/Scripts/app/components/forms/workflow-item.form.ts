
import { Input, Output, Component, OnInit, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { SelectItem } from '../../models/form.model';
import { CompanySettings as cs } from '../../models/company-settings.model';
import { WorkflowService } from '../../services/workflow.service';
import * as _ from 'lodash';


declare var CompanySettings: cs;

@Component({
    selector: 'workflow-item-form',
    templateUrl: './workflow-item.form.html',
    providers: [WorkflowService],
})

export class WorkflowItemForm implements OnInit {
    @Input() item: WorkflowItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();


    private ObjectTypes = new Array<SelectItem>();
    private ParentTypes = new Array<SelectItem>();
    private ResponsibilityTypes = new Array<SelectItem>();
    //private message: FormMessage = new FormMessage();
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

    constructor(private workflowService: WorkflowService) {
    }

    ngOnInit() {
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

        this.workflowService.getWorkflow(this.item.ID, this.item.WorkflowType).then(data => {
            //console.log(data);
            this.ObjectTypes = data.ObjectTypes;
            this.ObjectTypes.map(o => { o.label = o.Text; o.value = o.Value });
            this.ParentTypes = data.ParentTypes;
            this.ParentTypes.map(o => { o.label = o.Text; o.value = o.Value });
            this.ResponsibilityTypes = data.ResponsibilityTypes
            this.ResponsibilityTypes.map(o => { o.label = o.Text; o.value = o.Value });

            //this.objectTypeChange();

            this.numDays = data.WorkflowTypeRelation.Fields["DaysGivenToCompleteCertification"] || this.numDays;
            this.numMonths = data.WorkflowTypeRelation.Fields["MonthsUntilCertification"] || this.numMonths;
            this.dateScheduleCalculation = data.WorkflowTypeRelation.Fields["DateForScheduleCalculation"] || this.dateScheduleCalculation;

            this.isLoading = false;
            this.onLoadComplete.emit({ item: this.item });
        });
    }

    private save(): void {

        this.isSaving = true;

        try {
            this.item.Object = this.ObjectType.split('|')[0];
            this.item.ObjectID = parseInt(this.ObjectType.split('|')[1]);
            this.item.Parent = this.ParentType.split('|')[0];
            this.item.ParentID = parseInt(this.ParentType.split('|')[1]);
        } catch (exception) {
            this.isSaving = false;
            //this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: 'An error occurred while parsing the select item values.', initialItem: this.initialItem });
            return;
        }

        this.item.Fields = [];
        this.item.Fields.push({ key: 'DateForScheduleCalculation', value: this.dateScheduleCalculation });
        this.item.Fields.push({ key: 'DaysGivenToCompleteCertification', value: this.numDays });
        this.item.Fields.push({ key: 'MonthsUntilCertification', value: this.numMonths });

        this.workflowService.postWorkflow(this.item).then(p => {
            this.isSaving = false;
            //this.message.Success("Save completed successfully.");
            this.onSaveComplete.emit({ item: this.item, message: 'Save completed successfully.', initialItem: this.initialItem });
        });
    }

    private cancel(): void {
        this.onCancel.emit({ initialItem: this.initialItem });
    }

    private objectTypeChange() {
        this.isLoadingResponsibility = true;

        let val = this.ObjectType;

        let obj;
        let id;

        try {
            obj = val.split('|')[0];
            id = val.split('|')[1];
        } catch (exception) {
            this.isLoadingResponsibility = false;
            return;
        }

        this.workflowService.getResponsibilityTypeSelectList(id, obj)
            .then(r => {
                this.ResponsibilityTypes = r;
                this.ResponsibilityType = null;
                this.isLoadingResponsibility = false;
            }); 

        this.workflowService.getParentTypeSelectList(id, obj, this.item.WorkflowType)
            .then(r => {
                this.ParentTypes = r;
            });

    }

}
