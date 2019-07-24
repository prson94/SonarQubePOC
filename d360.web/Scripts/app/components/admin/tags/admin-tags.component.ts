import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { TagType } from '../../../models/tag.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { forEach } from '@angular/router/src/utils/collection';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-tags',
    templateUrl: 'admin-tags.component.html',
    providers: [TagService],
})

export class AdminTagsComponent extends AdminBaseComponent {
    tags: TagType[] = [];
    selected: TagType[] = [];

    error: any;

    deletePromptHTML: string;
    consolidatePromptHTML: string;
    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false


    public theDeleteCallback: Function;
    public theConsolidateCallback: Function;

    constructor(private tagsService: TagService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesObservableService, titleService: Title, rightSidebarService: RightSidebarService, ) {
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
    }


    closeEditor() {
        this.showEditor = false;
        if (this.selected.length == 0 && this.tags.length > 0)
            this.selectSingleItem(this.tags[0]);
    }

    add() {
        this.selected = [];
        this.showEditor = true;

    }
    saveTag(event) {
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
                    this.tags[this.findTagIndex(event.item.uid)] = event.item;
                }
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
        this.deletePromptHTML = '';
        if (this.selected.length == 1) {
            this.deletePromptHTML = `Please confirm that you wish to delete the tag '${this.selected[0].Value}'(${this.selected[0].UseCount} assets tagged)`;
        }
        else {
            let tagList = '';
            this.selected.forEach(t => {
                tagList += `<tr><td>${t.Value}</td><td>${t.UseCount} assets tagged</td></tr>`;
            });
            this.deletePromptHTML = `Please confirm that you wish to delete following tags: <table>${tagList}</table>`;
        }
        this.showDelete = true;
    }

    openConsolidateModal() {
        this.showConsolidate = true;
    }

    consolidateTags(parentUid: string, childrenUids: string[]) {
        this.tagsService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                console.log(result);
                if (result.type != 'error') {
                    this.selected.forEach(t => {
                        this.tags.splice(this.findTagIndex(t.uid), 1);
                    })
                    this.selected = [];
                }
                this.showConsolidate = false;
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    tagStateChanged(state: boolean) {
        console.log(state);
    }


    findTagIndex(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }
};
