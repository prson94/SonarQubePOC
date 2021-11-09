import {Component, Input, OnInit, OnChanges, SimpleChange} from '@angular/core';
import {GroupService} from '../../services/group.service';
import {ResourcesService} from '../../services/resources.service';
import {CountObject} from '../../models/resource.model';
import {Group} from '../../models/group.model';
import {BaseComponent} from '../shared/base.component';
import { CompanySettingsService } from '../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-group-responsibility',
    template: `
        <header>
            Items {{group?.Name}} Owns
            <d3s-tile-actions [hasExport]="false" [hasFilterMode]="true" [filterMode]="showFilter"
                              (filterModeChange)="showFilter = !showFilter"></d3s-tile-actions>
        </header>
        <div *ngIf="!isLoading" class="row">
            <div class="col l3 s12 relationship-container"><!--left nav-->
                <div class="row relationship" *ngFor="let r of items; let i = index"
                     [ngClass]="{'active' : isSelected(r)}" (click)="select(r)">
                    <div class="col s10 name" [title]="r.Type | technicalNameToDisplayValue">{{r.TypeName}}</div>
                    <div class="col s2 count center"
                         [ngClass]="{'empty-count': r.Count == 0, 'count': r.Count != 0}">{{r.Count}}</div>
                </div>
            </div>
            <div class="col l9 s12">
                <d3s-resource-responsibility-grid-component [simpleFilter]="showFilter" *ngIf="selected != null"
                                                            [Id]="group?.ID" [type]="'groups'"
                                                            [objectType]="selected.Type"
                                                            [objectId]="selected.TypeID"></d3s-resource-responsibility-grid-component>
            </div>
        </div>
    `
    ,
    providers: [GroupService, ResourcesService]
})

export class GroupResponsibilityComponent extends BaseComponent implements OnChanges {
    @Input() group: Group = null;
    items: CountObject[] = new Array<CountObject>();
    selected: CountObject;
    showFilter: boolean = true;

    constructor(
        private groupService: GroupService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['group'] && this.group) {
            this.load();
        }
    }

    isSelected(item: any) {
        return (item == this.selected);
    }

    select(item: any) {
        this.selected = item;
    }

    load() {
        this.isLoading = true;

        this.groupService.getResponsibilityBreakdownByGroup(this.group.ID).subscribe(
            r => {
                this.items = r;

                if (this.items && this.items.length > 0) {
                    this.select(this.items[0]);
                }

                this.isLoading = false;
            }
        );
    }
}
