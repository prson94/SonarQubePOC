import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';


@Component({
    selector: 'd3s-header-shopping-cart',
    template:
        `
        <div class="header-button" routerLink="/cart" i18n-title title="Shopping cart">
            <i class="fa fa-shopping-cart"></i>
        </div>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderShoppingCartComponent implements OnInit {

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private breadcrumbService: HeaderBreadcrumbService,
        protected headerActionsService: HeaderActionsService,
        private ref: ChangeDetectorRef
    ) { }

    ngOnInit() {

    }
}

