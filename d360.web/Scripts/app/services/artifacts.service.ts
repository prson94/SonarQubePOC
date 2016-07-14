///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class ArtifactService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    
}