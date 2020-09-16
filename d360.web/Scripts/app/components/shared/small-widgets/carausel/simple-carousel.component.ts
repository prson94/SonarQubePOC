
import { Component, ElementRef, Input, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'd3s-simple-carousel',
    templateUrl: './simple-carousel.component.html'
})

export class SimpleCarouselComponent {
    @Input() data: any[] = [];

    maxContentWidth: number = 1000;
    leftOffset: number = 0;
    showGoRight: boolean = false;

    private childWidth: number = 0;
    private currentylSelectedChildIDX: number = 0;
    constructor(
        private elementRef: ElementRef,
        private cdr: ChangeDetectorRef
    ) {
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

            for (var i = 0; i < subItems.children.length; i++) {
                var child = subItems.children[i].querySelector('.value');

                if (child.className.toLowerCase().indexOf('selected') > -1 && this.currentylSelectedChildIDX != i && !this.isChildVisible(child, content)) {
                    this.currentylSelectedChildIDX = i;

                    var selectedLocation = (child.getBoundingClientRect().right + child.getBoundingClientRect().left) / 2;;
                    var wrapperLocation = (content.getBoundingClientRect().right + content.getBoundingClientRect().left) / 2;

                    var moveFor = 0;
                    if (selectedLocation > wrapperLocation) {
                        moveFor = Math.round((content.getBoundingClientRect().right - child.getBoundingClientRect().right) / this.childWidth) - 1;
                        this.leftOffset = this.leftOffset + (moveFor * this.childWidth);
                    }
                    else {
                        moveFor = Math.round((content.getBoundingClientRect().left - child.getBoundingClientRect().left) / (this.childWidth)) + 1;
                        this.leftOffset = this.leftOffset + (moveFor * this.childWidth);
                    }

                }
            }

            if (this.leftOffset > 0) {
                this.leftOffset = 0;
            }
            

            this.cdr.detectChanges();
        }
    }

    isChildVisible(child: Element, parent: Element) {
        var c = child.getBoundingClientRect();
        var p = parent.getBoundingClientRect();

        return p.left < c.left && p.right > c.right;
    }


    moveRight() {
        this.leftOffset = this.leftOffset - this.childWidth;
    }

    moveLeft() {
        this.leftOffset = this.leftOffset + this.childWidth;
    }
};
