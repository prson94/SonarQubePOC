
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { DomainType, IDomainService } from '../models/domain.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';

@Injectable()
export class DomainService extends BaseService implements IDomainService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getDomains(): Promise<DomainType[]> {
        return this.http.get('services/domains')
            .toPromise()
            .then(response => <DomainType[]>response.json())
            .catch(err=>this.handleError(err));
    }
}