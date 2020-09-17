import { EventEmitter, Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, OnInit, Input, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, AfterContentInit, OnDestroy, HostListener, OnChanges, SimpleChanges, Output, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { KeyMapHelpers } from '../../../../static/keyboard-key-helper';

@Component({
    selector: 'ig-popup-menu',
    templateUrl: 'popup-menu.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./popup-menu.component.less']
})
export class PopupMenu implements AfterContentInit, OnDestroy, DoCheck {
    @Input() tabIndex: number = -1;
    @Input() items: PopupMenuItem[];

    @Output() onSelect = new EventEmitter();

    private navigationArr: PopupMenuItem[] = [];
    private isVisible: boolean = false;

    private positionTop: number;
    private positionLeft: number;
    private pressedKeys: any = {};

    @ViewChild('positionRef', { static: true }) positionEl: ElementRef;
    @ViewChild('element', { static: true }) popupEl: ElementRef;

    private updatePositionInterval: any = null;

    constructor(private cdRef: ChangeDetectorRef) {

    }
    private reset() {
        this.navigationArr = this.items;
        this.updatePropToAll(this.items, 'hasHoverState', false);
        this.updatePropToAll(this.items, 'isSubMenuOpened', false);
        this.pressedKeys = {};
    }

    ngDoCheck() {
        if (this.isVisible && this.updatePositionInterval == null) {
            this.updatePositionInterval = setInterval(() => this.setElementPosition(), 10);
        }
        else if (!this.isVisible && this.updatePositionInterval != null) {
            clearInterval(this.updatePositionInterval);
            this.updatePositionInterval = null;
        }
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
            if (i.isLabel == true) {
                i.disabled = true;
            }
            if (i.hasCheckbox) {
                i.items = null;
                i.icon = null;
            }
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
                this.reset();
            }
        }
    }

    @HostListener('document:keyup', ['$event'])
    handleKeyUp(event: KeyboardEvent) {
        if (this.isVisible) {
            delete this.pressedKeys[event.key];
        }
    }

    @HostListener('document:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (this.isVisible) {
            this.pressedKeys[event.keyCode] = true;

            let el: PopupMenuItem = null;
            if ([39, 37, 40, 38].indexOf(event.keyCode) != -1) {
                var activeItemIndex = this.navigationArr.indexOf(this.navigationArr.filter(x => x.hasHoverState)[0]);
                el = this.getLastHoveredElement(this.items);
                if (el == undefined) {
                    this.items[0].hasHoverState = true;
                }
            }

            //Arrow right
            if (event.keyCode === 39) {
                if (el && el.items) {
                    this.navigationArr = el.items;
                    el.isSubMenuOpened = true;
                    var firstActiveElement = this.navigationArr.filter(x => x.disabled != true)[0];
                    this.moveThroughElements(this.navigationArr.indexOf(firstActiveElement) - 1, true, this.navigationArr);
                }
            }

            //Arrow left
            if (event.keyCode === 37) {
                if (el && el.parent) {
                    var items = el.parent.parent;
                    el.parent.isSubMenuOpened = false;
                    if (items) {
                        this.navigationArr = items.items;
                    }
                    else {
                        this.navigationArr = this.items;
                    }
                    this.moveThroughElements(this.navigationArr.indexOf(el.parent) - 1, true, this.navigationArr);
                }
            }

            //Arrow down
            if (event.keyCode === 40) {
                event.preventDefault();
                this.moveThroughElements(activeItemIndex, true, this.navigationArr);
            }

            //Arrow up
            if (event.keyCode === 38) {
                event.preventDefault();
                this.moveThroughElements(activeItemIndex, false, this.navigationArr);
            }

            //Escape
            if (event.keyCode === 27) {
                this.isVisible = false;
                this.reset();
            }
            if (event.keyCode === 32) {
                event.preventDefault();
                el = this.getLastHoveredElement(this.items);
                if (el) {
                    this.select(el, event);
                }
            }

            console.log(this.pressedKeys);
        }
    }

    private setElementPosition() {
        if (this.positionEl) {
            var htmlEl = this.positionEl.nativeElement as HTMLElement;
            var box = htmlEl.getBoundingClientRect();
            var topPosition = box.top + window.scrollX - 12;
            if (topPosition != this.positionTop || box.left != this.positionLeft) {
                setTimeout(() => {
                    this.positionTop = topPosition;
                    this.positionLeft = box.left;
                    this.cdRef.markForCheck();
                });
            }

        }
    }

    private select(item: PopupMenuItem, $event) {
        $event.stopPropagation();
        if (!item.hasCheckbox) {
            this.onSelect.emit({ value: item.title, event: $event });
            this.toggle();
            this.reset();
        }
        else {
            item.isChecked = !item.isChecked;
            this.onSelect.emit({ value: item.title, isChecked: item.isChecked, event: $event });
        }
    }

    private hover(item: PopupMenuItem) {
        if (!item.isSeparator) {
            if (item.parent == null) {
                this.updatePropToAll(this.items, 'isSubMenuOpened', false);
            }
            else {
                item.parent.items.forEach(x => x.isSubMenuOpened = false);
            }

            item.isSubMenuOpened = true;
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
            if (el.disabled == true) {
                this.moveThroughElements(nextIdx, forward, arr);
            } else {
                this.setHoverStateToElement(el);
                return true;
            }
        }
        if (nextIdx > -1 && nextIdx < arr.length) {
            return this.moveThroughElements(nextIdx, forward, arr);
        }
        return false;
    }

    private setHoverStateToElement(item: PopupMenuItem) {
        this.updatePropToAll(this.items, 'hasHoverState', false);
        item.hasHoverState = true;
        this.setHoverStateToParent(item);
    }

    ngOnDestroy() {
        this.popupEl.nativeElement.remove();
        clearInterval(this.updatePositionInterval);
        this.updatePositionInterval = null;

    }

    private hasIcons(items: PopupMenuItem[]) {
        return items.some(x => x.icon && x.icon != '');
    }
    private hasCheckboxes(items: PopupMenuItem[]) {
        return items.some(x => x.hasCheckbox == true);
    }
    private hasShortcuts(items: PopupMenuItem[]) {
        return items.some(x => x.keys && x.keys.length > 0);
    }

    private getShortcutString(item: PopupMenuItem): string {
        if (item.keys) {
            var arr: string[] = [];
            item.keys.forEach(k => {
                arr.push(KeyMapHelpers.getCharForKeyCode(k));
            })
            return arr.join('+');
        }
        return '';
    }

    private getItemClass(item: PopupMenuItem): string {
        let cs: string = '';
        if (item.isSeparator) return 'separator';
        else cs = 'menu-sub-item';

        if (item.isActive) {
            cs += ' active';
        }
        if (item.hasHoverState) {
            cs += ' hover';
        }
        if (item.disabled) {
            cs += ' disabled';
        }
        if (item.isLabel) {
            cs += ' label';
        }
        if (item.default) {
            cs += ' default';
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

    getLastHoveredElement(items: PopupMenuItem[]): PopupMenuItem {
        var el: PopupMenuItem;
        items.forEach(item => {
            if (item.hasHoverState == true) {
                el = item;
            }

            if (item.items) {
                if (this.getLastHoveredElement(item.items)) {
                    el = this.getLastHoveredElement(item.items);
                }
            }
        })

        return el;
    }

    private setHoverStateToParent(item: PopupMenuItem) {
        if (item.parent) {
            item.parent.hasHoverState = true;
            this.setHoverStateToParent(item.parent);
        }
    }


}


@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule
    ],
    declarations: [PopupMenu],
    exports: [PopupMenu]
})

export class PopupMenuModule { }


export class PopupMenuItem {
    title?: string;
    icon?: string;
    items?: PopupMenuItem[];
    disabled?: boolean = false;
    tooltip?: string = '';
    default: boolean = false;
    isLabel: boolean = false;
    hasCheckbox: boolean = false;
    isChecked: boolean = null;
    keys: number[] = [];

    isSeparator?: boolean;
    isActive?: boolean = false;
    isFocused?: boolean = false;
    hasHoverState?: boolean = false;
    isSubMenuOpened?: boolean = false;


    itemID: number;
    parent: PopupMenuItem;
}
