import { NgModule, Component, Input, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DirectivesModule } from "../../../../directives/directives.module";
import { LinkClickInterceptor } from "../../../../services/href-click-service";
import { Router } from "@angular/router";

class OwnershipResource {
    ResourceName: string;
    ResourceUid: string;
    ResponsibilityTypes: string;
    ResourceItemUrl: string;
}

@Component({
    selector: "d3s-ownership-list",
    templateUrl: 'ownership-list.component.html',
    styles: [`
        ul.ownershiplist {
            padding: 0;
            margin: 0;
            padding-left: 16px;
        }
        ul.ownershiplist li {
            list-style-type: disc;
        }
        ul.ownershiplist li:first-child:nth-last-child(1) {
            list-style-type: none;
            margin-left: -12px;
        }
        ul.ownershiplist li.noshow {
            display: none;
        }
        .single-entry span {
            margin-left: -4px;
        }
    `],
})

export class OwnershipListComponent implements OnInit {
    @Input() list: OwnershipResource[];
    @Input() moreLimit: number = 3;
    @Input() interceptLinkClick: boolean = false;

    listLength: number = 0;
    showMore: boolean = false;

    constructor(
        private linkClickInterceptor: LinkClickInterceptor,
        private router: Router) {
    }

    ngOnInit() {
        this.listLength = this.list?.length ?? 0;
    }

    toggleMore(e) {
        e.preventDefault();
        e.stopPropagation();
        this.showMore = !this.showMore;
        return false;
    }

    moreText() {
        if (this.showMore) {
            return "Show less";
        } else {
            return `Show ${this.listLength - this.moreLimit} more...`;
        }
    }

    formatResponsibilityTypes(types: string) {
        if (types.length > 0) {
            return `(${types})`;
        }
        return types;
    }

    onClick($event, data) {
        if (this.interceptLinkClick) {
            this.linkClickInterceptor.sendEvent($event, data, data.ResourceItemUrl)
            return;
        }
        this.router.navigateByUrl(data.ResourceItemUrl);
        if ($event) {
            $event.preventDefault();
            $event.stopPropagation();
        }
    }
}

@NgModule({
    imports: [
        CommonModule,
        DirectivesModule
    ],
    declarations: [
        OwnershipListComponent
    ],
    exports: [
        OwnershipListComponent
    ],
    providers: [
    ]
})
export class OwnershipListModule { }