import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService, MessagesService } from '../../../services/index';
import { FusionRuleStep, FusionRuleMapping } from '../../../models/fusion.model';
import { FormMode } from '../../../models/form.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-fusion-rule-step-mapping-list',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>Mappings for selected step<d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
        <input [hidden]="!showSimpleFilter" #gbRuleMappings type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
        <p-dataTable #dtRuleMappings [globalFilter]="gbRuleMappings" [value]="values" selectionMode="single" [selection]="selection" (selectionChange)="selectionChange.emit($event)" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
            <footer *ngIf="dtRuleMappings.totalRecords"><d3s-grid-paging-info [totalRecords]="dtRuleMappings.totalRecords" [first]="dtRuleMappings.first" [rows]="dtRuleMappings.rows"></d3s-grid-paging-info></footer>
            <p-column header="Source" field="SourceFieldName" [filter]="!showSimpleFilter"></p-column>
            <p-column header="Target" field="TargetFieldName" [filter]="!showSimpleFilter"></p-column>
            <p-column header="">
                <template pTemplate type="body" let-row="rowData">
                    <div class="RowTools">
                        <a (click)="edit(row);"><i class="fa fa-pencil"></i></a>
                        <a (click)="delete(row);"><i class="fa fa-trash-o"></i></a>
                    </div>
                </template>
            </p-column>
        </p-dataTable>
    </div>

`,
    providers: [FusionService]
})

export class FusionRuleStepMappingListComponent extends BaseComponent implements OnChanges {
    @Input() fusionRuleStep: FusionRuleStep;
    @Input() selection: FusionRuleMapping;
    @Output() selectionChange = new EventEmitter<FusionRuleMapping>();
    @Output() onAddClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();

    values: FusionRuleMapping[];


    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionRuleStep'] && changes['fusionRuleStep'].currentValue != changes['fusionRuleStep'].previousValue) 
            this.load();
    }

    load() {
        if (this.fusionRuleStep == null) {
            this.values = [];
            return;
        }
        this.isLoading = true;
        this.fusionService.getFusionRuleStepMappings(this.fusionRuleStep.ID)
            .then(r => {

                //update Source/Target subject area fields with company settings value
                r.filter(i => i.TargetFieldName == "TaxonomyTypeID").forEach(i => {
                    i.TargetFieldName = (CompanySettings.ArtifactType_TaxonomyTypeID || "Subject Area");
                });
                r.filter(i => i.SourceFieldName == "TaxonomyTypeID").forEach(i => {
                    i.SourceFieldName = (CompanySettings.ArtifactType_TaxonomyTypeID || "Subject Area");
                });

                this.values = r;
                if (this.values.length > 0) {
                    if (this.selection == null || this.values.findIndex(v => v.ID == this.selection.ID) < 0)
                        this.selectionChange.emit(this.values[0]);
                } else
                    this.selectionChange.emit(null);
                this.isLoading = false;
            });
    }

    add() {
        this.onAddClick.emit();
    }

    edit(e: FusionRuleMapping) {
        this.selectionChange.emit(e);
        this.onEditClick.emit();
    }

    delete(e: FusionRuleMapping) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit();
    }
}