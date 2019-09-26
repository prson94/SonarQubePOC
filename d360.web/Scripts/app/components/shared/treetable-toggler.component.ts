import { Component, Input, Output, EventEmitter, NgModule } from '@angular/core';
import { TreeTable } from 'primeng/treetable';

@Component({
    selector: 'd3s-treeTableToggler',
    template: `  
<div style="display: inline-block">
        <a href="#" class="ui-treetable-toggler" *ngIf="rowNode.node.leaf === false || rowNode.level !== 0 || rowNode.node.children && rowNode.node.children.length; else spacer" 
            (click)="onClick($event)" 
            [style.visibility]="rowNode.node.leaf === false || (rowNode.node.children && rowNode.node.children.length) ? 'visible' : 'hidden'" 
            [style.marginLeft]="rowNode.level * 16 + 'px'">
            <i [ngClass]="rowNode.node.expanded ? 'fa fa-fw fa-caret-down' : 'fa fa-fw fa-caret-right'"></i>
        </a>  
        <ng-template #spacer>
            <div [style.marginLeft]="(rowNode.level || 1) * 16 + 'px'"></div>
        </ng-template>
</div>
`
})

export class D3STreeTableToggler {

    @Input() rowNode: any;

    constructor(public tt: TreeTable) { }

    onClick(event: Event) {
        this.rowNode.node.expanded = !this.rowNode.node.expanded;

        if (this.rowNode.node.expanded) {
            this.tt.onNodeExpand.emit({
                originalEvent: event,
                node: this.rowNode.node
            });
        }
        else {
            this.tt.onNodeCollapse.emit({
                originalEvent: event,
                node: this.rowNode.node
            });
        }

        this.tt.updateSerializedValue();
        this.tt.tableService.onUIUpdate(this.tt.value);

        event.preventDefault();
    }
}
