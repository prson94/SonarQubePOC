///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Http } from '@angular/http';

@Injectable()
export class PageHeader {
    //http: Http;
    title: string;
    description: string;
    breadcrumbs: any;

    //constructor(http: Http) {
    //    this.http = http;
    //}

    //getInfo(key: string): Promise<void> {
    //    return this.http.get().subscribe();

    //}
}
