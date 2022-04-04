import { Component, Input, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy } from "@angular/core";
import { LookupGrid, LookupGridField } from "../../../models/grid-definition.model";
import { BaseComponent } from "../base.component";
import { DetailField } from "../../../models/object-detail.model";
import { AssetService } from "../../../services/asset.service";
import { Subscription } from "rxjs";
import { CompanySettingsService } from "../../../services/settings.service";


class OwnershipResource {
    ResourceName: string;
    ResourceUid: string;
    ResponsibilityTypes: string;
    ResourceItemUrl: string;
}

@Component({
    selector: "ig-asset-lookup-list",
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <d3s-ownership-list *ngIf="!isLoading" [list]="resources" [interceptLinkClick]="interceptLinkClick"></d3s-ownership-list>
    `,
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetLookupListComponent extends BaseComponent implements OnDestroy, OnInit {
    @Input() data: LookupGrid;
    @Input() field: DetailField;
    @Input() assetUid: string = '';
    @Input() interceptLinkClick: boolean = false;

    lookupField: LookupGridField;
    loadSubscription: Subscription;
    resources: OwnershipResource[];

    constructor(private assetService: AssetService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    ngOnDestroy() {
        if (this.loadSubscription) {
            this.loadSubscription.unsubscribe();
        }
    }

    ngOnInit() {
        this.loadData();
    }

    export() {
        var params = {
            '_pageSize': 10000,
            '_pageNum': 1
        };
        let fileName: string = this.field.FieldName;
        if (this.data['name']) {
            fileName = this.data['name'];
        }
        this.assetService.getAssetsComplexFieldValue(this.assetUid, this.field.FieldName, params, true, fileName);
    }

    loadData() {
        this.isLoading = true;
        var params = {
            '_pageSize': 10000,
            '_pageNum': 1
        };

        if (this.loadSubscription) {
            this.loadSubscription.unsubscribe();
        }

        this.loadSubscription = this.assetService.getAssetsComplexFieldValue(this.assetUid, this.field.FieldName, params)
            .subscribe((result) => {
                this.data = result;
                this.resources = result.Values
                    .reduce((p, c, i) => {
                        let idx = p.findIndex((x) => { return x.ResourceName === c.ResourceName; });
                        if (idx === -1) {
                            p.push({
                                ResourceName: c.ResourceName,
                                ResourceUid: c.ResourceUid ?? c.SecurityAssetUid,
                                ResponsibilityTypes: [c.ResponsibilityTypeName],
                                ResourceItemUrl: c.ResourceItemUrl,
                            });
                        } else {
                            p[parseInt(idx)].ResponsibilityTypes.push(c.ResponsibilityTypeName);
                        }
                        return p;
                    }, [])
                    .sort((a, b) => (a.ResourceName < b.ResourceName ? -1 : 1))
                    .map((x) => {
                        return {
                            ResourceName: x.ResourceName,
                            ResourceUid: x.ResourceUid,
                            ResponsibilityTypes: x.ResponsibilityTypes.filter((v, i, s) => s.indexOf(v) === i).sort().join(", "),
                            ResourceItemUrl: x.ResourceItemUrl,
                        };
                    });
                this.isLoading = false;
                this.cdRef.markForCheck();
            }, null, () => {
                this.isLoading = false;
                this.cdRef.markForCheck();
            });
    }
}
