import * as _ from 'lodash';
import { AfterViewInit, Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { StoredAssetBrowserFilterModel, AssetBrowserFilterModel, FilterSelectionsModel, DiagramType } from '../../../../../models/lineage.model';
import { BrowserService } from '../../../../../services/browser.service';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';
import { MenuItem } from 'primeng/api';

@Component({
    selector: 'd3s-assetbrowser-savedfilter',
    templateUrl: './savedfilter.component.html',
    providers: [BrowserService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
.ig-dropdown{ padding-right: 4px; }
        `]
})
export class AssetBrowserSavedFilterComponent implements OnInit, AfterViewInit, OnChanges {
    @Input() diagramType: DiagramType;
    @Input() options: FilterSelectionsModel;
    @Input() filterModel: AssetBrowserFilterModel;
    @Output() applySavedFilter: EventEmitter<AssetBrowserFilterModel> = new EventEmitter();

    allFilters: StoredAssetBrowserFilterModel[] = [];
    savedFilters: StoredAssetBrowserFilterModel[] = [];
    selectedFilter: StoredAssetBrowserFilterModel;
    createUserFilter: StoredAssetBrowserFilterModel = new StoredAssetBrowserFilterModel();
    saveFilterModalVisible: boolean = false;
    saveFilterModalWorking: boolean = false;
    deleteFilterModalVisible: boolean = false;
    deleteFilterModalWorking: boolean = false;

    removeTitle = $localize`Remove`;
    saveTitle = $localize`Save`;

    menuitems = [
        { title: $localize`Add`, callback: (event) => { this.add() } },
        { title: this.saveTitle, disabled: !this.hasSelectedUserFilter(), callback: (event) => { this.update() } },
        { title: this.removeTitle, disabled: !this.hasSelectedUserFilter(), callback: (event) => { this.showRemove() } }
    ];

    constructor(
        private browserService: BrowserService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {

    }

    public ngOnInit() {
        this.load();
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }



    ngOnChanges(changes: SimpleChanges): void {
        if (changes["diagramType"]) {
            this.selectedFilter = null;
            this.savedFilters = this.allFilters.filter(f => { return f.diagramType == this.diagramType; });
        }
    }

    private numberOfHops() {
        let numberOfHops: number = this.filterModel.NumberOfImpactHops;
        if (this.diagramType == DiagramType.Lineage) {
            numberOfHops = this.filterModel.NumberOfLineageHops;
        }
        return numberOfHops;
    }

    private add() {
        this.saveFilterModalVisible = true;
        this.saveFilterModalWorking = false;
        this.createUserFilter = new StoredAssetBrowserFilterModel();
        this.createUserFilter.assetTypes = this.options.AssetTypeOptions
            .filter((a) => this.filterModel.SelectedAssetTypes.indexOf(a.AssetTypeId) > -1)
            .map((a) => { return { uid: a.Uid, class: a.Class } });
        this.createUserFilter.responsibilityTypes = this.options.ResponsibilityTypeOptions
            .filter((r) => this.filterModel.SelectedResponsibilityTypes.indexOf(r.Id) > -1)
            .map((r) => { return { uid: r.Uid, type: r.Name } });
        this.createUserFilter.predicates = this.options.PredicateOptions
            .filter((p) => this.filterModel.SelectedPredicates.indexOf(p.Id) > -1)
            .map((p) => { return { uid: p.Uid, type: p.Name } });
        this.createUserFilter.ancestryMode = this.filterModel.AncestryMode;
        this.createUserFilter.numberOfHops = this.numberOfHops();
        this.createUserFilter.diagramType = this.diagramType;
        this.createUserFilter.name = '';
    }

    apply(e) {
        this.menuitems.forEach(x => {
            if (x.title == this.removeTitle || x.title == this.saveTitle) {
                x.disabled = !this.hasSelectedUserFilter();
                this.cdRef.markForCheck();
            }
        });

        if (!this.hasSelectedUserFilter())
            return;

        let model: AssetBrowserFilterModel = this.filterModel;

        var selectedAssetTypes = this.options.AssetTypeOptions
            .filter((a) => this.selectedFilter.assetTypes.findIndex((f) => f.uid == a.Uid) > -1)
            .map((a) => a.AssetTypeId);

        var selectedPredicates = this.options.PredicateOptions
            .filter(p => this.selectedFilter.predicates.findIndex((f) => f.uid == p.Uid) > -1)
            .map((p) => p.Id);

        var selectedResponsibilityTypes = this.options.ResponsibilityTypeOptions
            .filter((r) => this.selectedFilter.responsibilityTypes.findIndex((f) => f.uid == r.Uid) > -1)
            .map((r) => r.Id);

        model.SelectedAssetTypes = selectedAssetTypes;
        model.SelectedPredicates = selectedPredicates;
        model.SelectedResponsibilityTypes = selectedResponsibilityTypes;

        if (this.selectedFilter.diagramType) {
            model.DiagramType = this.selectedFilter.diagramType;
        }

        if (this.selectedFilter.numberOfHops) {
            if (model.DiagramType == DiagramType.Impact) {
                model.NumberOfImpactHops = this.selectedFilter.numberOfHops;
            }
            else {
                model.NumberOfLineageHops = this.selectedFilter.numberOfHops;
            }
        }

        if (this.selectedFilter.ancestryMode) {
            model.AncestryMode = this.selectedFilter.ancestryMode;
        }

        this.applySavedFilter.emit(model);
    }

    cancel() {
        this.saveFilterModalVisible = false;
        this.deleteFilterModalVisible = false;
    }

    create() {
        this.saveFilterModalWorking = true;
        this.browserService
            .saveUserFilter(this.createUserFilter)
            .subscribe(filter => {
                this.saveFilterModalVisible = false;
                this.saveFilterModalWorking = false;
                this.allFilters.push(filter);
                this.savedFilters = this.allFilters.filter(f => { return f.diagramType == this.diagramType; });
                this.selectedFilter = filter;
                this.menuitems.forEach(x => {
                    if (x.title == this.removeTitle || x.title == this.saveTitle) {
                        x.disabled = !this.hasSelectedUserFilter();
                        this.cdRef.markForCheck();
                    }
                });
                this.messagesService.showInfoMessage($localize`Success`, $localize`Filter added successfully`);
                this.cdRef.markForCheck();
            });
    }

    delete() {
        this.deleteFilterModalWorking = true;
        if (this.hasSelectedUserFilter()) {
            this.browserService
                .deleteUserFilter(this.selectedFilter)
                .subscribe(success => {
                    if (success) {
                        var filters = this.savedFilters;
                        var idx = filters.findIndex(f => f.uid == this.selectedFilter.uid);
                        filters.splice(idx, 1);
                        this.savedFilters = filters.filter(f => true);
                        this.selectedFilter = undefined;
                        this.menuitems.forEach(x => {
                            if (x.title == this.removeTitle || x.title == this.saveTitle) {
                                x.disabled = !this.hasSelectedUserFilter();
                                this.cdRef.markForCheck();
                            }
                        });
                        this.messagesService.showInfoMessage($localize`Success`, $localize`Filter removed successfully`);
                        this.cdRef.markForCheck();
                        this.deleteFilterModalWorking = false;
                        this.deleteFilterModalVisible = false;
                    }
                });
        }
    }

    private hasSelectedUserFilter(): boolean {
        return (this.selectedFilter != undefined && this.selectedFilter != null);
    }

    private load() {
        if (this.allFilters.length == 0) {
            this.browserService
                .getUserFilters()
                .subscribe(filters => {
                    this.allFilters = filters;
                    this.savedFilters = this.allFilters.filter(f => { return f.diagramType == this.diagramType; });
                    this.selectedFilter = this.savedFilters.find(f => f.isDefault == true);
                });
        }
    }

    private showRemove() {
        this.deleteFilterModalVisible = true;
        this.deleteFilterModalWorking = false;
    }

    private update() {
        if (!this.hasSelectedUserFilter())
            return;

        this.createUserFilter = JSON.parse(JSON.stringify(this.selectedFilter));
        this.createUserFilter.assetTypes = this.options.AssetTypeOptions
            .filter(a => this.filterModel.SelectedAssetTypes.indexOf(a.AssetTypeId) > -1)
            .map((a) => { return { uid: a.Uid, class: a.Class } });
        this.createUserFilter.responsibilityTypes = this.options.ResponsibilityTypeOptions
            .filter(r => this.filterModel.SelectedResponsibilityTypes.indexOf(r.Id) > -1)
            .map((r) => { return { uid: r.Uid, type: r.Name } });
        this.createUserFilter.predicates = this.options.PredicateOptions
            .filter(p => this.filterModel.SelectedPredicates.indexOf(p.Id) > -1)
            .map((p) => { return { uid: p.Uid, type: p.Name } });

        this.createUserFilter.ancestryMode = this.filterModel.AncestryMode;
        this.createUserFilter.diagramType = this.filterModel.DiagramType;
        this.createUserFilter.numberOfHops = this.numberOfHops();

        this.browserService
            .saveUserFilter(this.createUserFilter)
            .subscribe(filter => {
                var idx = this.allFilters.findIndex(f => f.uid == filter.uid);
                this.allFilters[idx] = filter;
                this.savedFilters = this.allFilters.filter(f => { return f.diagramType == this.diagramType; });
                this.selectedFilter = filter;
                this.menuitems.forEach(x => {
                    if (x.title == this.removeTitle || x.title == this.saveTitle) {
                        x.disabled = !this.hasSelectedUserFilter();
                        this.cdRef.markForCheck();
                    }
                });
                this.messagesService.showInfoMessage($localize`Success`, $localize`Filter saved successfully`);
                this.cdRef.markForCheck();
            });
    }
} 