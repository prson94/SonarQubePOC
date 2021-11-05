import { Input, Component, ChangeDetectionStrategy, Output, EventEmitter, ViewChild, HostListener, ElementRef } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteMenu, SiteNav } from '../../../models/site-menu.model';
import * as _ from 'lodash';
import { CompanySettingsService } from '../../../services/settings.service';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-site-menu-category',
    templateUrl: 'site-menu-category.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuCategoryComponent extends BaseComponent {
    @Input() url: string;
    @Input() title: string;
    @Input() rootIconName: string;
    @Input() public menu: SiteMenu;
    @Input() showClearButton: boolean = false;
    @Input() expanded: boolean;
    @Input() imageUrl: string;
    @Input() countData: any[];
    @Input() isActive: boolean = false;

    @Output() clearClick = new EventEmitter();
    @Output() activeItemChanged = new EventEmitter();

    @HostListener('document:click', ['$event'])
    documentClick(event: MouseEvent) {
        if (this.menu && this.menu.isActiveItem) {
            this.activeItemChanged.emit(undefined);
        }    
    }

    constructor(
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    @ViewChild('item', { static: false }) item: ElementRef;
    
    navigateToUrl(url) {        
        if (url) {
            this.router.navigateByUrl(url);
        }
    }
    
    show(item) {
        this.activeItemChanged.emit({ item: this });        
        this.positionMenu(null);
    }

    private positionMenu(event: any) {
        if (event != null && (event.keyCode == 40 || event.keyCode == 13 || event.keyCode == 38)) {
            return;
        }
        if (this.menu && this.menu.NavigationItems) {
            let submenu = this.item.nativeElement.children[0].nextElementSibling;
            if (submenu) {
                var dims = this.item.nativeElement.getBoundingClientRect();
                this.menu.isActiveItem = true;                
                submenu.style.zIndex = ++SiteNav.zindex;
                submenu.style.top = dims.top + 'px';
                submenu.style.left = this.item.nativeElement.offsetWidth + 'px';

                window.setTimeout(() => {
                    this.repositionMenuToFit(submenu);
                }, 150);
            }
        }
    }

    stopNavigation(event) {
        event.stopPropagation();
    }

    repositionMenuToFit(element) {
        var dims = element.getBoundingClientRect();
        let windowHeight = window.innerHeight;
        if (dims) {
            var maxHeight = dims.top + dims.height;

            //case where menu is bigger than height of page
            if (dims.height > windowHeight) {
                dims = element.getBoundingClientRect();
                element.style.top = 40 + 'px';
                maxHeight = dims.top + dims.height;
                if (maxHeight > windowHeight) { //case where bottom is below page after resizing
                    var topOffset = dims.top + (windowHeight - maxHeight);
                    element.style.top = topOffset + 'px';
                }
            }
            else if (maxHeight > windowHeight) { //case where bottom is below page
                var topOffset = dims.top + (windowHeight - maxHeight);

                element.style.top = topOffset + 'px';
            }
        }
    }
}