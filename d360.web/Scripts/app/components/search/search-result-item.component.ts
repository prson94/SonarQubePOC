import { Component, Input, ChangeDetectionStrategy, OnInit, ChangeDetectorRef, HostListener, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult, SearchDetail, SearchResultFieldDisplay, SearchPathComponent } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { TagService } from '../../services/tag.service';
import { Tag, TagItem } from '../../models/tag.model';
import { ObjectStatisticsService } from '../../services/object-statistics.service';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { DatePipe } from '@angular/common';

declare var CompanySettings;

@Component({
    selector: 'd3s-search-result-item',
    templateUrl: './search-result-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService, ObjectStatisticsService, DatePipe],
    host: { '(window:resize)': 'checkSize()' }
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;
    private lastCalculatedDate: number;
    showStatus: boolean = false;
    showPath: boolean = false;
    private status: string;
    private path: string;
    showShoppingCart = false;
    private obj: string;
    private objID: number;
    searchDetails: SearchDetail;
    private formattedPath: string;
    private displayInfopopup: boolean = false;

    showScrollButtons: boolean = false;
    disableScrollLeft: boolean = false;
    disableScrollRight: boolean = false;

    @ViewChild('cardmenu', { static: false }) cardmenuRef: Menu;
    @ViewChild('cardmenubutton', { static: false }) cardmenubuttonRef: ElementRef;
    @ViewChild('fieldScroller', { static: false }) fieldScroller: ElementRef;
    
    @HostListener('document:click', ['$event.target'])
    public hostclick(targetElement) {
        if (this.cardmenuRef != undefined && this.cardmenuRef.visible == true && targetElement.closest('.kebabmenu') == null) {
            if (!this.cardmenubuttonRef.nativeElement.contains(targetElement)) {
                this.cardmenuRef.hide();
            }
        }
    }

    parseTagResult(tags: any[]) {
        return tags.map((tag) => { return { uid: tag.Uid, Value: tag.Value }; });
    }
    get type() {
        if (this.result) {
            switch (this.result.Group) {
                case 'FusionAttributes':
                    return 'FusionAttribute';
                case 'Reference':
                    return 'ReferenceItemType';
                default:
                    return this.result.Group;
            }
        }
    }

    constructor(private router: Router,
        private shoppingCartService: ShoppingCartService,
        private messagesService: MessagesObservableService,
        private objectStatisticsService: ObjectStatisticsService,
        private ref: ChangeDetectorRef,
        private datePipe: DatePipe) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.EnableShoppingCart != null && CompanySettings.EnableShoppingCart.toString() == 'true')
            this.showShoppingCart = true;

        this.loadDetails();
        //Need to wait for ViewChildren, but can't use AfterOnInit
        setTimeout(() => {
            this.checkSize();
        });
    }

    private loadDetails() {
        if (this.result.Uid) {
            this.objectStatisticsService.getSearchDetails(this.result.Uid).subscribe(
                (result) => {
                    this.searchDetails = result;
                    if (this.searchDetails && this.searchDetails?.AssetDetail.Status) {
                        this.status = this.searchDetails.AssetDetail.Status;
                        this.showStatus = true;
                    } else {
                        this.showStatus = false;
                    }
                    this.ref.markForCheck();
                }
            );  
        }
    }

    showBadges(): boolean {
        return this.showStatus || (this.searchDetails && this.searchDetails.Scores.length > 0)
    }

    showInfo() {
        this.displayInfopopup = true;
    }

    getCardMenuItems(): MenuItem[] {
        var menu: MenuItem[] = [
            { label: 'More Information', command: (event) => { this.showInfo() } },
        ];
        if (this.result.Uid && CompanySettings.LineageVersion == 3 && ['Reference', 'Resource', 'Group', 'Grammatic type', 'Fusion'].indexOf(this.result.Group) == -1) {
            menu.push({
                label: 'View Diagram',
                command: (event) => { this.navigateVisualization(); }
            });
        }
        if (this.showShoppingCart && ['Synonym', 'Grammatic type'].indexOf(this.result.Group) == -1) {
            menu.push({
                label: '',
                icon: 'fa fa-cart-plus',
                command: (event) => { this.add(); }
            });
        }
        return menu;
    }

    navigateLink() {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
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

    getDataForPreview() {
        return {
            DisplayName: this.result.DisplayName,
            TypeName: this.result.Type,
            Description: this.result.Description,
            AssetID: this.result.ID,
            UID: this.result.Uid
        }
    }

    formatPathAsString(): string {
        if (this.result.Group && this.result.AssetPath) {
            return this.result.Group +' > ' + this.result.AssetPath.map(p => p.Key.join(' / ') + ' (' + p.AssetType + ')').join(' > ');
        }
        return '';
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
};