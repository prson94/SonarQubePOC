import { CommonModule, DatePipe } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TagService } from '../../../services/tag.service';
import { CoreModule } from '../core.module';


@Component({
    selector: 'd3s-tag-view',
    providers: [TagService],
    templateUrl: './d3s-tag-view.html',
})

export class TagView implements OnInit {
    @Input() data: any;

    private tags: any[];
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    constructor(private tagService: TagService) { }

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
    }

    getTagUrl(tag: any): string {
        return `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${tag.uid.toString().toLowerCase()}`;
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
            var parentWidth = this.container.nativeElement.closest('td').offsetWidth - 10;

            this.container.nativeElement.style.width = parentWidth + 'px';
            this.container.nativeElement.querySelectorAll('.tag-item-wrapper')
                .forEach((x) => {
                    if (x.offsetWidth > parentWidth) {
                        x.setAttribute('original-width', x.offsetWidth);
                        x.style.maxWidth = (parentWidth - 30) + 'px';
                        x.classList.add('too-long');
                        x.setAttribute('max-width', parentWidth - 30);
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
        this.tagService.getTagTooltip(tag.uid)
            .subscribe(t => {
                var x = t[0];
                var date = this.formatDate(x.CreatedOn);
                let template = `<span class="span-break">${x.Value}</span>
                                <span>Tag added by ${x.CreatedBy} on ${date}</span>`;
                el.querySelector('.tag-tooltip').innerHTML = template;

            });
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
}



@NgModule({
    declarations: [
        TagView,
    ],
    exports: [
        TagView,
    ]
    , imports: [
        CommonModule,
        CoreModule
    ]

})

export class TagViewModule { }