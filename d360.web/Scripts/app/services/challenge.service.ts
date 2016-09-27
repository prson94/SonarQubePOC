
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Challenge } from '../models/challenge.model';

@Injectable()
export class ChallengeService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getChallengeInfo(objectID: number, objectType: string): Promise<Challenge> {
        if (objectType != 'Artifact') return Promise.resolve(null);
        return this.http.get(`workflow/ChallengeNotification?id=${objectID}`)
            .toPromise()
            .then(response => <Challenge>response.json())
            .catch(err => this.handleError(err));
    }
}