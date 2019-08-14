import { Component, HostListener, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { TagType, TagItem } from '../../../models/tag.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { forEach } from '@angular/router/src/utils/collection';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Node } from '@angular/compiler/src/render3/r3_ast';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
declare var CompanySettings;

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService]
})

export class AdminTagsComponent extends AdminBaseComponent {
    tags: TagType[] = [];
    selected: TagType[] = [];

    error: any;

    consolidatePromptHTML: string;
    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false

    private deletePopupTitle: string = 'Delete Tag';
    private editPopupTitle: string = 'Edit Tag';


    public theDeleteCallback: Function;
    public theConsolidateCallback: Function;

    @ViewChild('dt') tableEl: any;
    private lastSelectedElement: TagType;

    constructor(private router: Router, private tagsService: TagService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesObservableService, titleService: Title, rightSidebarService: RightSidebarService, ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Tags";
        this.setCommonItems();
        this.tabTitle = 'Tags';
        this.rightSidebarService.setCurrentArea(this.areaName, 'fa-tag', this.tabTitle);

    }

    ngOnInit() {
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/Tag/0`
            });
        }
        this.getTags();

        this.theDeleteCallback = this.deleteTags.bind(this);
        this.theConsolidateCallback = this.consolidateTags.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getTags() {
        this.isLoading = true;
        this.tagsService.getTagsList().subscribe(res => {
            if (res && res.length > 0) {
                this.tags = res.sort((a, b) => a.Value.localeCompare(b.Value));
                if (this.tags.length > 0) this.selected.push(this.tags[0]);
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    private deselectElement(element: any) {
        element.classList.remove('ui-state-highlight');
        element.querySelector('span.ui-chkbox-icon').classList.remove('pi-check');
        element.querySelector('span.ui-chkbox-icon').classList.remove('pi');
        element.querySelector('div.ui-chkbox-box').classList.remove('ui-state-active');

    }
    private selectElement(element: any) {
        element.classList.add('ui-state-highlight');
        element.querySelector('span.ui-chkbox-icon').classList.add('pi-check');
        element.querySelector('span.ui-chkbox-icon').classList.add('pi');
        element.querySelector('div.ui-chkbox-box').classList.add('ui-state-active');

    }

    private clearAllSelectedItems() {
        this.tableEl.el.nativeElement.querySelectorAll("tr.ui-state-highlight")
            .forEach(x => {
                this.deselectElement(x);
            });

    }

    selectSingleItem(event: MouseEvent, item: TagType, element: ElementRef = null) {
        this.editPopupTitle = 'Edit Tag';


        //p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
        if (event && element) {
            if (event.ctrlKey && !event.shiftKey) {
                if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                    this.selected = this.selected.filter(x => x.uid != item.uid);
                    var el = (<any>(event.target)).parentNode;
                    this.deselectElement(el);
                }
                else {
                    this.selected.push(item);
                    var el = (<any>(event.target)).parentNode;
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
                    if (!tableRows[i].classList.contains('ui-state-highlight')) {
                        this.selected.push(this.tags[i]);
                        this.selectElement(tableRows[i]);
                    }
                }

                this.lastSelectedElement = item;
                return;
            }

        }

        if (element)
            this.clearAllSelectedItems();

        this.selected = [];
        this.selected.push(item);

        this.lastSelectedElement = item;
    }


    closeEditor() {
        this.showEditor = false;
        if (this.selected.length == 0 && this.tags.length > 0)
            this.selectSingleItem(null, this.tags[0]);
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
                    this.tags.push(result);
                }
                else {
                    this.tags[this.findTagIndex(event.item.uid)].Value = event.item.Value;
                }
                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));

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
                        this.tags.splice(this.findTagIndex(t.uid), 1);
                    })
                    this.selected = [];
                }
                this.showDelete = false;
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    openDeleteModal() {
        this.deletePopupTitle = this.selected.length == 1 ? 'Delete Tag' : 'Delete Tags';
        this.showDelete = true;
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

    tagStateChanged(state: boolean) {
        this.tagsService.setTaggingStatus(state)
            .subscribe(result => {
                if (result)
                    this.messagesService.showInfoMessage("Success", `Tagging status successfully changed to '${state}'!`);
                    CompanySettings["EnableTagging"] = state.toString();
            }
                , err => {
                    this.showMessageForResult(this.messagesService, err);
                })
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

    private export() {
        this.tagsService.exportTags();
    }

};
