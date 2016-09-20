import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeService } from '../../services/index';
import { FusionAttributeValueDetails } from '../../models/fusion-attribute.model';

@Component({
    selector: 'd3s-fusion-attribute-item-details',
    template: ` 
                <header>{{name}} Details</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading" class="row">
                    <div class="col l6 m6">
                        <div class="FieldName">Name</div>
                        <div class="FieldContent">{{fusionAttributeValueDetails?.Name}}</div>
                    </div>
                    <div class="col l6 m6">
                        <div class="FieldName">Path</div>
                        <div class="FieldContent">{{fusionAttributeValueDetails?.TextPath}}</div>
                    </div>
                    <div *ngFor="let field of fusionAttributeValueDetails?.Fields" class="col l6 m6">
                        <div class="FieldName">{{field.Name}}</div>
                        <div class="FieldContent scrollLargeText" [title]="field?.Value">{{field?.Value}}</div>
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
})

export class FusionAttributeItemDetailsComponent extends BaseComponent implements OnChanges {
    @Input() fusionAttributeId: number;
    @Input() name: string;

    private fusionAttributeValueDetails: FusionAttributeValueDetails;

    constructor(private fusionAttributeService: FusionAttributeService, private router: Router) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusionAttributeId'] && this.fusionAttributeId) {            
            this.load();
        }
    }

    private load() {
        this.isLoading = true;
        this.fusionAttributeService.getFusionAttributeDetails(this.fusionAttributeId)
            .then(res => {
                this.isLoading = false;
                this.fusionAttributeValueDetails = res;
            });
    }

    public openItemInFusion() {
        this.router.navigateByUrl(`/a/fusion/${this.fusionAttributeValueDetails.FusionID};fusionAttributeTypeId=${this.fusionAttributeValueDetails.FusionAttributeTypeID};fusionAttributeId=${this.fusionAttributeId}`);
    }
};