import { CommonModule, DatePipe } from '@angular/common';
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
import {
    AutoCompleteModule,
    TreeModule,
    OverlayPanelModule,
    SharedModule,
    DialogModule,
} from 'primeng/primeng';
import { debounceTime } from 'rxjs/operators';
import { AuthenticationService } from '../../../services/authentication.service';


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
    @Input() assetUID: string;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private searchResults: any[] = [];
    private tags: any[];
    private searchTags: any[];
    private tagsLoading = false;
    private tagID: any;
    existingTag: boolean = false;
    selected: TagType[] = [];
    private selectedtag: TagType = new TagType();
    private editPopupTitle: string = 'Edit Tag';
    private deletePopupTitle: string = 'Delete Tag';
    private isShowAll: boolean = false;
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
        this.selected = this.tags;
    }

    addTag(event,tag) {
        this.selectedtag.Value = tag.name;
        this.selectedtag.uid = tag.code;
        this.tagInput.nativeElement.value = tag.name;
        this.search(event,tag.name);
    }

    show(event, searchPanel, target) {
        searchPanel.show(event);
        let lineDims = target.getBoundingClientRect();
        window.setTimeout(() => {

            let dispPanel = searchPanel.el.nativeElement.children[0];
            dispPanel.style.top = (lineDims.bottom + dispPanel.getBoundingClientRect().height + 1) + "px";
            dispPanel.style.left = (lineDims.left) + "px";
            dispPanel.style.display = "table";
            dispPanel.style.position = "fixed";
            dispPanel.style.maxWidth = (window.innerWidth - lineDims.left) + "px";
        }, 150);
    }

    search(event, searchValue) {
        if (event.key != "Enter" && event.key != undefined) {
            this.selectedtag.Value = undefined;
            this.selectedtag.uid = undefined;
        }
        if (event.key == "Enter") {
            if (this.selectedtag.Value == undefined)
                this.selectedtag.Value = searchValue;
            if (this.selectedtag.Value != "")
                this.saveTag({ Value: this.selectedtag.Value, uid: this.selectedtag.uid });
        }
        this.tagsLoading = true;
        clearTimeout(this.timeouthandle);
        this.timeouthandle = window.setTimeout(() => {
            this.tagService.searchTagsTypeAhead(searchValue.toLowerCase(), 10)
                .subscribe(res => {
                    if (res && res.length > 0) {
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

    saveTag(event) {
        this.existingTag = false;
        var tags = Array<TagApiModel>();
        let tag = new TagApiModel();
        tag.AssetUID = this.assetUID;
        tag.TagName = event.Value;
        tags.push(tag);
        this.tags.forEach(x => {
            if (x.Value == event.Value) {
                this.existingTag = true;
                this.showEditor = false;
                this.messagesService.showError('Error', 'Tag already assigned to Asset');
            }
        })
        if (!this.existingTag) {
            this.tagService.doesTagExist(event.Value)
                .subscribe(result => {
                    if (result == null) {
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
                                        this.tagInput.nativeElement.value = "";
                                        this.ref.markForCheck();
                                    });
                            });
                    }
                    else {
                        this.tagService.createAssetTag(tags)
                            .subscribe(result => {
                                let msg: string = '';
                                if (result != null) {
                                    msg = `${event.Value} succesfully added to Asset`;
                                }
                                this.showMessageForResult(this.messagesService, result, msg);
                                event.uid = result[0].Uid;
                                this.tags.push(event);
                                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
                                event.UseCount = 0;
                                this.tagInput.nativeElement.value = "";
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

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
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

    private getParentForResizing(element: HTMLElement, tags: string) {
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
    }

    openTagPage(event: MouseEvent, item: any) {
        if ((this.isEditable != true && this.showDelete == false) || (<HTMLElement>event.target).className == 'tag-item-wrapper')
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid.toString().toLowerCase()}`]);
        event.stopPropagation();
    }

    enter(tag: any, el: HTMLElement) {
        this.isTooltipLoaded = false;
        this.tagService.getTagTooltip(tag.uid)
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
        if (this.container) {
            let parent = this.getParentForResizing(this.container.nativeElement, 'TD,DIV');
            if (!parent) {
                console.warn("No suitable parent found for tag resizing!");
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