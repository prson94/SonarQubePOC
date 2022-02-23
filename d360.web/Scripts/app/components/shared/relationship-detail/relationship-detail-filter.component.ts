import { Input, Component, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { AdvancedFilteringService, AdvancedFilterUpdate } from '../../assets-grid/advanced-filtering/advanced-filtering.service';
@Component({
    selector: 'gov-relationship-detail-filter',
    templateUrl: './relationship-detail-filter.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class RelationshipDetailFilterComponent {
    @Input() relationshipTypesResolvedNames: any[] = [];

    allSelected = true;
    constructor(private advFilterService: AdvancedFilteringService,
        private cdRef: ChangeDetectorRef) {
        this.advFilterService.onFilterUpdate().subscribe((data) => {
            if (data.source !== this.constructor.name) {
                if (data.fieldName === "relationshiptype" && data.values) {
                    let values: string[] = data.values.map((x) => x.value.toLowerCase());
                    this.relationshipTypesResolvedNames.forEach((rt) => {
                        rt.isSelected = false;
                        values.forEach((val) => {

                            if (val === rt.name.toLowerCase()) {
                                rt.isSelected = true;
                            }
                        })
                    });
                    this.cdRef.markForCheck();
                }
            }
        });
    }

    toggleAll($event) {
        this.relationshipTypesResolvedNames.forEach((rt) => rt.isSelected = this.allSelected);
        this.onFilterChange();
    }
    toggleItem($event) {
        this.onFilterChange();
    }

    onFilterChange() {
        var ev = new AdvancedFilterUpdate();
        ev.source = this.constructor.name;
        ev.fieldName = "relationshiptype";
        ev.values = this.relationshipTypesResolvedNames.filter((rt) => rt.isSelected).map((m) => m.name);
        this.advFilterService.updateFilter(ev);
    }
}
