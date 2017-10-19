import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs/Observable';
import { MapType } from '../models/map.model';

@Injectable()
export class MapsService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }


    public getMapTypes(): Promise<MapType[]> {
        return this.http.get('api/map/types')
            .toPromise()
            .then(response => <MapType[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getMapType(id: number): Promise<MapType> {
        return this.http.get(`api/map/type/${id}`)
            .toPromise()
            .then(response => <MapType>response.json())
            .then(r => r[0])
            .catch(err => this.handleError(err));
    }

    public getMapTypeIntersectTypes(id: number): Promise<any[]> {
        return this.http.get(`api/map/type/${id}/intersectTypes`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    public addMapType(model: MapType): Promise<any> {
        return this.http.post(`form/AddMapType`, model)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    public editMapType(model: MapType): Promise<any> {
        return this.http.post(`form/EditMapType`, model)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    public deleteMapType(id: number): Promise<any> {
        return this.http.delete(`form/DeleteMapType/${id}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    public getMapTypeTemplates(mapTypeId: number): Promise<any[]> {
        return this.http.get(`api/map/type/${mapTypeId}/templates`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }
  
}