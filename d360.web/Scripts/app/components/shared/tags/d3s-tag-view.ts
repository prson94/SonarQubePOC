import { CommonModule } from '@angular/common';
import { NgModule, Input, Component, OnInit, ViewChild, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TagType } from '../../../models/tag.model';
import { TagService } from '../../../services/tag.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CoreModule } from '../core.module';
import { Router } from '@angular/router';


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
    private tags: any[];
    selected: TagType[] = [];
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    private tagTooltip: TagType;
    private isTooltipLoaded: boolean = false;

    constructor(private router: Router, private tagService: TagService, private messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSidebarService: RightSidebarService, private ref: ChangeDetectorRef) {
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

        if (this.container) {
            let parent = this.getParentForResizing(this.container.nativeElement, 'TD,DIV');
            if (!parent) {
                console.warn("No suitable parent fount for tag resizing!");
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

    openTagPage(event: MouseEvent, item: any) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid}`]);
        event.stopPropagation();
    }

    enter(tag: any, el: HTMLElement) {
        this.isTooltipLoaded = false;
        if (!this.isEditable) {
            this.tagService.getTagTooltip(tag.uid)
                .subscribe(t => {
                    this.tagTooltip = t[0];
                    this.isTooltipLoaded = true;
                    this.ref.markForCheck();
                });
        }
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