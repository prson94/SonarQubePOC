
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { MapRuleItemDetail } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-technical-mappings',
    template: ` 
        <p-treeTable [value]="technicalMappingsTree">
        <p-column header="Group">
            <template let-row="rowData" pTemplate type="body">
                <div *ngIf="row.data.ID != 0">{{row.data.ID}}</div>
                <div *ngIf="row.data.ID == 0">NONE</div>
            </template>
        </p-column>
            <p-column header="Transformation">
                <template pTemplate type="body" let-item="rowData">
                    <div [innerHtml]="item.Transformation"></div>
                </template>
            </p-column>
            <p-column header="Source Object" field="SourceObjectName"></p-column>
            <p-column header="Source Configuration" field="SourceFusion"></p-column>
            <p-column header="Source Attribute" field="SourceFusionAttributeTextPath"></p-column>
            <p-column header="Target Object" field="TargetObjectName"></p-column>
            <p-column header="Target Configuration" field="TargetFusion"></p-column>
            <p-column header="Target Attribute" field="TargetFusionAttributeTextPath"></p-column>
            <p-column header="">
                <template pTemplate type="body" let-row="rowData">
                    <div *ngIf="row.data.ParentTextID == null" class="RowTools">
                        <span><i class="fa fa-plus"></i></span>
                        <span><i class="fa fa-pencil"></i></span>
                        <span><i class="fa fa-trash-o"></i></span>
                    </div>
                    <div *ngIf="row.data.ParentTextID != null" class="RowTools">
                        <span><i class="fa fa-pencil"></i></span>
                        <span><i class="fa fa-trash-o"></i></span>
                    </div>
                </template>
            </p-column>
        </p-treeTable>
                `,
    providers: [FusionService]
})

export class FusionTechnicalMappingsComponent extends BaseComponent implements OnInit {
    technicalMappings: MapRuleItemDetail[];
    technicalMappingsTree: TreeNode[];


    constructor( private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.fusionService.getFusionTechnicalMappings().then(m => {
            console.log(m);
            this.technicalMappingsTree = [];
            this.technicalMappings = m.filter(i => i.Type == "MapRule");
            
            for (let t of this.technicalMappings) {
                let n: TreeNode = {};
                n.data = t;
                n.children = [];
                
                t.children = m.filter(i => i.Type == "MapRuleItem" && i.ParentTextID == t.TextID);
                for (let c of t.children) {
                    let cn: TreeNode = {};
                    cn.data = c;
                    n.children.push(cn);
                }

                this.technicalMappingsTree.push(n);
            }
            console.log(this.technicalMappingsTree);

        });
    }

};