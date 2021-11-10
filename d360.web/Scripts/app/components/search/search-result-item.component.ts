import { Component, Input, Output, ChangeDetectionStrategy, OnInit, ChangeDetectorRef, ViewChild, ElementRef, EventEmitter, Inject, forwardRef } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult, SearchResultFieldDisplay, SearchSelecton } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { DatePipe } from '@angular/common';
import { PopupMenu } from '../shared/controls/popup-menu/popup-menu.component';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

@Component({
    selector: 'd3s-search-result-item',
    templateUrl: './search-result-item.component.html',
    styleUrls: ["search-result-item.component.less"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService, DatePipe],
    host: { '(window:resize)': 'checkSize()' }
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;
    @Input() selection: SearchSelecton;

    @Output() onSelect = new EventEmitter();

    showStatus: boolean = false;
    showPath: boolean = false;

    menuitems: any[] = [{ title: "Open" }, { title: "Open in New Tab" }];

    showScrollButtons: boolean = false;
    disableScrollLeft: boolean = false;
    disableScrollRight: boolean = false;

    @ViewChild('cardmenu', { static: false }) cardmenu: PopupMenu;
    @ViewChild('fieldScroller', { static: false }) fieldScroller: ElementRef;
    
    constructor(private router: Router,
        private shoppingCartService: ShoppingCartService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef,
        private elementRef: ElementRef,
        private datePipe: DatePipe) {
        super(settingsService);
    }

    ngOnInit() {
        let showCart = this.settingsService.getSettingById(CompanySettingEnum.EnableShoppingCart).BooleanSetting.Value;
        if (showCart) {
            this.menuitems.push({ title: "Add to Cart" });
        }

        this.loadDetails();
        //Need to wait for ViewChildren, but can't use AfterOnInit
        setTimeout(() => {
            this.checkSize();
        });
    }

    private loadDetails() {
        if (this.result.Status) {
            this.showStatus = true;
        }
    }

    parseTagResult(tags: any[]) {
        return tags.map((tag) => { return { uid: tag.Uid, Value: tag.Value }; });
    }

    get type() {
        if (this.result) {
            switch (this.result.Group) {
                case 'Reference':
                    return 'ReferenceItemType';
                default:
                    return this.result.Group;
            }
        }
    }

    showBadges(): boolean {
        return this.showStatus || this.result.Scores.length > 0;
    }

    clickMenuItem(event: any) {
        const key = event.value.toLowerCase();

        if (key === "open") {
            this.navigateLink();
        } else if (key === "open in new tab") {
            this.navigateLink(true);
        } else if (key === "add to cart") {
            this.add();
        }
    }

    navigateLink(newTab: boolean = false) {
        const url = SiteUrlHelpers.convertClassicUrl(this.result.Url);
        if (newTab) {
            // eslint-disable-next-line
            window.open(url, "_blank");
        } else {
            this.router.navigateByUrl(url);
        }
    }

    private navigateVisualization() {
        let url = (this.result.Group == 'Diagram Asset') ? this.result.Url : '/sidebar/visualization/browser/' + this.result.Uid;
        this.router.navigateByUrl(url);
    }

    private add() {
        var type = this.result.ID.toString().split('|')[0];
        var id = this.result.ID.toString().split('|')[1];
        this.shoppingCartService.addShoppingCartItem(this.type, +id, 1)
            .subscribe(r => this.showMessageForResult(this.messagesService, r));
    }

    formatPathAsString(): string {
        if (this.result.Group && this.result.AssetPath) {
            return this.result.Group +' > ' + this.result.AssetPath.map(p => p.Key.join(' / ') + ' (' + p.AssetType + ')').join(' > ');
        }
        return '';
    }

    get isSelected(): boolean {
        return this.selection?.ID === this.result.ID;
    }

    /**
     * Formats display of field value.
     * Links are returned from API in format <url>|<displayvalue>, Booleans are displayed as an icon etc.
     * If Prefix/Suffic is set, they are added to the display value
     * @param field
     * @param forTitle Return is used in title, so booleans are shown as value and links shown as displayvalue
     */
    getFieldDisplayValue(field: SearchResultFieldDisplay, forTitle: boolean = false):string {
        let val: string = (field.Empty) ? '---' : field.Value;
        if (val === null || val === undefined)
            return '';

        if (!field.Empty) {
            switch (field.Type.toLowerCase()) {
                case 'link':
                    if (field.Value.length > 2 && field.Value.indexOf('|') > 0) {
                        let link: string[] = field.Value.split('|', 2);
                        val = forTitle ? link[1] : '<a href="' + link[0] + '" target="_blank">' + link[1] + '</a>';
                    }
                    break;
                case 'boolean':
                    if (!forTitle) {
                        if (field.Value == 'True')
                            val = '<i class="fa fa-check enabled"></i>';
                        else
                            val = '<i class="fa fa-times disabled"></i>';
                    }
                    break;
                case 'decimal':
                case 'number':
                    val = Number(val).toLocaleString();
                    break;
                case 'date':
                    val = val.substr(0, val.indexOf(' '));
                    break;
                case 'datetime':
                    //Date is UTC
                    let utc = Date.parse(val + ' UTC');
                    val = this.datePipe.transform(utc, 'medium');
                    break;
            }
        }
        if (field.Suffix)
            val += ' ' + field.Suffix;
        if (field.Prefix)
            val = field.Prefix + ' ' + val;
        return val;
    }

    /* Field scroller section */

    checkSize() {
        if (this.fieldScroller) {
            let maxWidth = this.getElementRightPosition(this.fieldScroller.nativeElement.parentElement);
            let lastTab = this.getElementRightPosition(this.fieldScroller.nativeElement.lastChild);
            this.showScrollButtons = lastTab > maxWidth;
        }
        this.checkScrollPos();
    }

    checkScrollPos() {
        if (this.fieldScroller) {
                let currentPosition = this.fieldScroller.nativeElement.scrollLeft;
                this.disableScrollLeft = currentPosition == 0;
    
                let maxWidth = this.getElementRightPosition(this.fieldScroller.nativeElement.parentElement);
                let lastTab = this.getElementRightPosition(this.fieldScroller.nativeElement.lastChild);
                this.disableScrollRight = lastTab <= maxWidth;
    
                this.ref.markForCheck();
        }
    }

    private getElementRightPosition(element) {
        if (element && element.getBoundingClientRect) {
            return element.getBoundingClientRect().right;
        }
        return NaN;
    }

    private getElementWidth(element) {
        if (element && element.getBoundingClientRect) {
            return element.getBoundingClientRect().right - element.getBoundingClientRect().left;
        }
        return NaN;
    }

    scroll(direction: string) {
        let el = this.fieldScroller.nativeElement;
        let scrollAmount = 0;
        let scrollDistance = Math.floor(this.getElementWidth(el)*0.95);
        let move = () => {
            if (direction == 'L') {
                el.scrollLeft -= 10;
            } else {
                el.scrollLeft += 10;
            }
            scrollAmount += 10;
            if (scrollAmount >= scrollDistance) {
                this.checkScrollPos();
                window.clearInterval(id);
            }
            this.checkScrollPos();
        };

        let id = window.setInterval(move, 5);
    }

    /* events */
    onClick() {
        this.elementRef.nativeElement.children[0].focus();
    }

    onTouchEnd() {
        this.elementRef.nativeElement.children[0].focus();
    }

    onFocus() {
        if (!this.isSelected) {
            this.onSelect.emit({
                ID: this.result.ID,
                AssetUid: this.result.Uid,
                ObjectType: this.result.Object,
                HasProfiling: this.result.HasProfiling
            });
        }
    }

    onKeyDown(event: KeyboardEvent) {
        if (!this.cardmenu.isVisible && ["ArrowDown", "ArrowUp"].indexOf(event.key) !== -1) {
            event.preventDefault();
            const resultElement = this.elementRef.nativeElement.parentElement;
            const neighbor: HTMLDivElement = (event.key === "ArrowDown") ? resultElement.nextElementSibling : resultElement.previousElementSibling;
            neighbor?.querySelector<HTMLDivElement>(".card-res")?.focus();
        }
    }
};