import { CommonModule } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit, ViewChild, ElementRef, ChangeDetectorRef, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TagType, TagApiModel, Tag } from '../../../models/tag.model';
import { TagService } from '../../../services/tag.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CoreModule } from '../core.module';
import { Router } from '@angular/router';

import { TreeModule } from 'primeng/tree';
import { SharedModule } from 'primeng/shared';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { AutoCompleteModule } from 'primeng/autocomplete';

import { debounceTime } from 'rxjs/operators';
import { AuthenticationService } from '../../../services/authentication.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
    providers: [TagService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:resize)': 'manageWidth()' }
})

export class TagView extends AdminBaseComponent implements OnInit {
    public theDeleteCallback: Function;
    @ViewChild('tagInput') tagInput: ElementRef;
    @Input() data: any;
    @Input() isEditable: boolean = false;
    @Input() allowAddTag: boolean = false;
    @Input() assetUID: string;
    @Input() ignoreResizing: boolean = false;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private inputValue: any;
    private searchResults: any[] = [];
    private tags: any[];
    private searchTags: any[];
    private resultPanel: any;
    private targetPanel: any;
    private tagsLoading = false;
    private tagID: any;
    existingTag: boolean = false;
    selected: TagType[] = [];
    private selectedtag: TagType = new TagType();
    private editPopupTitle: string = 'Edit Tag';
    private deletePopupTitle: string = 'Delete Tag';
    private isShowAll: boolean = false;
    showDeleteOption: boolean = false;
    @ViewChild("container") container: ElementRef;
    error: any;
    timeouthandle: any;

    private tagTooltip: TagType;
    private isTooltipLoaded: boolean = false;

