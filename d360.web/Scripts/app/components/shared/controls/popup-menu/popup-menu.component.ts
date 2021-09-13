import { EventEmitter, Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, OnInit, Input, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, AfterContentInit, OnDestroy, HostListener, OnChanges, SimpleChanges, Output, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { KeyMapHelpers } from '../../../../static/keyboard-key-helper';
import { IgBadgeModule } from '../badge/badge.module';

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
    @Input() location: PopupMenuLocation;
    @Input() allowAllUnchecked: boolean = true;

    @Output() onSelect = new EventEmitter();

    navigationArr: PopupMenuItem[] = [];
    isVisible: boolean = false;
    currentLocation: PopupMenuLocation = PopupMenuLocation.BottomLeft;

    positionTop: number;
    positionLeft: number;
    pressedKeys: any = {};

    anchorElement: HTMLElement;
    @ViewChild('positionRef', { static: true }) positionEl: ElementRef;
    @ViewChild('element', { static: true }) popupEl: ElementRef;

    updatePositionInterval: any = null;
    openToLeftSide: boolean = false;
    openToBottomSide: boolean = true;

    toggleInProgress: boolean = false;
    typedCharacters: any[] = [];

    isMac: boolean = false;

    constructor(public cdRef: ChangeDetectorRef) {
        this.isMac = navigator.platform.indexOf('Mac') > -1;
    }
    reset() {
        this.navigationArr = this.items;
        this.updatePropToAll(this.items, 'hasHoverState', false);
        this.updatePropToAll(this.items, 'isSubMenuOpened', false);
        this.currentLocation = PopupMenuLocation.BottomLeft;
        this.pressedKeys = {};
    }

    clearSearch: any;
    search() {
        var searchString = this.typedCharacters.join('').toLowerCase();
        if (searchString) {
            var el = this.getLastHoveredElement(this.navigationArr);
            var currentIndex = this.navigationArr.indexOf(el);
            if (currentIndex == -1)
                currentIndex = 0;

            var foundIdx = -1;
            for (let idx = currentIndex; idx < this.navigationArr.length; idx++) {
                if (this.navigationArr[idx].title && this.navigationArr[idx].title.toLowerCase().indexOf(searchString) > -1 && this.navigationArr[idx].disabled != true) {
                    foundIdx = idx;
                }
            }
            if (foundIdx == -1) {
                for (let idx = 0; idx < this.navigationArr.indexOf(el); idx++) {
                    if (this.navigationArr[idx].title && this.navigationArr[idx].title.toLowerCase().indexOf(searchString) > -1 && this.navigationArr[idx].disabled != true) {
                        foundIdx = idx;
                    }
                }
            }

            if (foundIdx != -1) {
                var foundEL = this.navigationArr[foundIdx];
                this.setHoverStateToElement(foundEL);
            }
        }

        clearTimeout(this.clearSearch);
        this.clearSearch = setTimeout(() => {
            this.typedCharacters = [];
        }, 1000);
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

            //If there are keys, but Mac keys are not set, populate mac keys array and replace CTRL->Command key
            if ((i.keys && i.keys.length > 0) && (!i.keysMac || i.keysMac.length == 0)) {
                i.keysMac = [];
                i.keys.forEach(x => {
                    if (x === 17) {
                        i.keysMac.push(91);
                    } else {
                        i.keysMac.push(x);
                    }

                })
            }

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
            if (this.isVisible && !this.toggleInProgress) {
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
            let isNavigationKey: boolean = [39, 37, 40, 38].indexOf(event.keyCode) != -1;

            if (isNavigationKey) {
                let el: PopupMenuItem = null;
                if ([39, 37, 40, 38].indexOf(event.keyCode) != -1) {
                    var activeItemIndex = this.navigationArr.indexOf(this.navigationArr.filter(x => x.hasHoverState)[0]);
                    el = this.getLastHoveredElement(this.items);
                    if (el == undefined) {
                        this.items[0].hasHoverState = true;
                    }
                }
                else {
                    this.typedCharacters.push(event.key);
                    this.search();
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
            }
            //Escape
            if (event.keyCode === 27) {
                this.isVisible = false;
                this.reset();
            }

            //Space
            if (event.keyCode === 32) {
                event.preventDefault();
                var el = this.getLastHoveredElement(this.items);
                if (el) {
                    this.select(el, event);
                }
            }

            //Trigger shortcuts
            this.navigationArr.forEach(item => {
                if (item.keys && item.keys.length > 0) {
                    var doesMatch = true;
                    item.keys.forEach(key => {
                        if (!this.pressedKeys[key.toString()]) {
                            doesMatch = false;
                        }
                    })
                    if (doesMatch) {
                        this.select(item, { event: 'shortcut' });
                    }
                }

                if (item.keysMac && item.keysMac.length > 0) {
                    var doesMatch = true;
                    item.keysMac.forEach(key => {
                        if (!this.pressedKeys[key.toString()]) {
                            doesMatch = false;
                        }
                    })
                    if (doesMatch) {
                        this.select(item, { event: 'shortcut' });
                    }
                }
            })
        }
    }

    setElementPosition() {

        if (this.positionEl && this.isVisible) {

            var htmlEl = this.positionEl.nativeElement as HTMLElement;
            var menu = this.popupEl.nativeElement as HTMLElement;

            var box = htmlEl.getBoundingClientRect();
            var topPosition = box.top + window.scrollY - 12;
            var leftPosition = box.left;

            var isOverflowBottom = (window.innerHeight < (htmlEl.getBoundingClientRect().bottom + menu.offsetHeight));
            var isOverflowRight = ((htmlEl.getBoundingClientRect().left + menu.offsetWidth + 32) > window.innerWidth);

            this.openToBottomSide = false;
            if (isOverflowBottom && isOverflowRight) {
                this.currentLocation = PopupMenuLocation.TopRight;
            }
            else if (isOverflowBottom && !isOverflowRight) {
                this.currentLocation = PopupMenuLocation.TopLeft;
            }
            else if (isOverflowRight && !isOverflowBottom) {
                this.currentLocation = PopupMenuLocation.BottomRight;
                this.openToBottomSide = true;
            }
            else if (!isOverflowRight && !isOverflowBottom) {
                this.currentLocation = PopupMenuLocation.BottomLeft;
                if (this.location)
                    this.currentLocation = this.location;
                this.openToBottomSide = true;
            }


            if (this.anchorElement) {

                if (this.currentLocation == PopupMenuLocation.BottomLeft || this.currentLocation == PopupMenuLocation.BottomRight) {
                    topPosition = this.anchorElement.getBoundingClientRect().bottom + window.scrollY - 12;
                }
                else {
                    topPosition = this.anchorElement.getBoundingClientRect().top + window.scrollY + 12;
                }
            }

            if (this.currentLocation == PopupMenuLocation.TopLeft || this.currentLocation == PopupMenuLocation.TopRight) {
                topPosition = topPosition - menu.offsetHeight;
            }

            if (this.currentLocation == PopupMenuLocation.TopRight || this.currentLocation == PopupMenuLocation.BottomRight) {
                leftPosition = leftPosition - menu.offsetWidth;
                if (this.anchorElement) {
                    var ancBox = this.anchorElement.getBoundingClientRect();
                    leftPosition = ancBox.right - menu.offsetWidth;
                }
                this.openToLeftSide = true;
            }
            else {
                this.openToLeftSide = false;
            }

            topPosition = Math.floor(topPosition);
            leftPosition = Math.floor(leftPosition);

            var hasLocationChanged = Math.abs(topPosition - this.positionTop) > 3
                || Math.abs(leftPosition - this.positionLeft) > 3;
            var isUnset = !this.positionLeft && !this.positionTop;

            if (isUnset || hasLocationChanged) {
                setTimeout(() => {
                    this.positionTop = topPosition;
                    this.positionLeft = leftPosition;

                    this.cdRef.markForCheck();
                });
            }

        }
    }

    select(item: PopupMenuItem, $event) {
        if (item.disabled)
            return;

        if (item.callback) {
            item.callback();
        }

        if ($event.stopPropagation)
            $event.stopPropagation();

        if (item && item.items) {
            this.navigationArr = item.items;
            item.isSubMenuOpened = true;
            var firstActiveElement = this.navigationArr.filter(x => x.disabled != true)[0];
            this.moveThroughElements(this.navigationArr.indexOf(firstActiveElement) - 1, true, this.navigationArr);
            return;
        }

        if (!item.hasCheckbox) {
            this.onSelect.emit({ value: item.title, event: $event });
            this.toggle();
            this.reset();
        }
        else {
            if (this.allowAllUnchecked === false) {
                var checkBoxes = this.items.filter((x) => x.hasCheckbox === true && x.isChecked === true);
                if (checkBoxes.length === 1) {
                    //if only selected checkbox is current checkbox do not allow its unchecking
                    if (checkBoxes[0] === item) {
                        return;
                    }
                }
            }

            item.isChecked = !item.isChecked;
            this.onSelect.emit({ value: item.title, isChecked: item.isChecked, event: $event });
        }
    }

    mouseenter(item: PopupMenuItem) {
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

    moveThroughElements(idx: number, forward: boolean, arr: PopupMenuItem[]): boolean {

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

    setHoverStateToElement(item: PopupMenuItem) {
        this.updatePropToAll(this.items, 'hasHoverState', false);
        item.hasHoverState = true;
        this.setHoverStateToParent(item);
    }

    ngOnDestroy() {
        this.popupEl.nativeElement.remove();
        clearInterval(this.updatePositionInterval);
        this.updatePositionInterval = null;

    }

    hasIcons(items: PopupMenuItem[]) {
        return items.some(x => x.icon && x.icon != '');
    }
    hasCheckboxes(items: PopupMenuItem[]) {
        return items.some(x => x.hasCheckbox == true);
    }
    hasShortcuts(items: PopupMenuItem[]) {
        return items.some(x => x.keys && x.keys.length > 0);
    }

    getShortcutString(item: PopupMenuItem): string {
        if (item.keys) {
            var arr: string[] = [];
            if (this.isMac) {
                item.keysMac.forEach(k => {
                    arr.push(KeyMapHelpers.getCharForKeyCode(k, this.isMac));
                })
            }
            else {
                item.keys.forEach(k => {
                    arr.push(KeyMapHelpers.getCharForKeyCode(k, this.isMac));
                })
            }
            return this.isMac ? arr.join('') : arr.join('+');
        }
        return '';
    }

    getItemClass(item: PopupMenuItem): string {
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

    onFocus(item: PopupMenuItem) {
        item.isFocused = true;
    }
    onFocusOut(item: PopupMenuItem) {
        item.isFocused = false;
    }

    public toggle($event: MouseEvent = null) {
        if ($event && $event.srcElement) {
            this.anchorElement = ($event.srcElement as HTMLElement).closest('button');
        }
        this.toggleInProgress = true;
        setTimeout(() => {
            this.isVisible = !this.isVisible;
            this.toggleInProgress = false;
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

    setHoverStateToParent(item: PopupMenuItem) {
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
        FormsModule,
        IgBadgeModule
    ],
    declarations: [PopupMenu],
    exports: [PopupMenu]
})

export class PopupMenuModule { }

export enum PopupMenuLocation {
    TopLeft, TopRight, BottomLeft, BottomRight
}

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
    keysMac: number[] = [];
    badge: PopupMenuItemBadge;
    callback: Function;

    isSeparator?: boolean;
    isActive?: boolean = false;
    isFocused?: boolean = false;
    hasHoverState?: boolean = false;
    isSubMenuOpened?: boolean = false;


    itemID: number;
    parent: PopupMenuItem;

    constructor(data: Partial<PopupMenuItem>) {
        Object.assign(this, data);
    }
}

export class PopupMenuItemBadge {
    text: string = '';
    variant: string = 'default'
}
