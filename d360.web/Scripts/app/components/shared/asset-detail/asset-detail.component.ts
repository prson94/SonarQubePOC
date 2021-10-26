import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { DetailRow, DetailField, DetailFieldType, NymType, Category, ComplexLookupType } from '../../../models/object-detail.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { SynonymPermission } from '../../../models/artifacts.model';

declare var CurrentResourceID;

@Component({
    selector: 'ig-asset-detail',
    templateUrl: './asset-detail.component.html',
    providers: [ObjectDetailService, AssetService]
})


export class AssetDetailComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() nymTypes: NymType[] = [];
    @Input() objectUID: string;
    @Input() hasAddRelationshipsPermissions: boolean;
    @Input() hasModifyRelationshipsPermissions: boolean;
    @Input() hasDeleteRelationshipsPermissions: boolean;
    @Input() useAccordion: boolean = false;
    @Input() shouldBePadded: boolean = true;
    @Input() tooltipAlign: string;
    @Input() showHeader: boolean = false;
    @Input() showTabs: boolean = false;
    @Input() showHeaderLine: boolean = true;
    @Input() spacerHeight: string = '32px';
    @Input() paddingLeft: string;
    @Input() isSidePanel: boolean = false;
    @Input() useAssetDetailColumnDefinition: boolean = false;
    @Input() synonymPermission: SynonymPermission;
    
    @Input() assetDetail: any;

    assetUID: string;
    assetTypeUID: string;
    isLoading = false;
    DetailFieldType = DetailFieldType;

    readonly systemProperties: string = "System Fields";
    readonly noCategory: string = "None";
    readonly defaultCategory: string = "General";

    model: any;
    tab: string = 'detail';
    categories: Category[] = new Array<Category>();
    systemPropertiesCategory: Category = new Category(this.systemProperties);

    rows = new Array<DetailRow>();
    constructor(
        private router: Router,
        private objectDetailService: ObjectDetailService,
        protected messagesService: MessagesObservableService,
        private assetService: AssetService,
        private cdRef: ChangeDetectorRef) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p === 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
            if (p === 'objectUID') {
                this.objectUID = changes['objectUID'].currentValue;
            }
        }

        this.load();
    }

    public load(): void {
        let detailSub = null;
        if (this.assetDetail) {
            detailSub = this.assetDetail;
        } else {
            if (this.objectType && this.objectID) {
                detailSub = this.objectDetailService.getObjectDetail(this.objectID, this.objectType, true, this.showHeader, this.useAssetDetailColumnDefinition);
            }

            if (this.objectType && this.objectUID) {
                detailSub = this.objectDetailService.getObjectDetailByUid(this.objectUID, this.objectType, true, this.showHeader, this.useAssetDetailColumnDefinition);
            }
        }        

        if (detailSub) {
            this.isLoading = true;
            detailSub
                .subscribe((data) => {
                    this.model = data;
                    this.rows = data.rows;
                    this.objectID = data.ObjectID;
                    this.objectType = data.Object;
                    this.categories = [];
                    for (var i = 0; i < this.rows.length; i++) {
                        if (this.rows[i].Category == null || this.rows[i].Category === "" || this.rows[i].Category === this.noCategory) {
                            this.rows[i].Category = this.defaultCategory;
                        }
                    }

                    this.populateSystemProperties(this.rows);

                    //remove system property rows.
                    this.rows = this.rows.filter((r) => !r.Category || r.Category.toUpperCase() !== this.systemProperties.toUpperCase());

                    this.rows.forEach((r) => {
                        if (r.Category && r.Category.toUpperCase() !== this.noCategory.toUpperCase() && this.categories.find((c) => c.name === r.Category) == null) {
                            let category = new Category(r.Category);
                            category.active = true;
                            this.categories.push(category);
                        }


                        this.populateRow(r);
                    });

                    let displayRows = this.rows.filter((r) => (r.Category == null || r.Category.toUpperCase() === this.noCategory.toUpperCase()) && ((r.FirstColumnFields && r.FirstColumnFields.length > 0) || (r.SecondColumnFields && r.SecondColumnFields.length > 0)));
                    if (this.categories.findIndex((x) => x.name.toUpperCase() === this.systemProperties.toUpperCase()) >= 0) {
                        this.categories.push(this.categories.splice(this.categories.findIndex((x) => x.name.toUpperCase() === this.systemProperties.toUpperCase()), 1)[0]);
                    }
                    for (let i = 0; i < this.categories.length; i++) {
                        let items = this.rows.filter((r) => r.Category === this.categories[i].name);
                        this.categories[i].rows = [];
                        for (let j of items) {
                            if ((j.FirstColumnFields && j.FirstColumnFields.length > 0) || (j.SecondColumnFields && j.SecondColumnFields.length)) {
                                this.categories[i].rows.push(j);
                            }
                        }
                    }

                    this.categories = this.categories.sort((a, b) => {
                        if (a.name === this.defaultCategory || b.name === this.systemProperties) {
                            return -1;
                        }
                        if (b.name === this.defaultCategory || a.name === this.systemProperties) {
                            return 1;
                        }

                        return 0;
                    });

                    if (this.showHeader && this.model.Scores != null) {
                        this.model.Scores.forEach((s) => {
                            this.setThresholdClass(s);
                        });
                    }

                    this.rows = displayRows;
                    this.loadCategory();
                    this.loadState();
                    this.isLoading = false;
                    this.cdRef.markForCheck();
                });
        }
    }

    private setDetailFieldType(field: DetailField) {
        field.Type = DetailFieldType.Field;
        if ((field.Value == null || field.Value === '') && field.ShowIfEmpty === false) {
            field.Type = DetailFieldType.None;
        }
        if (field.TooltipContext != null) {
            if (field.Value != null && field.Value !== '') {
                field.Type = DetailFieldType.Tooltip;
            }
            else {
                field.Type = DetailFieldType.None;
            }
        }

        if (field.ComplexLookupType === ComplexLookupType.Grid) {
            field.Type = DetailFieldType.LookupGrid;
        } else if (field.ComplexLookupType === ComplexLookupType.List) {
            field.Type = DetailFieldType.LookupList;
        }
    }

    private saveState() {
        localStorage.setItem(this.storageKey, JSON.stringify(this.categories.map((c) => { return { name: c.name, active: c.active }; })));
    }

    private loadState() {
        var state = JSON.parse(localStorage.getItem(this.storageKey));

        if (state != null) {
            state.forEach((s) => {
                let ix = this.categories.findIndex((c) => c.name === s.name);
                if (ix > -1) {
                    this.categories[ix].active = s.active;
                }
            });
        }
    }

    get storageKey(): string {
        return `asset_detail_${CurrentResourceID}_${this.assetTypeUID}`;
    }

    private loadCategory() {
        this.categories.forEach((c) => {
            var rcount = c.rows.length;
            c.rows.forEach((r) => {
                let fcount = r.FirstColumnFields.length;
                r.FirstColumnFields.forEach((f) => {
                    if (f.Type === DetailFieldType.LookupGrid || f.Type === DetailFieldType.LookupList) {
                        if (!f.Data || !f.Data.Values || f.Data.Values.length === 0) {
                            c.hasData = true;
                        }
                        fcount--;

                        if (fcount <= 0) {
                            rcount--;
                        }

                        if (rcount <= 0) {
                            c.loaded = true;
                        }
                    }
                    else {
                        if (f.Type !== DetailFieldType.None) {
                            c.hasData = true;
                        }
                        fcount--;
                        if (fcount <= 0) {
                            rcount--;
                        }
                        if (rcount <= 0) {
                            c.loaded = true;
                        }
                    }
                });
            });
        });

        // if there are no fields (non-system) without a category then expand the first category unless it's system properties
        if (this.categories && this.categories.length > 0
            && this.rows.filter((x) => !x.Category || x.Category.toUpperCase() !== this.noCategory.toUpperCase()).length === 0
            && this.categories[0].name.toUpperCase() !== this.systemProperties.toUpperCase()) {
            this.categories[0].active = true;
        }

    }

    private populateRow(row) {
        row.FirstColumnFields.forEach((f) => {
            this.setDetailFieldType(f);

            if ((f.FieldName || "").toUpperCase() === 'ASSETUID') {
                this.assetUID = f.Value;
            }

            if ((f.FieldName || "").toUpperCase() === "ASSETTYPEUID") {
                this.assetTypeUID = f.Value;
            }

        });
        row.FirstColumnFields = row.FirstColumnFields.filter((f) => f.Type !== DetailFieldType.None);

        row.SecondColumnFields.forEach((s) => {
            this.setDetailFieldType(s);

            if (s.Type === DetailFieldType.LookupGrid || s.Type === DetailFieldType.LookupList) {
                this.assetService.getAssetsComplexFieldValue(this.objectUID, s.FieldName)
                    .subscribe((i) => {
                        s.Data = i;
                        if ((!s.Data || !s.Data.Values || s.Data.Values.length === 0) && (!s.ShowIfEmpty)) {
                            s.Type = DetailFieldType.None;
                            row.SecondColumnFields.splice(row.SecondColumnFields.indexOf(s), 1);
                        }
                    });
            }

            if ((s.FieldName || "").toUpperCase() === 'ASSETUID') {
                this.assetUID = s.Value;
            }

            if ((s.FieldName || "").toUpperCase() === "ASSETTYPEUID") {
                this.assetTypeUID = s.Value;
            }
        });

        row.SecondColumnFields = row.SecondColumnFields.filter((f) => f.Type !== DetailFieldType.None);
    }

    private populateSystemProperties(rows: DetailRow[]) {
        let systemPropertyItems = this.rows.filter((row) => row.Category && row.Category.toUpperCase() === this.systemProperties.toUpperCase());

        this.systemPropertiesCategory.rows = [];
        for (let j of systemPropertyItems) {
            if ((j.FirstColumnFields && j.FirstColumnFields.length > 0) || (j.SecondColumnFields && j.SecondColumnFields.length)) {
                this.systemPropertiesCategory.rows.push(j);
            }
        }
        this.systemPropertiesCategory.rows.forEach((row) => {
            this.populateRow(row);
        });

        this.systemPropertiesCategory.hasData = this.systemPropertiesCategory.rows.length > 0;
        this.systemPropertiesCategory.loaded = true;
    }

    open(newTab: boolean = false) {
        let url = SiteUrlHelpers.getObjectUrl(this.model.Object, this.model.ObjectID, this.model.ObjectTypeID);

        if (newTab) {
            window.open(url, '_blank');
        } else {
            this.router.navigateByUrl(url);
        }
    }

    setThresholdClass(score: any) {
        if (score != null && score.UpperThreshold != null && score.LowerThreshold != null) {
            let v = score.Value * 100;
            if (v <= score.LowerThreshold) {
                score.Class = 'poor';
            } else if (v > score.LowerThreshold && v <= score.UpperThreshold) {
                score.Class = 'average';
            } else {
                score.Class = 'good';
            }
        }
    }

    clickTab(key: string) {
        this.tab = key;
    }
}