    constructor(private router: Router,
        private tagService: TagService,
        private messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        rightSidebarService: RightSidebarService,
        private ref: ChangeDetectorRef,
        private auth: AuthenticationService) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }
    
    ngOnInit() {
        this.theDeleteCallback = this.deleteTags.bind(this);
        try {
            if (this.data && (typeof this.data == 'string'))
                this.tags = JSON.parse(this.data);
            else this.tags = this.data;
        }
        catch
        {
            console.warn("d3s-tag-view::Error while parsing tags!");
        }
        if (this.tags) {
            this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
        }
        this.selected = this.tags;
    }

    addTag(event,tag) {
        this.tagInput.nativeElement.style.background = "white";
        this.selectedtag.Value = tag.name;
        this.selectedtag.uid = tag.code;
        this.tagInput.nativeElement.value = tag.name;
        this.search(event, tag.name);
        this.saveTag({ Value: this.selectedtag.Value, uid: this.selectedtag.uid });
    }

    show(event, searchPanel, target) {
        this.resultPanel = searchPanel;
        this.targetPanel = target;
        searchPanel.show(event);
        this.tagInput.nativeElement.style.background = "white";
        target.style.background = "white";
        target.style.border = "1px solid #66A9D6";
        let lineDims = target.getBoundingClientRect();
        window.setTimeout(() => {
            let dispPanel = searchPanel.el.nativeElement.children[0];
            dispPanel.style.maxWidth = (window.innerWidth - lineDims.left - 5) + "px";
        }, 150);
    }

    search(event, searchValue) {
        this.tagsLoading = true;
        clearTimeout(this.timeouthandle);
        this.timeouthandle = window.setTimeout(() => {
            this.tagService.searchTagsTypeAhead(searchValue, 10)
                .subscribe(res => {
                    if (res && res.length > 0) {
                        this.searchResults = res.sort((a, b) => a.name.localeCompare(b.name));
                        this.tagsLoading = false;
                        this.ref.markForCheck();
                    }
                    else if (res && res.length == 0) {
                        this.searchResults = res;
                        this.tagsLoading = false;
                        this.ref.markForCheck();
                    }
            }, err => this.error = err);
        }, 400);
    }

    getTags() {
        this.isLoading = true;
        this.tagService.getTagsList().subscribe(res => {
            if (res && res.length > 0) {
                this.tags = res.sort((a, b) => a.Value.localeCompare(b.Value));
                if (this.tags.length > 0) this.selected.push(this.tags[0]);
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    openDeleteModal(tag: any) {
        if (this.isEditable == true) {
            this.deleteTags(tag);
        }
    }

    checkKey(event,value) {
        if (event.key == "Enter" ) {
            event.name = value;
            this.saveTag(event);
        }
    }

    saveTag(event) {
        this.existingTag = false;
        var tags = Array<TagApiModel>();
        let tag = new TagApiModel();
        tag.AssetUID = this.assetUID;
        tag.TagName = event.name;
        tags.push(tag);
        this.tags.forEach(x => {
            if (x.Value == event.name) {
                this.existingTag = true;
                this.showEditor = false;
                this.messagesService.showError('Error', 'Tag already assigned to Asset');
            }
        })
        if (event.name.includes("|")) {
            this.existingTag = true;
            this.messagesService.showError('Error', "Tag can't contain | character");
        }
        if (!this.existingTag) {
            this.tagService.doesTagExist(event)
                .subscribe(result => {
                    if (result == null) {
                        event.Value = event.name;
                        this.tagService.saveTag(event)
                            .subscribe(result => {
                                let msg: string = '';
                                if (event.uid == undefined) {
                                    msg = `${event.Value} succesfully created`;
                                }
                                this.showMessageForResult(this.messagesService, result, msg);
                                this.tagService.createAssetTag(tags)
                                    .subscribe(result => {
                                        let msg: string = '';
                                        if (event.uid == undefined) {
                                            msg = `${event.Value} succesfully added to Asset`;
                                        }
                                        this.showMessageForResult(this.messagesService, result, msg);
                                        if (event.uid == undefined) {
                                            event.uid = result[0].Uid;
                                            this.tags.push(event);
                                        }
                                        this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
                                        event.UseCount = 0;
                                        this.searchResults = [];
                                        this.inputValue = "";
                                        this.ref.markForCheck();
                                    });
                            });
                    }
                    else {
                        this.tagService.createAssetTag(tags)
                            .subscribe(result => {
                                let msg: string = '';
                                if (result != null) {
                                    msg = `${event.name} succesfully added to Asset`;
                                }
                                this.showMessageForResult(this.messagesService, result, msg);
                                event.uid = result[0].Uid;
                                event.Value = event.name;
                                this.tags.push(event);
                                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
                                event.UseCount = 0;
                                this.searchResults = [];
                                this.inputValue = "";
                                this.ref.markForCheck();

                            });
                    }
                });
        }
        this.showEditor = false;
    }

    deleteTags(selectedTag) {
        this.tagID = selectedTag.uid;
        var tags = Array<TagApiModel>();
        let tag = new TagApiModel();
        tag.AssetUID = this.assetUID;
        tag.TagUID = this.tagID;
        tags.push(tag);
        this.tagService.deleteAssetTag(tags).
            subscribe(result => {
                let msg: string = '';
                msg = `Tag succesfully removed`;
                this.showMessageForResult(this.messagesService, result, msg);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.tags.splice(this.findTagIndex1(selectedTag.uid), 1);
                }
                this.ref.markForCheck();

            }, err => this.showMessageForResult(this.messagesService, err));
    }

    showRemoveTag() {
        if (this.isEditable) {
            if (!this.auth.isAdmin || !this.hasModifyAssetPermissions())
                this.showDeleteOption = false;
            if (this.auth.isAdmin || this.hasModifyAssetPermissions())
                this.showDeleteOption = true;
            if (!this.showDeleteOption) {
                var tagElements = this.container.nativeElement.querySelectorAll('.tagging');
                (function () {
                    if (typeof NodeList.prototype.forEach === "function") return false;
                    tagElements.forEach = Array.prototype.forEach;
                })();
                tagElements.forEach(tagEle => {
                    this.tags.forEach(tag => {
                        this.tagService.getAssetTagDetails(tag.TooltipID, this.assetUID).
                            subscribe(result => {
                                if (tagEle.children[1].innerText.trim() == tag.Value.trim()) {
                                    if (result == CurrentResourceID)
                                        this.showDeleteOption = true;
                                    if (result != CurrentResourceID)
                                        tagEle.children[2].parentElement.removeChild(tagEle.children[2]);

                                }
                            }, err => this.showMessageForResult(this.messagesService, err));
                    })
                })
            }

        }
    }

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
    }

    resetValue() {
        this.inputValue = null;
    }

    setVisibility() {

        var items = Array.prototype.slice.call(this.container.nativeElement.querySelectorAll('.tag-item-wrapper'), 0);
        if (items.length > 0) {
            for (let index = 0; index < items.length; index++) {
                var aElement = this.getParentForResizing(items[index], 'A');
                if (!this.isShowAll && index > 9) {
                    aElement.classList.add('hide');
                }
                else {
                    aElement.classList.remove('hide');
                }
            }
        }
        this.ref.markForCheck();
    }

    private getParentForResizing(element: HTMLElement, tags: string): HTMLElement {
        var searchFor = tags.split(',');
        var el = null;
        searchFor.forEach(tagName => {
            if (element.parentElement.tagName == tagName) {
                el = element.parentElement;
            }
        });

        if (el) return el;

        if (element.parentElement) {
            return this.getParentForResizing(element.parentElement, tags);
        }
        return null;
    }

    ngAfterViewInit() {
        this.manageWidth();
        this.showRemoveTag();
    }


    openTagPage(event: MouseEvent, item: any) {
        if ((this.isEditable != true && this.showDelete == false) || (<HTMLElement>event.target).className == 'tag-item-wrapper')
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid.toString().toLowerCase()}`]);
        event.stopPropagation();
    }

    public highlight(item, input) {
        if (!input) {
            return item;
        }
        return item.replace(new RegExp(input, "gi"), match => {
            return '<span style="background: #F5FF57;">' + match + '</span>';
        });
    }

    enter(tag: any, el: HTMLElement) {
        this.isTooltipLoaded = false;
        this.tagService.getTagTooltip(tag.uid, this.assetUID)
            .subscribe(t => {
                this.tagTooltip = t[0];
                this.isTooltipLoaded = true;
                this.ref.markForCheck();
            });
    }


    findTagIndex1(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }
    findTagIndex(id: number) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.TooltipID == id) return index;
        }
    }

    manageWidth() {
        if (this.ignoreResizing) return;

        if (this.container) {
            let parent = this.getParentForResizing(this.container.nativeElement, 'TD,DIV');

            if (!parent) {
                console.warn("No suitable parent found for tag resizing!");
            } else if (parent.classList.contains("tagsnomanagewidth")) {
                this.setVisibility();
                return;
            }

            let ofWidth = parent ? parent.offsetWidth - 10 : 500;
            parent.classList.remove('no-text-overflow')
            this.container.nativeElement.style.width = ofWidth + 'px';

            var items = Array.prototype.slice.call(this.container.nativeElement.querySelectorAll('.tag-item-wrapper'), 0);
            if (items.length > 0) {
                for (let i = 0; i < items.length; i++) {
                    var x = items[i];
                    if (x.offsetWidth > ofWidth) {
                        x.setAttribute('original-width', x.offsetWidth);
                        x.style.maxWidth = (ofWidth - 30) + 'px';
                        x.classList.add('too-long');
                        x.setAttribute('max-width', ofWidth - 30);
                    }
                }
            }
            this.setVisibility();
        }
    }
}
@NgModule({
    declarations: [
    ],
    exports: [
    ],
    imports: [
        CommonModule,
        CoreModule,
        AutoCompleteModule,
        TreeModule,
        OverlayPanelModule,
        SharedModule,
        DialogModule
    ]

})

export class TagViewModule { }
