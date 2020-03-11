
import { Component, ChangeDetectionStrategy, ElementRef, Input, OnChanges, OnInit, ContentChild, AfterViewChecked, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'd3s-simple-carousel',
    templateUrl: './simple-carousel.component.html'
})

export class SimpleCarouselComponent implements OnInit {
    @Input() data: any[] = [];

    private maxContentWidth: number = 1000;
    private leftOffset: number = 0;
    private showGoRight: boolean = false;

    private childWidth: number = 0;

    constructor(
        private elementRef: ElementRef,
        private cdr: ChangeDetectorRef
    ) {
    }

    ngOnInit() {

    }

    ngAfterViewChecked() {
        var content = this.elementRef.nativeElement as HTMLElement;
        var subItems = content.getElementsByClassName('carousel-content')[0];
        var numberOfItems = subItems.children.length;
        if (numberOfItems) {
            this.childWidth = subItems.children[0].clientWidth;
            this.maxContentWidth = this.childWidth * numberOfItems + 60;

            var lastChild = subItems.children[numberOfItems - 1];

            this.showGoRight = true;

            if (lastChild.getBoundingClientRect().left < content.getBoundingClientRect().right) {
                this.showGoRight = false;
            }

            this.cdr.detectChanges();
        }
    }

    moveRight() {
        this.leftOffset = this.leftOffset - this.childWidth;
    }

    moveLeft() {
        this.leftOffset = this.leftOffset + this.childWidth;
    }
};
