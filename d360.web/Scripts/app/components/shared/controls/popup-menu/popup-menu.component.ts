import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, OnInit, Input, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, AfterContentInit, OnDestroy, HostListener, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'ig-popup-menu',
    templateUrl: 'popup-menu.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./popup-menu.component.less']
})
export class PopupMenu implements AfterContentInit, OnDestroy {
    @Input() tabIndex: number = -1;
    @Input() items: PopupMenuItem[];

    private navigationArr: PopupMenuItem[] = [];
    private isVisible: boolean = false;

    @ViewChild('positionRef', { static: true }) positionEl: ElementRef;
    @ViewChild('element', { static: true }) popupEl: ElementRef;

    constructor(private cdRef: ChangeDetectorRef) {

    }

    ngAfterContentInit() {
        setTimeout(() => {
            document.body.append(this.popupEl.nativeElement);
            this.setElementPosition();
        });

        this.assignUniqueIDs(this.items, 1, null);
        this.navigationArr = this.items;
    }

    assignUniqueIDs(items: PopupMenuItem[], nextId: number, parent: PopupMenuItem) {
        items.forEach(i => {
            i.parent = parent;
            i.itemID = nextId;
            nextId += 1;
            if (i.items) {
                this.assignUniqueIDs(i.items, i.itemID + 10000, i);
            }
        })
    }

    updatePropToAll(items: PopupMenuItem[], prop: string, value: any) {
        items.forEach(x => {
            x[prop] = value;
            if (x.items) {
                this.updatePropToAll(x.items, prop, value);
            }
        });
    }

    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.setElementPosition();
    }
    @HostListener('document:mousewheel', ['$event'])
    onDocumentMousewheelEvent(event) {
        this.setElementPosition();
    }

    @HostListener('document:click', ['$event'])
    clickout(event) {
        if (!this.popupEl.nativeElement.contains(event.target)) {
            if (this.isVisible) {
                this.isVisible = false;
            }
        }
    }

    @HostListener('document:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (this.isVisible) {
            var activeItemIndex = this.navigationArr.indexOf(this.navigationArr.filter(x => x.isActive)[0]);
            var el = this.getActiveElement(this.items);
            if (el == undefined) {
                this.items[0].isActive = true;
            }
            if (event.keyCode === 39) {
                if (el && el.items) {
                    this.navigationArr = el.items;
                    this.moveThroughElements(-1, true, this.navigationArr);
                }
            }
            if (event.keyCode === 37) {
                if (el && el.parent) {
                    var items = el.parent.parent;
                    if (items) {
                        this.navigationArr = items.items;
                    }
                    else {
                        this.navigationArr = this.items;
                    }
                    this.moveThroughElements(this.navigationArr.indexOf(el.parent) - 1, true, this.navigationArr);
                }
            }

            if (event.keyCode === 40) {
                event.preventDefault();
                this.moveThroughElements(activeItemIndex, true, this.navigationArr);
            }
            if (event.keyCode === 38) {
                event.preventDefault();
                this.moveThroughElements(activeItemIndex, false, this.navigationArr);
            }
        }
    }

    private setElementPosition() {
        if (this.positionEl) {
            setTimeout(() => {
                var htmlEl = this.positionEl.nativeElement as HTMLElement;
                var box = htmlEl.getBoundingClientRect();

                var popup = this.popupEl.nativeElement as HTMLElement;
                popup.style.top = box.top + 'px';
                popup.style.left = box.left + 'px';
                this.cdRef.markForCheck();
            });
        }
    }

    private select(item: PopupMenuItem, $event: MouseEvent) {
        this.updatePropToAll(this.items, 'isSelected', false);
        item.isSelected = true;
        $event.stopPropagation();
    }

    private hover(item: PopupMenuItem) {
        if (!item.isSeparator) {
            this.setHoverStateToElement(item);
        }
    }

    private moveThroughElements(idx: number, forward: boolean, arr: PopupMenuItem[]): boolean {

        let nextIdx: number = 0;
        if (forward)
            nextIdx = idx + 1;
        else {
            nextIdx = idx - 1;
        }

        var el = arr[nextIdx];
        if (el && el.isSeparator != true) {
            this.setHoverStateToElement(el);
            return true;
        }
        if (nextIdx > -1 && nextIdx < arr.length) {
            return this.moveThroughElements(nextIdx, forward, arr);
        }
        return false;
    }

    private setHoverStateToElement(item: PopupMenuItem) {
        this.updatePropToAll(this.items, 'isActive', false);
        item.isActive = true;
        this.setHoverStateToParent(item);
    }

    ngOnDestroy() {
        this.popupEl.nativeElement.remove();
    }

    private hasIcons(items: PopupMenuItem[]) {
        return items.some(x => x.icon && x.icon != '');
    }

    private getItemClass(item: PopupMenuItem): string {
        let cs: string = '';
        if (item.isSeparator) return 'separator';
        else cs = 'menu-sub-item';

        if (item.isActive) {
            cs += ' active';
        }
        if (item.isSelected) {
            cs += ' selected';
        }
        if (item.disabled) {
            cs += ' disabled';
        }
        return cs;
    }

    private onFocus(item: PopupMenuItem) {
        item.isFocused = true;
    }
    private onFocusOut(item: PopupMenuItem) {
        item.isFocused = false;
    }

    public toggle() {
        setTimeout(() => {
            this.isVisible = !this.isVisible;
            this.cdRef.markForCheck();
        }, 10);

    }

    getFocusedElement(items: PopupMenuItem[]): PopupMenuItem {
        items.forEach(item => {
            if (item.isFocused == true) {
                return item;
            }
            else if (item.items) {
                return this.getFocusedElement(item.items);
            }
        })

        return null;
    }

    getActiveElement(items: PopupMenuItem[]): PopupMenuItem {
        var el: PopupMenuItem;
        items.forEach(item => {
            if (item.isActive == true) {
                el = item;
            }

            if (item.items) {
                if (this.getActiveElement(item.items)) {
                    el = this.getActiveElement(item.items);
                }
            }
        })

        return el;
    }

    private setHoverStateToParent(item: PopupMenuItem) {
        if (item.parent) {
            item.parent.isActive = true;
            this.setHoverStateToParent(item.parent);
        }
    }
}


@NgModule({
    imports: [
        CommonModule,
        TooltipModule
    ],
    declarations: [PopupMenu],
    exports: [PopupMenu]
})

export class PopupMenuModule { }


export class PopupMenuItem {
    label?: string;
    icon?: string;
    items?: PopupMenuItem[];
    disabled?: boolean = false;

    isSeparator?: boolean;
    isActive?: boolean = false;
    isSelected?: boolean = false;
    isFocused?: boolean = false;

    itemID: number;
    parent: PopupMenuItem;
}
