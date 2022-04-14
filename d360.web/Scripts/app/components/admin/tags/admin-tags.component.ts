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
import { AdvancedFilterFieldCondition, AdvancedFilterFieldType, ConnectingOperator, Filters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../../models/fieldtype-api.model';
import { Observable, of } from 'rxjs';
import { FilterService } from 'primeng/api';
import { Table } from 'primeng/table';
import { Operator, OperatorString } from '../../../models/operator.model';
import { remove } from 'lodash';
import { tap } from 'rxjs/operators';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService],
    styles: ['table, th, td { border: 1px solid black!important}']
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
    sort: any;

    deletePopupTitle: string = 'Delete Tag';
    editPopupTitle: string = 'Edit Tag';


    public theDeleteCallback: Function;
    public theConsolidateCallback: Function;

    @ViewChild('dt', { static: false }) tableEl: Table;
    private lastSelectedElement: TagType;

    filterFieldList$: Observable<AdvancedFilterFieldType[]> = of([
        {
            Name: 'Value',
            FriendlyName: 'Name',
            Type: new FieldType("Text"),
            Category: ""
        },
        // {
        //     Name: 'UseCount',
        //     FriendlyName: 'Use Count',
        //     Type: new FieldType("Number"),
        //     Category: ""
        // },
        {
            Name: 'CreatedBy',
            FriendlyName: 'Created By',
            Type: new FieldType("Text"),
            Category: ""
        },
    ]);

    constructor(
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

    updateSort(event) {
        this.sort = event;
    }
    onFilterChange(event) {
        if (event != 'globalSearch')
            this.filters.globalSearch = '';

        this.filters[event.prop] = event.value;
    }

    advancedFiltersChanged(event: Filters) {
        this.removeNotValidFilterOption(event);
        const connectingOperator = this.findOutTheConnectingOperator(event);

        if (connectingOperator === ConnectingOperator.Or) {
            this.filterByOrLogic(event);
        } else {
            this.filterByAndLogic(event);
        }
    }

    removeNotValidFilterOption(event: Filters): void {
        remove(event.data, (filterOption: AdvancedFilterFieldCondition) => {
            return filterOption.markForDeletion || !filterOption.field;
        });
    }
    
    // should return advanced filter connectin operator 'or', 'and' or null
    findOutTheConnectingOperator(event: Filters): string {
        const regexp = /\'\)\s(\w*)/; // match: ') word
        const match = event.filter.match(regexp);
        if (match) {
            return match[1];
        } 
        return null;
    }

    filterByAndLogic(event: Filters): void {
        let tagsForFiltering = [...this.readOnlyFullListOfTags];
        event.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            if(filterOption.operator === OperatorString.Contains) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return this.isTagFieldContainsValue(tag, filterOption.field, filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.NotContains) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return !this.isTagFieldContainsValue(tag, filterOption.field, filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.Equals) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return this.isDataValueEqualToSearchValue(tag[filterOption.field], filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.NotEquals) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return !this.isDataValueEqualToSearchValue(tag[filterOption.field], filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.StartsWith) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return this.isDataValueStartsWithSearchValue(tag[filterOption.field], filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.EndsWith) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return this.isDataValueEndsWithSearchValue(tag[filterOption.field], filterOption.value);
                });
            } else if(filterOption.operator === OperatorString.Populated) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return this.isDataValuePopulated(tag[filterOption.field]);
                });
            } else if(filterOption.operator === OperatorString.NotPopulated) {
                tagsForFiltering = tagsForFiltering.filter((tag: TagType) => {
                    return !this.isDataValuePopulated(tag[filterOption.field]);
                });
            } else {
                console.warn(`Unknown filter operator: '${filterOption.operator}' in and logic`);
            }
        });
        this.tags = tagsForFiltering;
    }

    filterByOrLogic(event: Filters): void {
        let filterResult = [];
        let fullListOfTags = [...this.readOnlyFullListOfTags];

        event.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            if(filterOption.operator === OperatorString.Contains) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return this.isTagFieldContainsValue(tag, filterOption.field, filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.NotContains) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return !this.isTagFieldContainsValue(tag, filterOption.field, filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.Equals) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return this.isDataValueEqualToSearchValue(tag[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.NotEquals) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return !this.isDataValueEqualToSearchValue(tag[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.StartsWith) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return this.isDataValueStartsWithSearchValue(tag[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.EndsWith) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return this.isDataValueEndsWithSearchValue(tag[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.Populated) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return this.isDataValuePopulated(tag[filterOption.field]);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else if(filterOption.operator === OperatorString.NotPopulated) {
                const filteredTags = remove(fullListOfTags, (tag: TagType) => {
                    return !this.isDataValuePopulated(tag[filterOption.field]);
                });
                filterResult = [...filterResult, ...filteredTags];
            } else {
                console.warn(`Unknown filter operator: '${filterOption.operator}' in or logic`);
            }
        });
        this.tags = filterResult;
    }

    isTagFieldContainsValue(tag: TagType, field: string, value: string): boolean {
        return tag[field].match(new RegExp(value, 'i'));
    }

    isDataValueEqualToSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase() === searchValue.toLowerCase();
    }

    isDataValueStartsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().startsWith(searchValue.toLowerCase());
    }

    isDataValueEndsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().endsWith(searchValue.toLowerCase());
    }

    isDataValuePopulated(dataValue: string): boolean {
        return dataValue.length > 0;
    }

    randomDate(start, end) {
        return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
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
            tag['CreatedBy'] = tag.CreatedByFirstName + tag.CreatedByLastName;
        });
    }
    

    private deselectElement(element: HTMLElement) {
        var trElement = this.getTrElement(element);

        trElement.classList.remove('p-highlight');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi-check');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi');
        trElement.querySelector('div.p-checkbox-box').classList.remove('p-state-active');
        trElement.querySelector('div.p-checkbox-box').classList.remove('p-highlight');
    }

    private selectElement(element: HTMLElement) {
        var trElement = this.getTrElement(element);

        trElement.classList.add('p-highlight');
        trElement.querySelector('span.p-checkbox-icon').classList.add('pi-check');
        trElement.querySelector('span.p-checkbox-icon').classList.add('pi');
        trElement.querySelector('div.p-checkbox-box').classList.add('p-state-active');

    }

    private getTrElement(element: HTMLElement) {
        if (element.tagName === "TR")
            return element;

        else
            return this.getTrElement(element.parentElement);
    }

    private clearAllSelectedItems(element: any) {
        var nodeList = this.tableEl.el.nativeElement.querySelectorAll("tr.p-highlight");
        Array.from(nodeList)
            .forEach(x => {
                this.deselectElement(x as HTMLElement);
            });
        if (nodeList.length == 0)
            this.selectElement(element);

    }

    selectSingleItem(event: MouseEvent, item: TagType, element: ElementRef = null) {
        this.editPopupTitle = 'Edit Tag';


        //p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
        if (event && element) {
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
                if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                    this.selected = this.selected.filter(x => x.uid != item.uid);
                    var el = (<any>(event.target)).parentNode;
                    el = (el.nodeName === "TD") ? el.parentNode : el;
                    this.deselectElement(el);
                }
                else {
                    this.selected.push(item);
                    var el = (<any>(event.target)).parentNode;
                    el = (el.nodeName === "TD") ? el.parentNode : el;
                    this.selectElement(el);
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
                        this.selectElement(tableRows[i]);
                    }
                }

                this.lastSelectedElement = item;
                return;
            }

        }
        let target = (<any>(event.target));
        if (element && target.nodeName !== "P-TABLECHECKBOX") {
            var el = (<any>(event.target));
            if (el.nodeName === "I")
                el = el.parentNode.parentNode.parentNode; //gets <a>-><div>-><td>
            if (el.nodeName === "A")
                el = el.parentNode.parentNode; //gets <div>-><td>
            el = (el.nodeName === "TD") ? el.parentNode : el;
            this.clearAllSelectedItems(el);
            this.selected = [];
            this.selected.push(item);
            this.lastSelectedElement = item;
        } else {
            if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                this.selected = this.selected.filter(x => x.uid != item.uid);
                var el = (<any>(event.target)).parentNode;
                el = (el.nodeName === "TD") ? el.parentNode : el;
                this.deselectElement(el);
            }
            else {
                this.selected.push(item);
                var el = (<any>(event.target)).parentNode;
                this.selectElement(el);
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
        this.editPopupTitle = 'Add Tag';
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
                    msg = `${result.Value} succesfully created`;
                }
                else {
                    msg = `${result.Value} succesfully updated`;
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
            this.deletePopupTitle = this.selected.length == 1 ? 'Delete Tag' : 'Delete Tags';
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

                    this.messagesService.showInfoMessage("Success", "Tag consolidation succesfull");

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
        this.tagsService.exportTags(this.filters, this.sort);
    }

    private mutateTags(mutator: (tags: TagType[]) => void) {
        const draft = this.tags.slice();
        mutator(draft);
        this.tags = draft;
    }
};
