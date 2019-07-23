import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TagService } from '../../../services/tag.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { TagType } from '../../../models/tag.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService],
})

export class AdminTagsComponent extends AdminBaseComponent {
    tags: TagType[] = [];
    selected: TagType;

    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;


    public theDeleteCallback: Function;

    constructor(private tagsService: TagService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title, rightSidebarService: RightSidebarService,) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Tags";
        this.setCommonItems();
    }

    ngOnInit() {
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/Tag/`
            });
        }
        this.getTags();

        this.theDeleteCallback = this.deleteTag.bind(this);

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getTags() {
        this.isLoading = true;
        this.tagsService.getTagsList().subscribe(res => {
            if (res && res.length > 0) {
                this.tags = res.sort((a, b) => a.Value.localeCompare(b.Value));
                if (this.tags.length > 0) this.selected = this.tags[0];
            }
            this.isLoading = false;
        }, err => this.error = err);
    }


    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.tags.length > 0)
            this.selected = this.tags[0];
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }
    saveTag(event) {
        this.tagsService.saveTag(event.item)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.uid == undefined) {
                    this.tags.push(result);
                }
                else {
                    this.tags[this.findTagIndex(event.item.uid)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }

    deleteTag(uid: string) {
        this.tagsService.deleteTagByUid(uid).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.tags.splice(this.findTagIndex(uid), 1);
                    this.selected = this.tags.length > 0 ? this.tags[0] : null;
                }
                this.showDelete = false;
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    findTagIndex(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }
};
