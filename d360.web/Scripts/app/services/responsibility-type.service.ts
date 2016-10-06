
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { IResponsibilityTypeService, ResponsibilityType, ResponsibilityTypeGroup, ResponsibilityTypeRelation } from '../models/responsibility-type.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';

@Injectable()
export class ResponsibilityTypeService extends BaseService implements IResponsibilityTypeService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResponsibilityTypes(): Promise<ResponsibilityType[]> {
        return this.http.get('api/ownership/types')
            .toPromise()
            .then(response => <ResponsibilityType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityType(id: number, group: ResponsibilityTypeGroup = ResponsibilityTypeGroup.People): Promise<ResponsibilityType> {
        return this.http.get(`form/ResponsibilityType?id=${id}&group=${group}`)
            .toPromise()
            .then(r => r.json())
            .then(r => {
                //console.log(r);
                let t = new ResponsibilityType();
                t = r.model;
                t.AllocationsList = r.allocations;
                t.ResponsibilityTypeRelations = r.selectedAllocations;
                if (t.ResponsibilityTypeRelations == null)
                    t.ResponsibilityTypeRelations = [];
                return t;
            })
            .catch(err => this.handleError(err));
    }

    putResponsibilityType(responsibilityType: ResponsibilityType): Promise<any> {
        return this.http.put(`form/ResponsibilityType`, responsibilityType)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    postResponsibilityType(responsibilityType: ResponsibilityType): Promise<any> {
        return this.http.post(`form/ResponsibilityType`, responsibilityType)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    deleteResponsibilityType(id: number): Promise<any> {
        return this.http.delete(`form/DeleteResponsibilityTypeByID?id=${id}`)
            .toPromise()
            .catch(err => this.handleError(err));
    }
}

