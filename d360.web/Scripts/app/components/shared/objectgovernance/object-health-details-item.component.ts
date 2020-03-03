import { Component, Input, Output, EventEmitter, OnChanges, AfterViewInit, SimpleChange } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { TreeNode } from 'primeng/api';
import { ScoreType } from '../../../models/metrics.model';


@Component({
    selector: 'd3s-object-health-details-item',
    templateUrl: `./object-health-details-item.component.html`,
    providers: [ScoreService],
})

export class ObjectHealthDetailsItemComponent extends BaseComponent implements OnChanges {
    @Input() item: TreeNode;
    @Input() definition: any[];
    @Input() isloading: boolean = false;

    @Input() showtype: ScoreType;
    private currentItemDetails: any;
    private scoreItemUid: string;
    private scoreItem: any;
    private disableToggle: boolean = false;
    public isCollapsed: boolean = false;
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

    public setCollapsed(val: boolean) {
        if (!this.disableToggle)
            this.isCollapsed = val;
    }
    private getReadableValue(value: string) {
        switch (value.toLowerCase()) {
            case 'eq':
                return 'Equals';
            case 'neq':
                return 'Not Equals';
            case 'lt':
                return 'Less Than';
            case 'lte':
                return 'Less Than or Equals';
            case 'gt':
                return 'Greater Than';
            case 'gte':
                return 'Greater Than or Equals';
            default: return '';
        }
    }

    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        return s;   
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
    private showExpand(item) {
        if (this.isloading)
            return;
        if ((!item && !item.data) || !this.currentItemDetails) {
            return;
        }
        if (item.data.IsGroup) {
            if (!item.data.Description && !item.children) {
                return false;
            }
        } else {
            if (!item.data.Description && !this.currentItemDetails.Conditions) {
                return false;
            }
        }
        this.disableToggle = false;
        return true;
    }
}