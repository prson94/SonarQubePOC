import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { TreeNode } from 'primeng/components/common/api';
import { ArtifactTypeEditorModel, ArtifactType, ArtifactTypeSummary, ArtifactTypeStatusCount, ArtifactTypeUsedVsUnusedResponsibility } from '../models/artifact-type.model';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ArtifactTypeService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getArtifactTypeEditor(id: number, parentID: number): Promise<ArtifactTypeEditorModel> {
        return this.http.get(`form/ArtifactType?parentID=${parentID}&id=${id}`)
            .toPromise()
            .then(response => <ArtifactTypeEditorModel>response.json())
            .catch(err=>this.handleError(err));
    }

    getArtifactTypeDetails(id: number): Promise<ArtifactType> {
        return this.http.get(`api/artifacts/${id}`)
            .toPromise()
            .then(response => <ArtifactType>response.json())
            .catch(err => this.handleError(err));
    }

    putArtifactType(model: ArtifactTypeEditorModel): Promise<any> {
        return this.http.put('form/ArtifactType', model)
            .toPromise()
            .then(response => response.json())
            .catch(err=>this.handleError(err));
    }
    
    getArtifactTypeTree(): Promise<TreeNode[]> {
        return this.http.get('internal/artifacts/types')
            .toPromise()
            .then(response => <TreeNode[]>response.json())
            .then(r => this.formTree(r))
            .catch(err=>this.handleError(err));
    }

    findArtifactType(tree: TreeNode[], id: number): TreeNode {
        for (var i = 0; i < tree.length; i++) {
            var n;
            if (tree[i].data.ID == id)
                return tree[i];
            if (tree[i].children && tree[i].children.length > 0) {
                n = this.findArtifactType(tree[i].children, id);
            }
            if (n) return n;
        }
        return null;
    }

    private formTree(data): TreeNode[] {
        var tree = new Array<TreeNode>();

        data.filter(d => d.ParentID == null).forEach(d => {
            tree.push({ data: d, children: [], expanded:false });
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });        
        return tree;
    }

    private formTreeR(node: TreeNode, data) {
        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }
    
    public getArtifactTypeStatus(artifactTypeId: number): Promise<ArtifactTypeStatusCount[]> {
        return this.http.get(`/queries/${artifactTypeId}/StatusBreakdownByArtifactType`)
            .toPromise()
            .then(response => <ArtifactTypeStatusCount[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getArtifactTypeUsedVsUnusedResponsibilities(artifactTypeId: number): Promise<ArtifactTypeUsedVsUnusedResponsibility[]> {
        return this.http.get(`queries/${artifactTypeId}/UsedVsUnusedResponsibilitiesByArtifactType`)
            .toPromise()
            .then(response => <ArtifactTypeUsedVsUnusedResponsibility[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getPossibleArtifactOwners(artifactTypeId: number): Promise<any[]> {
        return this.http.get(`/api/artifacttype/possibleowners/${artifactTypeId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    public deleteArtifactType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'artifacttype', id);
    }

    public getFilterListItems(id: number, type: string, fieldTypeId: number) {
        return this.http.get(`api/${type}/${id}/grid/definition/filterValues/${fieldTypeId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getObjectTypeParentsListItems(id: number, type: string) {
        return this.http.get(`api/${type}/${id}/grid/definition/parentValues`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}