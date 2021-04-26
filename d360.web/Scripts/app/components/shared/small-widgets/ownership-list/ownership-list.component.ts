import { NgModule, Component, Input, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";

class OwnershipResource {
    ResourceName: string;
    ResourceUid: string;
    ResponsibilityTypes: string;
    ResourceItemUrl: string;
}

@Component({
    selector: "d3s-ownership-list",
    template: `<span *ngIf="listLength===0">- - -</span>
                <ul *ngIf="listLength > 0" class="ownershiplist">
                    <li *ngFor="let owner of list; let i = index" [ngClass]="{'nobullet': listLength === 1, 'noshow': ((i >= moreLimit) && !showMore)}">
                        <span><a [href]="owner.ResourceItemUrl" [innerHtml]="owner.ResourceName"></a> ({{owner.ResponsibilityTypes}})</span>
                    </li>
                </ul>
                <a *ngIf="listLength > moreLimit" [innerHtml]="moreText()" (click)="toggleMore($event)"></a>`,
    styles: [`
        li.nobullet {
            list-style-type: none;
            margin-left: -10px;
        }
        li.noshow {
            display: none;
        }
        ul.ownershiplist {
            padding: 0;
            margin: 0;
            padding-left: 16px;
        }
        ul.ownershiplist li span {
            margin-left: -6px;
        }
    `],
})

export class OwnershipListComponent implements OnInit {
    @Input() list: OwnershipResource[];
    @Input() moreLimit: number = 3;

    listLength: number = 0;
    showMore: boolean = false;

    constructor() {
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
}

@NgModule({
    imports: [
        CommonModule,
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