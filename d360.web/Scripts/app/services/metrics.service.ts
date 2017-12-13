import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { Group, Map, Item } from '../models/metrics.model';


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
        return this.http.get(`/api/metrics/group/${id}`)
            .toPromise()
            .then(response => <Group>response.json())
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

}