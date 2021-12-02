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
import { Array } from 'core-js';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService]
})

export class AdminTagsComponent extends AdminBaseComponent {
    tags: ReadonlyArray<TagType> = []; // This is readonly array, because PrimeNGTable expects immutable data
    selected: TagType[] = [];

    error: any;

    consolidatePromptHTML: string;
    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false
    filters: any = { globalSearch: '', Value: '', UseCount: '' };
    sort: any;

    deletePopupTitle: string = 'Delete Tag';
    editPopupTitle: string = 'Edit Tag';


    public theDeleteCallback: Function;
    public theConsolidateCallback: Function;

    @ViewChild('dt', { static: false }) tableEl: any;
    private lastSelectedElement: TagType;

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
        this.setCommonSecondaryNavTabs(true);

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
    getTags() {
        this.isLoading = true;
        this.tagsService.getTagsList().subscribe(res => {
            if (res && res.length > 0) {
                this.tags = res.sort((a, b) => a.Value.localeCompare(b.Value));
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    private deselectElement(element: HTMLElement) {
        var trElement = this.getTrElement(element);

        trElement.classList.remove('p-highlight');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi-check');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi');
        trElement.querySelector('div.p-checkbox-box').classList.remove('p-state-active');

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
