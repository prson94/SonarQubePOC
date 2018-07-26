import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { HierarchyModel, PredicateType, HierarchyArtifactsModel, HierarchyArtifactItem, HierarchyPostModel } from '../../models/relations.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeNode } from 'primeng/primeng';
import { RelationshipsService } from '../../services/relationships.service';
import { FormHelper } from '../../models/form.model';


@Component({
    selector: 'd3s-structure-tile',
    styles: [
        `
        .row-item {
            font-size:14px;
            font-weight:600;
        }

        .item-type {
            font-size:.7em;
            font-weight:normal;
        }
        `
    ],
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s12 m6">
                            <p-treeTable [value]="items" selectionMode="single" [(selection)]="selectedRow" (onNodeSelect)="selectRow()">
                                <ng-template pTemplate="header">
	                                <tr>
		                                <th></th>
	                                </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-rowNode let-item="rowData">
	                                <tr [ttSelectableRow]="rowNode">
		                                <td>
			                                <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
			                                <div class="row-item">
                                                <span [style.color]="((item.Level > 0) ? (item.ObjectID == objectID && item.Object == objectType) : (item.SubjectID == objectID && item.Subject == objectType)) ? '#00C' : '#000'" >{{item.Name}}</span>&nbsp;&nbsp;<span class="item-type">{{item.ObjectTypeName}}</span>
                                            </div>
		                                </td>
	                                </tr>
                                </ng-template>
                            </p-treeTable>
                        </div>
                        <div class="col s12 m6">
                            <div *ngIf="isEditorLoading">
                                <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
                            </div>
                            <div [ngSwitch]="formMode" *ngIf="!isEditorLoading">
                                <div *ngSwitchDefault>
                                    TODO: replace this with new menu (d3s-tile-actions)
                                    <!-- replace with d3s-tile-actions -->
                                   <!-- <d3s-action-bar [items]="actions" (onClick)="action($event)"></d3s-action-bar> -->
                                </div>
                                <div *ngSwitchCase="FormMode.Delete">
                                    <div>
                                        Are you sure you want to remove {{selectedRow?.data?.Name}} ?
                                    </div>
                                    <button pButton type="button" label="Remove" (click)="delete()"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                                </div>
                                <div *ngSwitchCase="FormMode.Parent">
                                    <div class="FieldName">Choose an artifact</div>
                                    <div>                                    
                                        <select [(ngModel)]="selectedArtifact">
                                            <option *ngFor="let a of artifacts" [value]="a.Object + a.ObjectID.toString()">{{a.DisplayName}}</option>
                                        </select>
                                    </div>
                                    <button pButton type="button" label="Add Parent" (click)="add(true)"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                                </div>
                                <div *ngSwitchCase="FormMode.Child">
                                    <div class="FieldName">Choose an artifact</div>
                                    <div>                                    
                                        <select [(ngModel)]="selectedArtifact">
                                            <option *ngFor="let a of artifacts" [value]="a.Object + a.ObjectID.toString()">{{a.DisplayName}}</option>
                                        </select>
                                    </div>
                                    <button pButton type="button" label="Add Child" (click)="add()"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                `,
    providers: [ObjectDetailService, RelationshipsService],
})

export class StructureTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean;

    private isLoading = false;
    private isEditorLoading = false;
    private hasChanges = false;
    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    private hierarchyArtifactsModel: HierarchyArtifactsModel = null;
    private artifacts: HierarchyArtifactItem[];
    private selectedArtifact: string;

    items: TreeNode[] = [];
    selectedRow: TreeNode;

    actions: any[] = [];

    constructor(private objectDetailService: ObjectDetailService, private relationshipService: RelationshipsService) {

        this.actions.push({
            icon: 'level-up',
            title: 'add parent',
            key: 'parent',
            tooltip: 'add a parent',
            disabled: true,
            menu: null,
            data: null,
        });

        this.actions.push({
            icon: 'level-down',
            title: 'add child',
            key: 'child',
            tooltip: 'add a child',
            disabled: true,
            menu: null,
            data: null,
        });

        this.actions.push({
            icon: 'trash-o',
            title: 'delete selected artifact',
            key: 'delete',
            tooltip: 'delete selected artifact',
            disabled: true,
            menu: null,
            data: null,
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;
        this.objectDetailService.getRelationsHierarchyTree(PredicateType.TypeHierarchy, this.objectType, this.objectID)
            .then(d => {                
                this.items = d;
                this.isLoading = false;
            });
    }

    action(action: any) {
        switch ((action.key || '').toLowerCase().trim()) {
            case 'delete':
                this.formMode = FormMode.Delete;
                break;

            case 'child':
            case 'parent':
                this.hierarchyArtifactsModel = new HierarchyArtifactsModel();

                if (this.selectedRow == null)
                    return;

                let mapID = 0;
                let groupNumber = 0;

                this.hierarchyArtifactsModel.GroupNumber = this.selectedRow.data.GroupNumber || 0;
                this.hierarchyArtifactsModel.IntersectMapID = this.selectedRow.data.ID || 0;
                this.hierarchyArtifactsModel.IsAddingParent = false;
                this.hierarchyArtifactsModel.MapType = PredicateType.TypeHierarchy;
                this.hierarchyArtifactsModel.ID = this.objectID;
                this.hierarchyArtifactsModel.Type = this.objectType;

                this.isEditorLoading = true;
                this.relationshipService.getHierarchyArtifacts(this.hierarchyArtifactsModel)
                    .then(d => {
                        this.selectedArtifact = null;
                        this.artifacts = d;
                        this.isEditorLoading = false;

                        let mode = (action.key || '').toLowerCase().trim();
                        if (mode == 'child')
                            this.formMode = FormMode.Child;
                        else
                            this.formMode = FormMode.Parent;
                    });

                break;

            default:
                break;
        }
    }

    delete() {
        this.formMode = FormMode.Default;
        if (!this.selectedRow || !this.selectedRow.data.ID)
            return;
        this.isLoading = true;
        this.relationshipService.deleteHierarchyItem(this.selectedRow.data.ID)
            .then(() => {
                this.isEditorLoading = false;
                this.load();
            });
    }

    add(isAddingParent: boolean = false) {
        let artifact = this.artifacts.find(a => a.Object + a.ObjectID.toString() == this.selectedArtifact);

        if (!this.selectedRow || !this.selectedRow.data.ID || !artifact) {
            this.formMode = FormMode.Default;
            return;
        }

        var model = new HierarchyPostModel();
        model.Subject = (this.selectedRow.data.Level > 0) ? this.selectedRow.data.Object : this.selectedRow.data.Subject;
        model.SubjectID = (this.selectedRow.data.Level > 0) ? this.selectedRow.data.ObjectID : this.selectedRow.data.SubjectID;
        model.Object = artifact.Object;
        model.ObjectID = artifact.ObjectID;
        model.IsAddingParent = isAddingParent;
        model.HierarchyType = PredicateType.TypeHierarchy;
        model.GroupNumber = this.selectedRow.data.GroupNumber;
        model.IntersectMapID = (this.selectedRow.data.ID || 0);

        this.isLoading = true;

        this.relationshipService.postHierarchy(model)
            .then(d => {
                console.log(d);
                this.formMode = FormMode.Default;
                this.load();
            });
    }

    selectRow() {
        this.formMode = FormMode.Default;
        if (this.selectedRow == null) {
            this.actions.forEach(a => {
                a.disabled = true;
            });
        } else {
            this.actions.forEach(a => {
                a.disabled = false;
            });
        }
    }
}

 enum FormMode {
    Default,
    Parent,
    Child,
    Delete,
}

