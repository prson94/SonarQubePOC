///<reference path="./es6-shim.d.ts"/>
import { Injectable } from 'angular2/core';

@Injectable()
export class PageHeader {
    title: string;
    description: string;
    breadcrumbs: any;
}
