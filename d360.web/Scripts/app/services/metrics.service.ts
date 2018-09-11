import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { Group, Map, Item, Condition, MapForm, ConditionForm } from '../models/metrics.model';


@Injectable()
export class MetricsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    public getGroups(): Promise<Group[]> {
        return this.http.get(`/api/metrics/groups`)
            .toPromise()
            .then(response => <Group[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getGroup(id: number): Promise<Group> {
        return this.http.get(`/api/v2/metrics/groups/${id}`)
            .toPromise()
            .then(response => <Group>response.json())
            .catch(err => this.handleError(err));
    }

    public getGroupFormModel(id: number): Promise<Group> {
        return this.http.get(`/api/v2/metrics/groups/${id}`)
            .toPromise()
            .then(response => <Group>response.json())
            .catch(err => this.handleError(err));
    }

    public saveGroup(model: Group): Promise<JsonResult> {
        if (model.ID == null || model.ID < 1) { //add
            return this.http.post('/api/v2/metrics/groups', model)
                .toPromise()
                .then(response => <JsonResult>response.json())
                .catch(err => this.handleError(err));
        } else { //edit
            return this.http.put(`/api/v2/metrics/groups/${model.ID}`, model)
                .toPromise()
                .then(response => <JsonResult>response.json())
                .catch(err => this.handleError(err));
        }
    }

    public deleteGroup(id: number): Promise<JsonResult> {
        return this.http.delete(`/api/v2/metrics/groups/${id}`)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }


    public getItems(): Promise<Item[]> {
        return this.http.get(`/api/metrics/items`)
            .toPromise()
            .then(response => <Item[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getItem(id: number): Promise<Item> {
        return this.http.get(`/api/metrics/item/${id}`)
            .toPromise()
            .then(response => <Item>response.json())
            .catch(err => this.handleError(err));
    }

    public saveItem(item: any): Promise<JsonResult> {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamic(this.http, 'metricitem', item);
        }
        return this.putDynamic(this.http, 'metricitem', item);
    }

    public getMaps(groupId: number): Promise<Map[]> {
        return this.http.get(`/api/metrics/maps/${groupId}`)
            .toPromise()
            .then(response => <Map[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getMapFormModel(id: number): Promise<MapForm> {
        return this.http.get(`/form/metricmap/${id}`)
            .toPromise()
            .then(response => <MapForm>response.json())
            .catch(err => this.handleError(err));
    }

    public saveMap(model: MapForm): Promise<JsonResult> {
        if (model.Map.ID == null || model.Map.ID < 1) {
            return this.http.post('form/MetricMap', model)
                .toPromise()
                .then(response => response.json())
                .catch(err => this.handleError(err));
        } else {
            return this.http.put('form/MetricMap', model)
                .toPromise()
                .then(response => response.json())
                .catch(err => this.handleError(err));
        }
    }

    public getConditions(mapId: number): Promise<Condition[]> {
        return this.http.get(`/api/metrics/map/${mapId}/conditions`)
            .toPromise()
            .then(response => <Condition[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getCondition(mapId: number, fieldTypeId: number = 0): Promise<ConditionForm> {
        return this.http.get(`/form/metriccondition/${mapId}/${fieldTypeId}`)
            .toPromise()
            .then(response => <ConditionForm>response.json())
            .catch(err => this.handleError(err));
    }

    public saveCondition(model: Condition): Promise<JsonResult> {
            return this.http.post('form/MetricCondition', model)
                .toPromise()
                .then(response => response.json())
                .catch(err => this.handleError(err));
    }

    public getLookupList(id: number): Promise<any[]> {
        return this.http.get(`api/lookup/list/${id}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getConditionFields(assetTypeId: number): Promise<any[]> {
        return this.http.get(`api/metrics/condition/fields/${assetTypeId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

}