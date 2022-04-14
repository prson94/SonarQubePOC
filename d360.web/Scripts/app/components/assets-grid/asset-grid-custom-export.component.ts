import { Input, Component, EventEmitter, Output, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ArtifactType, AssetTypeExportTemplate } from '../../models/artifact-type.model';
import { ArtifactService } from '../../services/artifacts.service';
import { ExportTemplateService } from '../../services/export-template.service';
import { BaseComponent } from '../shared/base.component';
import { SortOrder } from '../../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridOwnerFilter } from '../../models/grid-definition.model';
import { RulesService } from '../../services/rules.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-asset-grid-custom-export',
    template: `
                <div class="row">    
                    <div class="col s12">&nbsp;</div>
                    <div class="form-instructions" i18n>Please select how you would like to see the exported results formatted.</div>                                
                    <div class="col s12" *ngFor="let option of exportOptions">
                        <a (click)="doExport(option)" style="padding:2px;cursor:pointer">{{option.Name}}</a> <span *ngIf="option.Description">- {{option.Description}}</span>
                    </div>  
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12 buttons">
                        <button pButton type="button" style="width: '150px';" i18n-label label="Close" (click)="closeClick.emit()"></button>                        
                    </div>                    
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ArtifactService, ExportTemplateService, RulesService]
})

export class AssetGridCustomExportComponent extends BaseComponent implements OnInit {
    @Input() gridObject: ArtifactType;
    @Input() objectType: string = 'ArtifactType';
    @Input() sortField: string;
    @Input() sortOrder: SortOrder;
    @Input() filters: GridFilterExpression[]
    @Input() relationships: GridRelationshipFilterExpression[];
    @Input() simpleFilter: string;
    @Input() owner: GridOwnerFilter;

    @Output() closeClick = new EventEmitter();
    @Output() customExportClick = new EventEmitter();

    exportOptions: AssetTypeExportTemplate[];

    constructor(
        protected artifactService: ArtifactService,
        protected rulesService: RulesService,
        protected settingsService: CompanySettingsService,
        protected exportTempalteService: ExportTemplateService,
        private changeDetectorRef: ChangeDetectorRef
    ) { super(settingsService); }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.exportTempalteService.getExportTemplatesForAssetType(this.gridObject.AssetTypeUID).subscribe(res => {
            this.isLoading = false;
            this.exportOptions = res;
            this.changeDetectorRef.markForCheck();
        });
    }

    private doExport(option: AssetTypeExportTemplate) {
        this.customExportClick.emit(option);
    }
}