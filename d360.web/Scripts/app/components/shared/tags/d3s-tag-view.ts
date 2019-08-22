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
import { CoreModule } from '../core.module';


@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
    providers: [TagService]
})

export class TagView extends AdminBaseComponent implements OnInit {
    public theDeleteCallback: Function;
    @Input() data: string;
    @Input() isEditable: boolean = false;
    private tags: any[];
    selected: TagType[] = [];
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    constructor(private tagsService: TagService, private messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSidebarService: RightSidebarService) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }

    ngOnInit() {
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
        this.openTagPage(event, `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${tag.uid.toString().toLowerCase()}`);
    }

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
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