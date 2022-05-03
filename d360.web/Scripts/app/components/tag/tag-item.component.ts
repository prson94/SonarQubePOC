import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AssetAction, EditFormData, DeleteFormData } from '../../models/secondaryNav.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { TagService } from '../../services/tag.service';
import { TagType, TagDetail, TagItem, TagDetailResponse } from '../../models/tag.model';
import { Location } from '@angular/common';
import { AuthenticationService } from '../../services/authentication.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { CompanySettingsService } from '../../services/settings.service';
import { SemanticType } from '../../models/semantic-type.model';
import { DataProfileService } from '../../services/dataprofile.service';
import { SelectAssetService } from '../../services/select-asset.service';
import { Observable, of, Subscription } from 'rxjs';
import { AssetDetailClickEvent, LinkClickInterceptor } from '../../services/href-click-service';
import { tap } from 'rxjs/operators';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../models/fieldtype-api.model';
import { UiAdvancedFiltering } from '../../services/ui-advanced-filtering.service';
import {uniqWith as _uniqWith, isEqual as _isEqual} from 'lodash';
import { Table } from 'primeng/table';
import { SearchService } from '../../services/search.service';


@Component({
    selector: 'd3s-tag-item',
    providers: [RulesService, PermissionsService, TagService, GridDefinitionService, AuthenticationService, DataProfileService],
    templateUrl: 'tag-item.component.html',
    styleUrls: ['./tag-item.component.less'],
    host: { 'class': 'gov-detail-page' }
})

export class TagItemComponent extends BaseComponent implements OnInit, OnDestroy {
    @ViewChild('dt') table: Table;
    routeParamsSubscription: any;
    tagUid: number;
    tag: TagType;
    tagUsage: TagDetail[];
    readOnlyFullListOfTagUsage: ReadonlyArray<TagDetail> = [];
    selection: TagDetail;
    advancedFilter: string = '';
    
    // sidepanel properties
    sidePanelOpen: boolean = false;
    sidePanelTab: string;
    hasProfiling: boolean = false;
    sidePanelStorageKey: string;
    sidePanelLoading: boolean = false;
    dataProfile: any;
    secondarySidePanelOpen: boolean;
    semanticType: SemanticType;
    selectedReferenceItem: any;
    selectedTag: any;
    selectedAsset: any;
    hrefSub: Subscription;

    private currentAreaName: string;
    private isAdmin: boolean = false;
    private backUrl: string;

    filters: any = { globalSearch: '', DisplayValue: '', AssetType: '', TagsAsString: '' };
    sort: any;

    private sub: any;
    actions: AssetAction;

