import { Input, Component  } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionConfigurationDetails } from '../../models/fusion.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-manual-load',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading">
                    <header>Manual Load Fusion Data</header>   
                    <div class="form-instructions">Please ensure that you have the original column headers in your spreadsheet.  Choose an appropriate type that you want to load.  Then choose your spreadsheet.  The layout of the spreadsheet should correspond exactly with the template available for download on the Fusion configuration page.</div> 
                    <div class="row">
                        <div class="col l2 m12 s12">
                            <d3s-fusion-structure-tree [fusion]="fusion" [(fusionAttributeTypeId)]="selectedFusionAttributeTypeId"></d3s-fusion-structure-tree>
                        </div>
                        <div class="col l10 m12 s12">
                            <p><a style="cursor:pointer" (click)="downloadTemplate()">Download Template</a> - Use the template to load new data to the {{fusion.Name}} fusion data.</p>
                            <div class="row">
                                <h4 style="margin-top:20px;margin-bottom:5px;">Upload Data from a spreadsheet</h4>
                                <p-fileUpload name="file" [url]="fileUploadUrl()" (onUpload)="onUpload($event)" 
                                        multiple="multiple" accept=".xls,.xlsx" maxFileSize="10000000">
                                    <template pTemplate type="content">
                                        <ul *ngIf="uploadedFiles.length">
                                            <li *ngFor="let file of uploadedFiles">{{file.name}} - {{file.size}} bytes</li>
                                        </ul>
                                    </template>        
                                </p-fileUpload>
                                <em>To see the progress of your upload view the <a (click)="goToFusion()" style="cursor:pointer">Execution Status</a> area of Fusion or click the History tab to the right.</em>
                            </div>
                        </div>
                    </div>
                </div>
          `,
    providers: [FusionService],
})

export class FusionManualLoadComponent extends BaseComponent {
    @Input() fusion: FusionConfigurationDetails;
    uploadedFiles: any[] = [];

    private selectedFusionAttributeTypeId: number;

    constructor(private router: Router, private fusionService: FusionService) {
        super();
    }
    

    private fileUploadUrl() {
        return `internal/fusion/${this.fusion.FusionTypeID}/configurations/${this.fusion.ID}/template/${this.selectedFusionAttributeTypeId}`;
    }

    private onUpload(event) {
        for (let file of event.files) {
            this.uploadedFiles.push(file);
        }        
    }
        
    private downloadTemplate() {
        if (!this.fusion || !this.fusion.ID || !this.fusion.FusionTypeID || !this.selectedFusionAttributeTypeId) {
            console.log("ERROR - NO FUSION / FUSIONATTRIBUTE TYPE ID POPULATED");

            return;
        }
        this.fusionService.downloadFusionManualLoadTemplate(this.fusion.ID, this.fusion.FusionTypeID, this.selectedFusionAttributeTypeId);
    }

    private goToFusion() {
        this.router.navigateByUrl(SiteUrlHelpers.SITE_URL_FUSION_ROOT );
    }
};