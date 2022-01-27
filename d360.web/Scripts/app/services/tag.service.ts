import { catchError, map, publishReplay, refCount } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Tag, TagType, TagApiModel, TagPermissionItem } from '../models/tag.model';
import { Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable({
    providedIn: 'root'
})
export class TagService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTags(phrase: string, excludeObjects: string = ''): Observable<Tag[]> {
        let url = `api/tagsuggestions?phrase=${phrase}&excludeObjects=${excludeObjects}`;

        return this.http.get(url).pipe(
            map(response => {
                return response
            }),
            map(item => { return <Tag[]>item }),
            catchError(err => this.handleError(err)));
    }

    getTagsList(getAll: boolean = true): Observable<TagType[]> {
        let url = `api/v2/tags`;

        if (getAll) {
            url += "?getAll=true";
        }

        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(items => <TagType[]>items.items),
                catchError(err => this.handleError(err)));

    }

    deleteTagByUid(uid: string, cascade: boolean = true): Observable<any> {
        let url = `api/v2/tags/${uid}?cascade=${cascade}`;
        return this.http.delete(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    deleteTags(tags: TagType[]): Observable<any> {
        let url = `api/v2/tags/`;

        if (tags.length == 1)
            return this.deleteTagByUid(tags[0].uid);

        let body: any[] = []
        tags.forEach(t => {
            body.push({ 'uid': t.uid, cascade: true });
        })

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: body
        };
        return this.http.delete(url, httpHeaders)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    saveTag(tag: TagType): Observable<any> {
        let url = `api/v2/tags/`;

        if (tag.uid == undefined || !tag.uid) {
            return this.http.post(url, tag)
                .pipe(map(response => <any>response),
                    catchError(err => this.handleError(err, true)));
        }
        url = `api/v2/tags/${tag.uid}`;
        return this.http.put(url, tag)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }
    createAssetTag(tags: TagApiModel[]): Observable<any> {
        let url = `api/v2/assets/tags`;
        return this.http.post(url, tags)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }
    getAssetTagDetails(tagID: number, assetUID: string): Observable<any> {
        let url = `api/v2/tags/AssetTagDetails?tagID=${tagID}&assetUID=${assetUID}`;
        return this.http.get(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    getAssetTagOwnerByName(tagName: string, assetUid: string): Observable<any> {
        let url = `api/v2/tags/AssetTagOwnerByName?tagName=${tagName}&assetUID=${assetUid}`;
        return this.http.get(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    deleteAssetTag(tags: TagApiModel[]): Observable<any> {

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: tags
        };
        let url = `api/v2/assets/tags`;
        return this.http.delete(url, httpHeaders)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    doesTagExist(tag: TagType): Observable<any> {
        let url = 'api/v2/tags/exists?value=' + encodeURIComponent(tag.Value);
        return this.http.get(url, { observe: "response" })
            .pipe(map((data) => {
                return data.status;
            }));
    }

    consolidateTags(parentTag: string, childrenTags: string[]): Observable<any[]> {
        let url = `api/v2/tags/consolidate/${parentTag}`;
        return this.http.post(url, childrenTags)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    getAssetPathsForTag(tagUid: string): Observable<any[]> {
        let url = `api/v2/tags/${tagUid}/assetpath`;
        return this.http.get(url)
            .pipe(map(response => <any[]>response),
                catchError(err => this.handleError(err, true)))
    }

    searchTags(q: string, exceptId, ignoreCounts: boolean = false): Observable<any[]> {
        let url = `api/v2/tags/search?value=${q}&exceptuid=${exceptId}&ignoreCounts=${ignoreCounts}`;
        return this.http.get(url)
            .pipe(map(response => <any[]>response),
                catchError(err => this.handleError(err, true)))
    }

    searchTagsTypeAhead(q: string, maxNumberOfResults: number = 200): Observable<any[]> {
        let url = `api/v2/tags/search?value=${encodeURIComponent(q)}&maxNumberOfResults=${maxNumberOfResults}`;
        return this.http.get(url)
            .pipe(map(response => <any[]>response),
                catchError(err => this.handleError(err, true)))
    }

    exportTags(filters: any, sort) {
        this.http.get(`api/v2/tags/export?globalSearch=${filters.globalSearch}&value=${filters.Value}&useCount=${filters.UseCount}&sortBy=${sort.field}&sortOrder=${sort.order}`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Tags'));
    }

    exportTagsByUid(uid: string, sort: any, filters: any) {
        var params = "globalSearch=" + filters.globalSearch;

        if (filters.AssetType) {
            params += "&AssetType=" + filters.AssetType;
        }

        if (filters.DisplayValue) {
            params += "&DisplayValue=" + filters.DisplayValue;
        }

        if (filters.TagsAsString) {
            params += "&TagsAsString=" + filters.TagsAsString;
        }

        if (sort.field) {
            params += "&sortBy=" + sort.field;
        }

        if (sort.order) {
            params += "&sortOrder=" + sort.order;
        }

        params += "&_pagesize=1000000";

        this.http.get(`api/v2/tags/${uid}/export?${params}`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Tags'));
    }

    getTagByUid(uid: string): Observable<TagType> {
        let url = `api/v2/tags?uid=${uid}`;

        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(response => <TagType>response.items[0]),
                catchError(err => this.handleError(err)));

    }

    getTagDetails(uid: string): Observable<any> {
        let url = `api/v2/tags/${uid}/details?_pagesize=1000000`;
        return this.http.get(url)
            .pipe(map(response => {
                var data = <any>response;
                if (data.items) {
                    data.items.forEach((tag) => {
                        tag.DisplayPath = (tag.DisplayPath as string).split('/').join('>');
                    });
                }
                return data;
            }), catchError(err => this.handleError(err)));
    }

    private tagTooltipsCache: any[] = [];

    getTagTooltip(tagUid: string, assetUid: string = null, value: string = null): Observable<any> {

        if (tagUid) {
            var cachedItem = this.tagTooltipsCache.find(x => x.tagUid == tagUid && x.assetUid == assetUid);
            if (cachedItem)
                return cachedItem.obs;
        }

        let url = `api/v2/tags/${tagUid}/tooltip`;

        if (!tagUid) {
            url = `api/v2/tags/tooltipByName?tagName=${encodeURIComponent(value)}`;
            if (assetUid != null)
                url += `&assetUid=${assetUid}`;
        }
        else if (assetUid != null)
            url += `?assetUid=${assetUid}`;

        var obs = this.http.get(url)
            .pipe(map(response => <any>response),
                publishReplay(1),
                refCount(),
                catchError(err => this.handleError(err)));

        var data = { tagUid: tagUid, assetUid: assetUid, obs: obs };
        this.tagTooltipsCache.push(data);

        return obs;
    }

    getTagPermissions(assetUid: string): Observable<TagPermissionItem[]> {
        let url = `api/v2/tags/permissions/${assetUid}`;

        return this.http.get(url)
            .pipe(map(response => <TagPermissionItem[]>response),
                catchError(err => this.handleError(err)));
    }

}