import { CommonModule, DatePipe } from '@angular/common';
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
    @Input() data: any;
    @Input() isEditable: boolean = false;
    private tags: any[];
    selected: TagType[] = [];
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    constructor(private tagService: TagService, private messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSidebarService: RightSidebarService) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }

    ngOnInit() {
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

    enter(tag: any, el: HTMLElement) {
        if (!this.isEditable) {
            this.tagService.getTagTooltip(tag.uid)
                .subscribe(t => {
                    var x = t[0];
                    var date = this.formatDate(x.CreatedOn);
                    let template = `<span class="span-break">${x.Value}</span>
                                <span>Tag added by ${x.CreatedBy} on ${date}</span>`;
                    el.querySelector('.tag-tooltip').innerHTML = template;

                });
        }
    }

    formatDate(str: string) {
        var date = new Date(str);

        var monthNames = [
            'January', 'February', 'March',
            'April', 'May', 'June', 'July',
            'August', 'September', 'October',
            'November', 'December'
        ];

        var partOfDay = "am";
        var day = date.getDate();
        var monthIndex = date.getMonth();
        var year = date.getFullYear();

        var hour = date.getHours();
        var minutes = date.getMinutes()

        let shour: string;
        let smin: string;

        if (hour > 11) {
            partOfDay = 'pm';
            hour -= 12;
        }
        if (hour == 0) hour = 12;

        shour = hour.toString();
        smin = minutes.toString();

        if (hour < 10) {
            shour = '0' + hour;
        }

        if (minutes < 10) {
            smin = '0' + smin;
        }

        return `${day} ${monthNames[monthIndex]} ${year} at ${shour}:${smin}${partOfDay}`;

    }

    findTagIndex(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
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