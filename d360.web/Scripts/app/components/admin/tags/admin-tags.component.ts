import { Component, ElementRef, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { TagType } from '../../../models/tag.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../../models/fieldtype-api.model';
import { Observable, of } from 'rxjs';
import { Table } from 'primeng/table';
import { tap } from 'rxjs/operators';
import { UiAdvancedFiltering } from '../../../services/ui-advanced-filtering.service';
import { SearchService } from '../../../services/search.service';
import {uniqWith as _uniqWith, isEqual as _isEqual} from 'lodash';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService]
})

export class AdminTagsComponent extends AdminBaseComponent {
    tags: ReadonlyArray<TagType> = []; // This is readonly array, because PrimeNGTable expects immutable data
    readOnlyFullListOfTags: ReadonlyArray<TagType> = [];
    selected: TagType[] = [];

    error: any;

    consolidatePromptHTML: string;
    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false
    filters: any = { globalSearch: '', Value: '', UseCount: '', DateCreated: '', CreatedBy: '' };
    advancedFilter: string = '';
    sort: any;

    deletePopupTitle: string = $localize`Delete Tag`;
    editPopupTitle: string = $localize`Edit Tag`;


    public theDeleteCallback: Function;
    public theConsolidateCallback: Function;

    @ViewChild('dt', { static: false }) tableEl: Table;
    private lastSelectedElement: TagType;

    filterFieldList$: Observable<AdvancedFilterFieldType[]> = of([
        {
            Name: 'Value',
            FriendlyName: $localize`Name`,
            Type: new FieldType("Text"),
            Category: "",
            RemovePopulatedOperator: true
        },
        {
            Name: 'UseCount',
            FriendlyName: $localize`Use Count`,
            Type: new FieldType("Number"),
            Category: "",
            RemovePopulatedOperator: true
        },
        {
            Name: 'CreatedOn',
            FriendlyName: $localize`Date Created`,
            Type: new FieldType("Date"),
            Category: "",
            RemovePopulatedOperator: true
        },
        {
            Name: 'CreatedBy',
            Type: new FieldType("Lookup"),
            FriendlyName: $localize`Created By`,
            Category: "",
            ValueLoader: this.getFilterValuesForCreatedBy.bind(this),
            RemovePopulatedOperator: true
        },
    ]);

    constructor(
        private uiAdvancedFiltering: UiAdvancedFiltering,
        private searchService: SearchService,
        private router: Router,
        private tagsService: TagService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Tags;
        this.setCommonItems();
        this.tabTitle = StringConstants.Section_Tags;
        this.secondaryNavService.setCurrentArea(this.areaName, 'fa-tag', this.tabTitle);
    }