    filterFieldList$: Observable<AdvancedFilterFieldType[]> = of([
        {
            Name: 'DisplayPath',
            FriendlyName: 'Asset',
            Type: new FieldType("Path"),
            Category: "",
            RemovePopulatedOperator: true
        },
        {
            Name: 'AssetType',
            FriendlyName: 'Asset Type',
            Type: new FieldType("Path"),
            Category: "",
            RemovePopulatedOperator: true
        },
        {
            Name: 'AddedByUid',
            FriendlyName: 'Added By',
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this),
            RemovePopulatedOperator: true
        },
        {
            Name: 'CreatedOn',
            FriendlyName: 'Date Added',
            Type: new FieldType("DateTime"),
            Category: "",
            RemovePopulatedOperator: true
        },
    ]);

    constructor(private route: ActivatedRoute,
        private uiAdvancedFiltering: UiAdvancedFiltering,
        private searchService: SearchService,
        private router: Router,
        private loc: Location,
        private dataProfileService: DataProfileService,
        protected tagsService: TagService,
        protected selectAssetService: SelectAssetService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private authService: AuthenticationService,
        private linkClickInterceptor: LinkClickInterceptor,
        private ref: ChangeDetectorRef
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((assetDetailClickEvent: AssetDetailClickEvent) => {
            this.linkClickInterceptor.handleEvent(this, assetDetailClickEvent);
        });
    }

    get panelApplies(): boolean {
        if (this.selection == null || this.sidePanelTab === 'detail') {
            return true;
        }
        if (this.selection != null && this.sidePanelTab === 'dataprofile') {
            return this.selection.HasProfiling;
        }
    }

    onSearch(searchString: string): void {
        this.searchService.serachTableLocally(this.table, searchString);
    }

    getFilterValues(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
        const addedBy: {name: string, value: string}[] = this.readOnlyFullListOfTagUsage.map((taggedAsset: TagDetail) => {
            return {name: taggedAsset.AddedBy, value: taggedAsset.AddedByUid};
        });
        const uniqAddedBy = _uniqWith(addedBy, _isEqual)
            .filter((s: {name: string, value: string}) => s.name.toLowerCase().includes(params.filter?.toLowerCase() ?? ""));

        if(uniqAddedBy.length === 1 && uniqAddedBy[0].name === '') {
            return of({
                items: [],
                count: 0
            });
        } else {
            return of({
                items: uniqAddedBy,
                count: uniqAddedBy.length
            });
        }
    }

    advancedFiltersChanged(event: Filters): void {
        this.advancedFilter = event.filter;
        this.tagUsage = this.uiAdvancedFiltering.runFiltering(this.readOnlyFullListOfTagUsage, event);
    }

    selectAsset(event: any) {
        this.selectAssetService.selectAsset(event, this);
    }

    secondaryPanelOpen(event: any) {
        this.secondarySidePanelOpen = true;
        this.semanticType = event.semanticType;
    }

    updateSort(event) {
        this.sort = event;
    }
    onFilterChange(event) {
        this.filters[event.prop] = event.value;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.tagUid = params['tagUid'];

            this.secondaryNavService.clearCurrentObject();

            this.logAction('open', 'Tag', this.tagUid);
            this.isLoading = true;

            this.loadPermissions(this.permissionsService, "Tag", this.tagUid)
                .then(p => {
                    if (this.hasModifyAssetPermissions() && this.hasDeleteAssetPermissions()) {
                        this.isAdmin = true;
                    }
                    this.load();
                });



        });


    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        this.secondaryNavService.clearActions();
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
        this.tagsService.getTagByUid(this.tagUid.toLocaleString())
            .subscribe(result => {
                if (result) {
                    this.tag = result;
                    this.setObjectInfo('Tag', this.tagUid);
                    this.buildBreadcrumb();
                    this.setBrowserTitle(this.titleService, this.tag.Value);

                    this.setObjectInfo(
                        'Tag',
                        this.tagUid,
                        this.tag.Value,
                        null,
                        null,
                        this.tag.uid
                    );


                    if (this.isAdmin) {

                        this.setCommonSecondaryNavTabs({ hasAudit: true });

                        if (this.auditSidebar) {
                            this.auditSidebar.url = `/sidebar/audit/Tag/${this.tagUid}`;
                        }
                    }
                    else {
                        this.setCommonSecondaryNavTabs({ hasAudit: false });

                    }
                    this.setActions();

                    this.secondaryNavService.showHeader(true);

                    this.tagsService.getTagDetails(this.tag.uid).pipe(
                        tap((tagDetailResponse: TagDetailResponse) => {
                            this.restoreNecessaryFieldsFromTagToAsset(tagDetailResponse.items);
                        })
                    ).subscribe((data: TagDetailResponse): void => {
                        this.tagUsage = data.items;
                        if (this.tagUsage.length > 0) {
                            this.selection = this.tagUsage[0];
                        }
                        this.tagUsage.forEach(tu => {
                            tu.TagsAsString = tu.Tags.map(x => x.Value).join('|');
                        })
                        this.readOnlyFullListOfTagUsage = [...this.tagUsage];
                        this.isLoading = false;
                    });


                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.currentAreaName = "Tags";
                    let areaBreadcrumb = new Breadcrumb(
                        this.currentAreaName, ``
                    );

                    let itemBreadcrumb = new Breadcrumb(
                        this.tag.Value,
                        `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${this.tag.uid}`
                    )

                    this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
                    this.headerBreadcrumbService.showBreadcrumb(itemBreadcrumb);
                }
                else {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);

                }

            },
                err => {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                });


    }

    restoreNecessaryFieldsFromTagToAsset(tagDetails: TagDetail[]): void {
        tagDetails.forEach((tagDetail: TagDetail): void => {
            const selectedTag = tagDetail?.Tags.find((tag: TagItem) => {
                return String(tag.uid) === String(this.tagUid);
            });
            const addedByFirstName = selectedTag?.CreatedByFirstName;
            const addedByLastName = selectedTag?.CreatedByLastName;
            if (addedByFirstName || addedByLastName) {
                tagDetail['AddedBy'] = `${addedByFirstName} ${addedByLastName}`;
            } else {
                tagDetail['AddedBy'] = ``;
            }
            tagDetail['AddedByUid'] = `${selectedTag?.CreatedByUid}`;
            tagDetail['CreatedOn'] = `${selectedTag?.CreatedOn}`;
        });
    }

    buildBreadcrumb() {
        this.secondaryNavService.setCurrentArea(this.tag.Value, 'fa-tag', 'Tagged Assets');

    }

    formatValue(item: TagDetail) {
        return item.AssetType.replace(':', ` <i class='fa fa-angle-right'></i> `);
    }

    openTagPage(item: TagItem) {
        if (item.Uid != this.tagUid) {
            this.openTagPageByID(item.Uid);
        }
    }

    openTagPageByID(id) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${id}`]);
    }

    export() {
        this.tagsService.exportTagsByUid(this.tag.uid, this.sort, this.filters, this.advancedFilter);
    }

    setActions() {
        this.actions = new AssetAction();
        this.actions.isVisible = true;
        this.actions.showDelete = false;
        this.actions.showEdit = false;
        this.actions.showBack = true;
        this.actions.type = 'TAG';

        this.actions.backCallback = this.onActionBackClick.bind(this);

        if (this.isAdmin) {
            this.actions.showEdit = true;
            this.actions.editCallback = this.onActionEditClick.bind(this);
            let editAction: EditFormData = new EditFormData();
            editAction.title = 'Edit Tag';
            editAction.closeClick = this.onActionEditCloseClick.bind(this);
            editAction.selected = { uid: this.tag.uid, Value: this.tag.Value, UseCount: this.tag.UseCount };
            editAction.isModalVisible = false;
            editAction.modalTitle = "Edit Tag";
            editAction.objectID = this.tag.uid;
            editAction.objectType = 'Tag';
            editAction.saveClick = this.saveTag.bind(this);
            editAction.showAsModal = true;
            this.actions.edit = editAction;

            this.actions.showDelete = true;
            let deleteAction: DeleteFormData = new DeleteFormData();
            deleteAction.callback = this.deleteCallback.bind(this);
            deleteAction.item = { uid: this.tag.uid, Value: this.tag.Value, UseCount: this.tag.UseCount };
            deleteAction.modalTitle = 'Delete Tag';
            deleteAction.isModalVisible = false;
            deleteAction.showAsModal = true;
            this.actions.deleteCallback = this.onActionDeleteClick.bind(this);
            this.actions.delete = deleteAction;
        }

        this.secondaryNavService.setActionTitleItems(this.actions);
    }



    onActionEditCloseClick() {
        if (this.actions) {
            this.actions.edit.isModalVisible = false;
            this.secondaryNavService.setActionTitleItems(this.actions);
        }
    }

    onActionEditClick() {
        this.actions.edit.isModalVisible = true;
        this.secondaryNavService.setActionTitleItems(this.actions);
    }

    onActionDeleteCloseClick() {
        if (this.actions) {
            this.actions.delete.isModalVisible = false;
            this.secondaryNavService.setActionTitleItems(this.actions);
        }
    }

    onActionDeleteClick() {
        this.actions.delete.isModalVisible = true;
        this.secondaryNavService.setActionTitleItems(this.actions);
    }

    onActionBackClick() {
        this.loc.back();
    }

    deleteCallback() {
        let tagForDelete: TagType[] = [];
        tagForDelete.push(this.tag);
        this.tagsService.deleteTags(tagForDelete).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.onActionBackClick();

            }, err => this.showMessageForResult(this.messagesService, err));
    }

    saveTag(event) {

        if (event.additionalOption && event.additionalOption.code) {
            let arr: string[] = [];
            arr.push(event.item.uid);
            this.consolidateTags(event.additionalOption.code, arr);
            return;
        }

        this.tagsService.saveTag(event.item)
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `${result.Value} succesfully created`;
                }
                else {
                    msg = `${result.Value} succesfully updated`;
                }
                this.tag = event.item;
                this.showMessageForResult(this.messagesService, result, msg);
                this.secondaryNavService.setCurrentArea(this.tag.Value, 'fa-tag', 'Tagged Assets');
                this.setBrowserTitle(this.titleService, this.tag.Value);

                this.tagUsage.forEach(detail => {
                    detail.Tags.forEach(t => {
                        if (Number(t["uid"]) === Number(this.tagUid)) {
                            t.Value = event.item.Value;
                        }
                    });
                });

                this.onActionEditCloseClick();
                this.ref.markForCheck();

            });
    }

    consolidateTags(parentUid: string, childrenUids: string[]) {
        this.tagsService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {
                    this.messagesService.showInfoMessage("Success", "Tag consolidation succesfull");
                    this.onActionEditCloseClick();
                    this.openTagPageByID(parentUid);

                }
            }, err => {
                this.showMessageForResult(this.messagesService, err);

            });
    }

};