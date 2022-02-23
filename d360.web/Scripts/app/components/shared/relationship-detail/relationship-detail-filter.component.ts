import { Input, Component, ChangeDetectionStrategy } from '@angular/core';
@Component({
    selector: 'gov-relationship-detail-filter',
    templateUrl: './relationship-detail-filter.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class RelationshipDetailFilterComponent {
    @Input() relationshipTypesResolvedNames: any[] = [];

    allSelected = true;
    constructor() {
    }

    toggleAll($event) {
        this.relationshipTypesResolvedNames.forEach((rt) => rt.isSelected = this.allSelected);
    }
}
