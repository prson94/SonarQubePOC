import { map } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { container } from '@angular/core/src/render3';


@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
})

export class TagView implements OnInit {
    @Input() data: string;

    private tags: any[];
    private isShowAll: boolean = false;
    @ViewChild("container") container: ElementRef;

    constructor() { }

    ngOnInit() {
        try {
            if (this.data)
                this.tags = JSON.parse(this.data);
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
            this.container.nativeElement.closest('td').classList.remove('no-text-overflow');
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
    ]

})

export class TagViewModule { }