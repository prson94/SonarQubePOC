import { CommonModule } from '@angular/common';
import { NgModule, Input, Component, EventEmitter, Output, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CoreModule } from './core.module';
import { ButtonModule } from 'primeng/primeng';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';
import { FormHelpers } from '../../static/form-helpers';
import { FormHelper } from '../../models/form.model';

@Component({
    selector: 'd3s-fusion-attribute-profile-details',
    template: ` 
<div class="tile tile-detail" *ngIf="fields.length > 0">
	<header>{{name}} Profile</header>
	<d3s-loading [isLoading]="isLoading"></d3s-loading>
	<div style="max-height: 400px; overflow-y: scroll">
		<div *ngIf="!isLoading" class="row">
			<div class="col s4">
				<div *ngFor="let f of fields">
					<ng-container *ngIf="f.col == 1">
						<div class="FieldName">{{f.name}}</div>
						<div class="FieldContent">{{f.value}}</div>
					</ng-container>
				</div>
			</div>
			<div class="col s4">
				<div *ngFor="let f of fields">
					<ng-container *ngIf="f.col == 2">
						<div class="FieldName">{{f.name}}</div>
						<div class="FieldContent">{{f.value}}</div>
					</ng-container>
				</div>
			</div>
			<div class="col s4">
				<div *ngFor="let f of fields">
					<ng-container *ngIf="f.col == 3">
						<div class="FieldName">{{f.name}}</div>
						<div class="FieldContent">{{f.value}}</div>
					</ng-container>
				</div>
			</div>
		</div>    
		<div *ngIf="hasClose" class="row">
			<div class="s12">&nbsp;</div>
			<div class="col s1">
				<button pButton type="button" (click)="close.emit()" label="Close"></button>
			</div>
		</div>    
	</div>
</div>
                `,
    providers: [FusionAttributeService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class FusionAttributeProfileDetailsComponent extends BaseComponent implements OnChanges {
    @Input() fusionAttributeId: number;
    @Input() name: string;
    @Input() objectType: string = "FusionAttribute";
    @Input() hasClose: boolean = false;

    @Output() close = new EventEmitter();
    @Output() assetIdChange = new EventEmitter();

    private profile: any;
    private fields: any[] = [];

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
        this.fusionAttributeService.getFusionAttributeProfile(this.objectType, this.fusionAttributeId).subscribe(
            res => {
                this.isLoading = false;

                this.profile = res;
                this.fields = [];

                let excluded = ['assetid', 'effectivestartdate', 'effectiveenddate', 'createdby', 'createdon', 'updatedby', 'updatedon'];
                let assetID = -1;

                if (this.profile != null && this.profile[0] != null) {
                    for (let i = 0; i < this.profile[0].length; i++) {
                        let name = this.profile[0][i].Key;
                        let value = this.profile[0][i].Value;

                        if (value == null) {
                            value = 'N/A';
                        }
                        if (value.toString().startsWith('/Date(')) {
                            value = new Date(parseInt(value.replace('/Date(', ''))).toLocaleDateString();
                        }
                        if (name.toLowerCase() == 'assetid') {
                            assetID = +value;
                        }
                        if (excluded.findIndex(e => e == name.toLowerCase()) > -1) {
                            continue;
                        }

                        this.fields.push({
                            name: name,
                            value: value,
                            col: (i % 3) + 1
                        })
                    }

                    if (assetID > -1) {
                        this.assetIdChange.emit(assetID);
                    }
                }

                this.ref.markForCheck();
            });
    }
};



@NgModule({
    declarations: [
        FusionAttributeProfileDetailsComponent,
    ],
    exports: [
        FusionAttributeProfileDetailsComponent,
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

export class SharedFusionAttributeProfileDetailsModule { }