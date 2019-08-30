import { CommonModule, DatePipe } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit, ViewChild, ElementRef, ChangeDetectorRef, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TagType, TagApiModel } from '../../../models/tag.model';
import { TagService } from '../../../services/tag.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CoreModule } from '../core.module';


@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
    providers: [TagService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class TagView extends AdminBaseComponent implements OnInit {
    public theDeleteCallback: Function;
    @Input() data: any;
    @Input() isEditable: boolean = false;
    @Input() assetUID: string;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private tags: any[];
    private tagID: any;
    selected: TagType[] = [];
    private editPopupTitle: string = 'Edit Tag';
    private deletePopupTitle: string = 'Delete Tag';
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;
    error: any;

    private tagTooltip: TagType;
    private isTooltipLoaded: boolean = false;

    constructor(private tagService: TagService, private messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSidebarService: RightSidebarService, private ref: ChangeDetectorRef) {
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
        //this.getTags();
        this.selected = this.tags;
    }

    getTagUrl(tag: any, event: MouseEvent) {
        if (this.isEditable != true && this.showDelete == false)
            this.openTagPage(event, `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${tag.uid.toString().toLowerCase()}`);
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
            this.selected.push(tag);
            this.deleteTags(tag);
            this.deletePopupTitle = this.selected.length == 1 ? 'Delete Tag' : 'Delete Tags';
        }
    }

    closeEditor() {
        this.showEditor = false;
        this.editPopupTitle = 'Edit Tag';
        this.selected = [];
    }

    add() {
        this.selected = [];
        this.editPopupTitle = 'Add Tag';
        this.showEditor = true;
    }

    saveTag(event) {
        var tags = Array<TagApiModel>();
        let tag = new TagApiModel();
        tag.AssetUID = this.assetUID;
        tag.TagName = event.item.Value;
        tags.push(tag);
        this.tagService.createAssetTag(tags)
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `${event.item.Value} succesfully added`;
                }
                this.showMessageForResult(this.messagesService, result, msg);
                if (event.item.uid == undefined) {
                    this.tags.push(event.item);
                }
                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));

                this.selected = [];
                event.item.UseCount = 0;
                this.selected.push(event.item);

            });
        this.showEditor = false;
    }

    deleteTags(selectedTag) {
        this.tagID = this.selected[0].uid;
        var tags = Array<TagApiModel>();
        let tag = new TagApiModel();
        tag.AssetUID = this.assetUID;
        tag.TagUID = this.tagID;
        tags.push(tag);
        this.tagService.deleteAssetTag(tags).
            subscribe(result => {
                let msg: string = '';
                msg = `Tag succesfully removed`;
                this.showMessageForResult(this.messagesService, result,msg);    
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.selected.forEach(t => {
                        this.tags.splice(this.findTagIndex(t.TooltipID), 1);
                    })
                    this.selected = [];
                }
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
    }

    setVisibility() {
        this.container.nativeElement.querySelectorAll('.tag-item-wrapper')
            .forEach((x, index) => {
                if (x.innerText != "Click to add...") {
                    if (!this.isShowAll && index > 9) {
                        x.closest('a').classList.add('hide');
                    }
                    else {
                        x.closest('a').classList.remove('hide');
                    }
                }
            });
    }

    ngAfterViewInit() {

        if (this.container) {
            let parent = this.container.nativeElement.closest('td')
                ? this.container.nativeElement.closest('td') : this.container.nativeElement.closest('div');

            let ofWidth = parent ? parent.offsetWidth - 10 : 500;

            this.container.nativeElement.style.width = ofWidth + 'px';
            this.container.nativeElement.querySelectorAll('.tag-item-wrapper')
                .forEach((x) => {
                    if (x.offsetWidth > ofWidth) {
                        x.setAttribute('original-width', x.offsetWidth);
                        x.style.maxWidth = (ofWidth - 30) + 'px';
                        x.classList.add('too-long');
                        x.setAttribute('max-width', ofWidth - 30);
                    }
                });

            this.setVisibility();

        }

    }

    openTagPage(event: MouseEvent, url: string) {
        window.open(url, "_blank");
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
}
@NgModule({
    declarations: [
    ],
    exports: [
    ]
    , imports: [
        CommonModule,
        CoreModule
    ]

})

export class TagViewModule { }