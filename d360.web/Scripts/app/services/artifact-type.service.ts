import { Injectable } from '@angular/core';
import { TreeNode } from 'primeng/api';
import {
    ArtifactTypeEditorModel,
    ArtifactType
} from '../models/artifact-type.model';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class ArtifactTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService,
        private router: Router
    ) {
        super(messagesService);
    }

    getArtifactTypeEditor(
        id: number,
        parentID: number
    ): Observable<ArtifactTypeEditorModel> {
        return this
            .http
            .get(`form/ArtifactType?parentID=${parentID}&id=${id}`)
            .pipe(
                map(response => <ArtifactTypeEditorModel>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    getArtifactTypeDetails(id: number, redirectToHome: boolean = false): Observable<ArtifactType> {
        return this
            .http
            .get(`api/artifacts/${id}`)
            .pipe(
                map(response => <ArtifactType>response),
                catchError(err => this.handleError(err, false, redirectToHome ? this.router : null))
            )
            ;
    }
    findArtifactTypeByUid(
        tree: TreeNode[],
        uid: string
    ): TreeNode {
        for (var i = 0; i < tree.length; i++) {
            var n;
            if (tree[i].data.uid == uid)
                return tree[i];
            if (tree[i].children && tree[i].children.length > 0) {
                n = this.findArtifactTypeByUid(tree[i].children, uid);
            }
            if (n) return n;
        }
        return null;
    }
    findArtifactTypeById(
        tree: TreeNode[],
        id: number
    ): TreeNode {
        for (var i = 0; i < tree.length; i++) {
            var n;
            if (tree[i].data.ID == id)
                return tree[i];
            if (tree[i].children && tree[i].children.length > 0) {
                n = this.findArtifactTypeById(tree[i].children, id);
            }
            if (n) return n;
        }
        return null;
    }
    private formTree(data): TreeNode[] {
        var tree = new Array<TreeNode>();

        data.filter((d) => d.ParentID == null).forEach((d) => {
            tree.push({ data: d, children: [], expanded: false });
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });

        return tree;
    }

    private formTreeR(
        node: TreeNode,
        data
    ) {
        data.filter(d => d.ParentID == node.data.ID).forEach((d) => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }
}
