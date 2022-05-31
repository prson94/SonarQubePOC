import { Input, Component, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { AdvancedFilteringService, AdvancedFilterUpdate } from '../../assets-grid/advanced-filtering/advanced-filtering.service';
@Component({
    selector: 'gov-relationship-filter',
    templateUrl: './relationship-filter.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class RelationshipFilterComponent {
    @Input() relationshipTypesResolvedNames: any[] = [];

    allSelected = true;
    constructor(private advFilterService: AdvancedFilteringService,
        private cdRef: ChangeDetectorRef) {
        this.advFilterService.onFilterUpdate().subscribe((data) => {
            if (data.source !== this.constructor.name) {
                if (data.fieldName === "relationshiptype" && data.values) {
                    this.cdRef.markForCheck();
                }
            }
        });
    }

    toggleItem($event) {
        this.onFilterChange();
    }

    onFilterChange() {
        var ev = new AdvancedFilterUpdate();
        ev.source = this.constructor.name;
        ev.fieldName = "relationshiptype";
        ev.values = this.relationshipTypesResolvedNames.filter((rt) => rt.isSelected);
        this.advFilterService.updateFilter(ev);
    }

    get hasSelectedValue(): boolean {
        if (!this.relationshipTypesResolvedNames || this.relationshipTypesResolvedNames.length === 0) {
            return false;
        }
        return this.relationshipTypesResolvedNames.some((r) => r.isSelected);
    }
}
