import {Injectable} from '@angular/core';
import {TreeNode} from 'primeng/components/common/api';
import {
    ArtifactTypeEditorModel,
    ArtifactType,
    ArtifactTypeStatusCount
} from '../models/artifact-type.model';
import {BaseObservableService} from './baseObservable.service';
import {MessagesObservableService} from './messages-observable.service';
import {JsonResult} from '../models/jsonresult.model';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

@Injectable()
export class ArtifactTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
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

    getArtifactTypeDetails(id: number): Observable<ArtifactType> {
        return this
            .http
            .get(`api/artifacts/${id}`)
            .pipe(
                map(response => <ArtifactType>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    putArtifactType(model: ArtifactTypeEditorModel): Observable<any> {
        return this
            .http
            .put('form/ArtifactType', model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    getArtifactTypeTree(): Observable<TreeNode[]> {
        return this
            .http.get('internal/artifacts/types')
            .pipe(
                map(response => <TreeNode[]>response),
                map(r => this.formTree(r)),
                catchError(err => this.handleError(err))
            )
            ;
    }

    findArtifactType(
        tree: TreeNode[],
        id: number
    ): TreeNode {
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
            tree.push({data: d, children: [], expanded: false});
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
        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = {data: d, children: []};
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }

    public getArtifactTypeStatus(artifactTypeId: number): Observable<ArtifactTypeStatusCount[]> {
        return this
            .http
            .get(`/queries/${artifactTypeId}/StatusBreakdownByArtifactType`)
            .pipe(
                map(response => <ArtifactTypeStatusCount[]>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    public getPossibleArtifactOwners(artifactTypeId: number): Observable<any[]> {
        return this
            .http
            .get(`/api/artifacttype/possibleowners/${artifactTypeId}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    public deleteArtifactType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'artifacttype', id);
    }

    public getFilterListItems(
        id: number,
        type: string,
        fieldTypeId: number
    ) {
        return this
            .http
            .get(`api/${type}/${id}/grid/definition/filterValues/${fieldTypeId}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    public getObjectTypeParentsListItems(
        id: number,
        type: string
    ) {
        return this
            .http
            .get(`api/${type}/${id}/grid/definition/parentValues`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            )
            ;
    }
}
