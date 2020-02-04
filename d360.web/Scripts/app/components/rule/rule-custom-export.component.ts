import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ExportTemplateService } from '../../services/export-template.service';
import { BaseComponent } from '../shared/base.component';
import { SortOrder } from '../../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridOwnerFilter } from '../../models/grid-definition.model';
import { RulesService } from '../../services/rules.service';
import { AssetTypeExportTemplate } from '../../models/artifact-type.model';
import { RuleType } from '../../models/rule.model';

@Component({
    selector: 'd3s-rule-custom-export',
    template: `
                <div class="row">    
                    <div class="col s12">&nbsp;</div>
                    <div class="form-instructions">Please select how you would like to see the exported results formatted.</div>                                
                    <div class="col s12" *ngFor="let option of exportOptions">
                        <a (click)="doExport(option)" style="padding:2px;cursor:pointer">{{option.Name}}</a> <span *ngIf="option.Description">- {{option.Description}}</span>
                    </div>  
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12 buttons">
                        <button pButton type="button" style="width: '150px';" label="Close" (click)="closeClick.emit()"></button>                        
                    </div>                    
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RulesService, ExportTemplateService]
})

export class RuleCustomExportComponent extends BaseComponent implements OnInit {
    @Input() ruleType: RuleType;

    @Output() closeClick = new EventEmitter();

    private exportOptions: AssetTypeExportTemplate[];

    constructor(
        protected RuleService: RulesService,
        protected exportTempalteService: ExportTemplateService,
        private changeDetectorRef: ChangeDetectorRef
    ) { super(); }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.exportTempalteService.getExportTemplatesForAssetType(this.ruleType.AssetTypeUID).subscribe(res => {
            this.isLoading = false;
            this.exportOptions = res;
            this.changeDetectorRef.markForCheck();
        });
    }

    private doDefaultExport() {
        this.RuleService.exportRules(this.ruleType.AssetTypeUID, this.ruleType.Name)
    }
     
    private doExport(option: AssetTypeExportTemplate) {
        this.RuleService.exportRulesCustomXls(option.ID, this.ruleType.AssetTypeUID, this.ruleType.Name);
    }
};