    ngOnInit() {
        this.setCommonSecondaryNavTabs({ hasAudit: true });

        if (this.auditSidebar) {
            this.auditSidebar.url = `/sidebar/audit/Tag/0`;
        }
        this.getTags();

        this.theDeleteCallback = this.deleteTags.bind(this);
        this.theConsolidateCallback = this.consolidateTags.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getFilterValuesForCreatedBy(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
        const createdBy: string[] = this.readOnlyFullListOfTags.map((tag: TagType) => {
            return tag.CreatedBy;
        });
        const uniqCreatedBy = _uniqWith(createdBy, _isEqual)
            .filter((s: string) => s.toLowerCase().includes(params.filter?.toLowerCase() ?? ""));

        if(uniqCreatedBy.length === 1 && uniqCreatedBy[0].name === '') {
            return of({
                items: [],
                count: 0
            });
        } else {
            return of({
                items: uniqCreatedBy,
                count: uniqCreatedBy.length
            });
        }
    }

    updateSort(event) { 
        this.sort = event;
    }
    onFilterChange(event) {
        if (event != 'globalSearch')
            this.filters.globalSearch = '';

        this.filters[event.prop] = event.value;
    }

    advancedFiltersChanged(event: Filters): void {
        this.advancedFilter = event.filter;
        this.tags = this.uiAdvancedFiltering.runFiltering(this.readOnlyFullListOfTags, event);
    }

    onSearch(searchString: string): void {
        this.searchService.serachTableLocally(this.tableEl, searchString);
    }

    getTags() {
        this.isLoading = true;
        this.tagsService.getTagsList().pipe(
            tap((tags: TagType[]) => {
                this.sortTags(tags);
            }),
            tap((tags: TagType[]) => {
                this.addCreatedByFieldToTags(tags);
            })
        ).subscribe((tags: TagType[]) => {
            if (tags && tags.length > 0) {
                this.tags = tags;
                this.readOnlyFullListOfTags = [...this.tags];
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    sortTags(tags: TagType[]): void {
        tags.sort((a, b) => a.Value.localeCompare(b.Value));
    }

    addCreatedByFieldToTags(tags: TagType[]): void {
        tags.forEach((tag: TagType): void => {
            tag['CreatedBy'] = `${tag.CreatedByFirstName} ${tag.CreatedByLastName}`;
        });
    }

    private triggerRerenderOfSelection() {
        // primeNg library expects us to pass new array whenever we want to change contents of array
        this.selected = this.selected.slice();
    }

    selectSingleItem(event: MouseEvent, item: TagType, element: ElementRef = null) {
        this.editPopupTitle = $localize`Edit Tag`;

        //p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
        if (event && element) {
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
                if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                    this.selected = this.selected.filter(x => x.uid != item.uid);
                    this.triggerRerenderOfSelection();
                }
                else {
                    this.selected.push(item);
                    this.triggerRerenderOfSelection();
                }

                this.lastSelectedElement = item;
                return;
            }
            if (event.shiftKey) {
                var lastIndex = this.tags.indexOf(this.lastSelectedElement);
                if (lastIndex == -1 && this.selected.length == 1) {
                    lastIndex = this.tags.indexOf(this.selected[0]);
                }
                var currentIndex = this.tags.indexOf(item);

                if (lastIndex > currentIndex) {
                    lastIndex += currentIndex;
                    currentIndex = lastIndex - currentIndex;
                    lastIndex -= currentIndex;
                }

                var tableRows = (<any>this.tableEl).el.nativeElement.querySelectorAll('table tbody tr');
                for (var i = lastIndex; i <= currentIndex; i++) {
                    if (!tableRows[i].classList.contains('p-highlight')) {
                        this.selected.push(this.tags[i]);
                        this.triggerRerenderOfSelection();
                    }
                }

                this.lastSelectedElement = item;
                return;
            }

        }
        let target = (<any>(event.target));
        if (element && target.nodeName !== "P-TABLECHECKBOX") {
            this.selected = [];
            this.selected.push(item);
            this.triggerRerenderOfSelection();
            this.lastSelectedElement = item;
        } else {
            if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                this.selected = this.selected.filter(x => x.uid != item.uid);
                this.triggerRerenderOfSelection();
            }
            else {
                this.selected.push(item);
                this.triggerRerenderOfSelection();
            }
            this.lastSelectedElement = item;
        }
    }


    closeEditor() {
        this.showEditor = false;
    }

    openEditor() {
        this.showEditor = true;
    }

    add() {
        this.selected = [];
        this.editPopupTitle = $localize`Add Tag`;
        this.showEditor = true;

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
                    msg = $localize`${result.Value} succesfully created`;
                }
                else {
                    msg = $localize`${result.Value} succesfully updated`;
                }
                this.showMessageForResult(this.messagesService, result, msg);
                if (event.item.uid == undefined) {
                    this.mutateTags(tags => tags.push(result));
                }
                else {
                    this.tags[this.findTagIndex(event.item.uid)].Value = event.item.Value;
                }

                this.mutateTags(tags => tags.sort((a, b) => a.Value.localeCompare(b.Value)));

                this.selected = [];
                event.item.UseCount = 0;
                this.selected.push(event.item);

                this.showEditor = false;

            });
    }

    deleteTags() {
        this.tagsService.deleteTags(this.selected).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.selected.forEach(t => {
                        this.mutateTags(tags => tags.splice(this.findTagIndex(t.uid), 1));
                    })
                    this.selected = [];
                }
                this.showDelete = false;
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    openDeleteModal() {
        window.setTimeout(() => {
            this.deletePopupTitle = this.selected.length == 1 ? $localize`Delete Tag` : $localize`Delete Tags`;
            this.showDelete = true;
        }, 100)

    }

    openConsolidateModal() {
        this.showConsolidate = true;
    }

    consolidateTags(parentUid: string, childrenUids: string[]) {
        this.tagsService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {

                    this.messagesService.showInfoMessage($localize`Success`, $localize`Tag consolidation succesfull`);

                    result.forEach(t => {
                        if (t.UseCount != 0)
                            this.tags[this.findTagIndex(t.uid)].UseCount = t.UseCount;
                        else if (t.uid != parentUid)
                            this.tags = this.tags.filter(x => x.uid != t.uid);
                    });
                }
                this.selected = [];
                this.selected.push(this.tags[0])
                this.showConsolidate = false;
                this.showEditor = false;
            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.showConsolidate = false;
                this.showEditor = false;

            });
    }

    findTagIndex(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }

    openTagDetails(item: TagType) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid}`]);
    }

    export() {
        this.tagsService.exportTags(this.filters, this.sort, this.advancedFilter);
    }

    private mutateTags(mutator: (tags: TagType[]) => void) {
        const draft = this.tags.slice();
        mutator(draft);
        this.tags = draft;
    }
};
