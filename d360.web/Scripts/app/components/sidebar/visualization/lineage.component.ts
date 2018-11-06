import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-lineage-wrapper',
    template: `
        <ng-container *ngIf="!useLegacy; else legacy">
            <d3s-lineage-diagram [objectID]="objectID" [objectType]="objectType" [readonly]="true"></d3s-lineage-diagram>
        </ng-container>
        <ng-template #legacy>
            <d3s-lineage [objectID]="objectID" [objectType]="objectType" [readonly]="true" [usageOnly]="usageOnly"></d3s-lineage>
        </ng-template>
        `
})

export class LineageComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private usageOnly: boolean = false;
    private useLegacy: boolean = true;

    constructor(
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.UseLegacyLineage != null) {
            this.useLegacy = CompanySettings.UseLegacyLineage.toString() == "true" ? true : false;
        }

        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.usageOnly = params['showUsageOnly'] == '1';
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
