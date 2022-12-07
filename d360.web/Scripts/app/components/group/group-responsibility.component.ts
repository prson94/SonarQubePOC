import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { GroupService } from '../../services/group.service';
import { ResourcesService } from '../../services/resources.service';
import { CountObject } from '../../models/resource.model';
import { Group } from '../../models/group.model';
import { BaseComponent } from '../shared/base.component';
import { CompanySettingsService } from '../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-group-responsibility',
    templateUrl: 'group-responsibility.component.html',
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
        return (item === this.selected);
    }

    select(item: any) {
        this.selected = item;
    }

    load() {
        this.isLoading = true;

        this.groupService.getResponsibilityBreakdownByGroup(this.group.ID).subscribe(
            (r) => {
                this.items = r;

                if (this.items && this.items.length > 0) {
                    this.select(this.items[0]);
                }

                this.isLoading = false;
            }
        );
    }
}
