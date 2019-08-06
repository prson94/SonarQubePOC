import { Component, HostListener } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { TagType, TagItem } from '../../../models/tag.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { forEach } from '@angular/router/src/utils/collection';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService],
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

    constructor(private router: Router, private tagsService: TagService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesObservableService, titleService: Title, rightSidebarService: RightSidebarService, ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Tags";
        this.setCommonItems();
        this.rightSidebarService.setCurrentArea(this.areaName, this.area === 'Tags' ? 'fa-tag' : "fa-tag", this.tabTitle);

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



    selectSingleItem(item: TagType) {
        this.selected = [];
        this.selected.push(item);
        this.editPopupTitle = 'Edit Tag';
    }


    closeEditor() {
        this.showEditor = false;
        if (this.selected.length == 0 && this.tags.length > 0)
            this.selectSingleItem(this.tags[0]);
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
                    this.messagesService.showInfoMessage("Success", "Tagging status successfully changed!");
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
