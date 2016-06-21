///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { TreeNode } from 'primeng/primeng';
import { ArtifactTypeEditorModel, ArtifactType } from '../models/artifact-type.model';

@Injectable()
export class ArtifactTypeService {

    constructor(private http: Http) { }

    getArtifactTypeEditor(id: number, parentID: number): Promise<ArtifactTypeEditorModel> {
        return this.http.get(`form/ArtifactType?parentID=${parentID}&id=${id}`)
            .toPromise()
            .then(response => <ArtifactTypeEditorModel>response.json())
            .catch(this.handleError);
    }

    putArtifactType(model: ArtifactTypeEditorModel): Promise<any> {
        return this.http.put('form/ArtifactType', model)
            .toPromise()
            .catch(this.handleError);
    }

    postArtifactType(model: ArtifactTypeEditorModel): Promise<any> {
        return this.http.post('form/ArtifactType', model)
            .toPromise().
            catch(this.handleError);
    }

    getArtifactTypeTree(): Promise<TreeNode[]> {
        return this.http.get('artifacts/types')
            .toPromise()
            .then(response => <TreeNode[]>response.json())
            .then(r => this.formTree(r))
            .catch(this.handleError);
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
            tree.push({ data: d, children: [] });
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });
        console.log(tree);
        return tree;
    }

    private formTreeR(node: TreeNode, data) {
        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}