import { Component, Input, Output, EventEmitter, OnChanges, AfterViewInit, SimpleChange } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown } from '../../../models/score.model';
import { TreeNode } from 'primeng/api';
import { Item } from '../../../models/metrics.model';
import { validateDashboardLoad } from 'powerbi-models';


@Component({
    selector: 'd3s-object-health-details-item',
    templateUrl: `./object-health-details-item.component.html`,
    providers: [ScoreService],
})

export class ObjectHealthDetailsItemComponent extends BaseComponent implements OnChanges, AfterViewInit {
    @Input() item: TreeNode;
    @Input() definition: any[];
    @Input() isloading: boolean = false;
    private currentItemDetails: any;
    private scoreItemUid: string;
    private scoreItem: any;
    private isCollapsed: boolean = false;
    constructor(protected scoreService: ScoreService) {
        super();
    }
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'definition') {
                requiresLoad = (changes['definition'].currentValue != changes['definition'].previousValue) && changes['definition'] != undefined;
            }
            if (p == 'item') {
                requiresLoad = (changes['item'].currentValue != changes['item'].previousValue) && changes['item'] != undefined;
            }
        }
        if (requiresLoad) {
            this.isLoading = true;
            this.loadItemDetails();
        }
    }
    ngAfterViewInit(): void {
        this.loadItemDetails();
    }
    private toggleDetails() {
        this.isCollapsed = !this.isCollapsed;
    }
    private loadItemDetails() {
        if (this.definition)
            this.getCurrentItemDetails();
    }

    private getCurrentItemDetails() {
        if (this.definition) {
            var definitionItem = this.definition.filter(x => { return x.Uid == this.item.data.Uid })[0];
            if (definitionItem) {
                this.currentItemDetails = definitionItem;
            }
        }
        this.isLoading = false;
    }

    GetChildPropertValue(parent, child, property) {
        if (this.definition && parent && child) {
            let parentItem = this.definition.filter(x => { return x.Uid == parent.Uid })[0];
            if (parentItem && parentItem.Metrics.length > 0) {
                let childItem = parentItem.Metrics.filter(y => { return y.Uid == child.Uid })[0];
                return childItem[property];
            }
        }
    }
}