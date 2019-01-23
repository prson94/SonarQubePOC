import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ArtifactType, AssetTypeExportTemplate } from '../../models/artifact-type.model';
import { ArtifactService } from '../../services/artifacts.service';
import { ExportTemplateService } from '../../services/export-template.service';
import { BaseComponent } from '../shared/base.component';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridOwnerFilter } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-artifact-custom-export',
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
    providers: [ArtifactService, ExportTemplateService]
})

export class ArtifactCustomExportComponent extends BaseComponent implements OnInit {
    @Input() artifactType: ArtifactType;
    @Input() sortField: string;
    @Input() sortOrder: SortOrder;
    @Input() filters: GridFilterExpression[]
    @Input() relationships: GridRelationshipFilterExpression[];
    @Input() attributes: GridAttributeFilterExpression[];
    @Input() simpleFilter: string;
    @Input() owner: GridOwnerFilter;

    @Output() closeClick = new EventEmitter();

    private exportOptions: AssetTypeExportTemplate[];
    
    constructor(
        protected artifactService: ArtifactService,
        protected exportTempalteService: ExportTemplateService,
        private changeDetectorRef: ChangeDetectorRef
    ) { super(); }

    ngOnInit() {
        this.load();
    }
    
    private load() {
        this.isLoading = true;
        this.exportTempalteService.getExportTemplatesForAssetType(this.artifactType.AssetTypeUID).subscribe(res => {
            this.isLoading = false;
            this.exportOptions = res;
            this.changeDetectorRef.markForCheck();
        });
    }

    private doDefaultExport() {
        this.artifactService.getArtifactsXls(false, this.artifactType, this.sortField, this.sortOrder, this.filters, this.relationships, this.attributes, this.simpleFilter, this.owner);
    }

    private doExport(option: AssetTypeExportTemplate) {
        this.artifactService.getArtifactsCustomXls(option.ID, false, this.artifactType, this.sortField, this.sortOrder, this.filters, this.relationships, this.attributes, this.simpleFilter, this.owner);
    }
};