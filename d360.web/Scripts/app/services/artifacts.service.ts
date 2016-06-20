///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { TreeNode } from 'primeng/primeng';

@Injectable()
export class ArtifactsService {

    constructor(private http: Http) { }

    getArtifactTypeTree(): Promise<TreeNode[]> {
        return this.http.get('artifacts/types')
            .toPromise()
            .then(response => <TreeNode[]>response.json())
            .then(r => this.formTree(r))
            .catch(this.handleError);
    }

    private formTree(data): TreeNode[] {
        var tree = new Array<TreeNode>();

        data.filter(d => d.ParentID == null).forEach(d => {
            tree.push({ data: d, children: [] });
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

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}