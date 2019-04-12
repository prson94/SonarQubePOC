import { CommonModule } from '@angular/common';
import { NgModule, Input, Component, EventEmitter, Output, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { FusionAttributeValueDetails } from '../../models/fusion-attribute.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CoreModule } from './core.module';
import { ButtonModule } from 'primeng/primeng';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';

@Component({
    selector: 'd3s-fusion-attribute-item-details',
    template: ` 
                <header>{{name}} Details</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading" class="row">
                    <div class="col l6 m6" *ngIf="fusionAttributeValueDetails.Name">
                        <div class="FieldName">Name</div>
                        <div class="FieldContent">{{fusionAttributeValueDetails.Name}}</div>
                    </div>
                    <div class="col l6 m6" *ngIf="fusionAttributeValueDetails.TextPath">
                        <div class="FieldName">Path</div>
                        <div class="FieldContent">{{fusionAttributeValueDetails.TextPath}}</div>
                    </div>
                    <div *ngFor="let field of fusionAttributeValueDetails?.Fields" class="col l6 m6">
                        <div class="FieldName">{{field.Name}}</div>
                        <div class="FieldContent scrollLargeText">
                             <object-detail-field [field]="field"></object-detail-field>
                        </div>
                        
                    </div>                    
                </div>    
                <div *ngIf="hasClose" class="row">
                    <div class="s12">&nbsp;</div>
                    <div class="col s1">
                        <button pButton type="button" (click)="close.emit()" label="Close"></button>
                    </div>
                </div>            
                `,
    styles: [`
            .scrollLargeText{
                overflow:auto;
                max-height:150px;
                white-space:normal;
                word-wrap:break-word;
            }
        `],
    providers: [FusionAttributeService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class FusionAttributeItemDetailsComponent extends BaseComponent implements OnChanges {
    @Input() fusionAttributeId: number;
    @Input() name: string;
    @Input() objectType: string = "FusionAttribute";
    @Input() hasClose: boolean = false;

    @Output() close = new EventEmitter();
    @Output() assetIdChange = new EventEmitter();

    private fusionAttributeValueDetails: FusionAttributeValueDetails;

    constructor(private fusionAttributeService: FusionAttributeService, private router: Router, private ref: ChangeDetectorRef) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusionAttributeId'] && this.fusionAttributeId) {            
            this.load();
        }
    }

    private load() {
        this.isLoading = true;
        this.fusionAttributeService.getFusionAttributeDetails(this.objectType, this.fusionAttributeId).subscribe(
            res => {
                this.fusionAttributeValueDetails = res;
                this.assetIdChange.emit(this.fusionAttributeValueDetails.AssetID);
                this.ref.markForCheck();

                this.isLoading = false;
            }
        );
    }

    public openItemInFusion() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionAttributeValueDetails.FusionID};fusionAttributeTypeId=${this.fusionAttributeValueDetails.FusionAttributeTypeID};fusionAttributeId=${this.fusionAttributeId}`);
    }
};



@NgModule({
    declarations: [
        FusionAttributeItemDetailsComponent,
    ],
    exports: [
        FusionAttributeItemDetailsComponent,
    ]
    , imports: [
        CommonModule,
        RouterModule,

        CoreModule,
        SharedObjectDetailsModule,
        //prime
        ButtonModule,
    ]

})

export class SharedFusionAttributeItemDetailsModule { }