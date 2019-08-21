import { map } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { container } from '@angular/core/src/render3';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { DynamicEditorComponent } from '../dynamicgrideditor/dynamic-editor.component';
import { Tag, TagType } from '../../../models/tag.model';
import { TagService } from '../../../services/tag.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router } from '@angular/router';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';


@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
    providers: [TagService]
})

export class TagView extends AdminBaseComponent implements OnInit {
    public theDeleteCallback: Function;
    @Input() data: string;
    @Input() isEditable: boolean = false;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private tags: any[];
    selected: TagType[] = [];
    private editPopupTitle: string = 'Edit Tag';
    private deletePopupTitle: string = 'Delete Tag';
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    constructor(private tagsService: TagService, private messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSidebarService: RightSidebarService) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteTags.bind(this);
        try {
            if (this.data)
                this.tags = JSON.parse(this.data);
        }
        catch
        {
            console.warn("d3s-tag-view::Error while parsing tags!");
        }
        this.selected = this.tags;
    }

    getTagUrl(tag: any, event: MouseEvent) {
        if (this.isEditable != true && this.showDelete == false)
            this.openTagPage(event, `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${tag.uid.toString().toLowerCase()}`);
        else if (this.showDelete !=true) {
            this.showEditor = true;
        }
    }

    openDeleteModal(tag: any) {
        if (this.isEditable == true) {
            this.showDelete = true;
            this.selected.push(tag);
            this.deletePopupTitle = this.selected.length == 1 ? 'Delete Tag' : 'Delete Tags';
        }
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

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
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

    setVisibility() {
        this.container.nativeElement.querySelectorAll('.tag-item-wrapper')
            .forEach((x, index) => {
                if (!this.isShowAll && index > 9) {
                    x.closest('a').classList.add('hide');
                }
                else {
                    x.closest('a').classList.remove('hide');
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

    //Transition speed is set in .less
    enter(el: HTMLElement) {
        el.classList.remove('too-long');
        var setTo = el.getAttribute('original-width');
        el.style.maxWidth = setTo + 'px';
    }

    leave(el: HTMLElement) {
        var setTo = el.getAttribute('max-width');
        el.style.maxWidth = setTo + 'px';
        el.classList.add('too-long');

    }

    findTagIndex(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }
}
