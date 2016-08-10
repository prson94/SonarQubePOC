///<reference path="../es6-shim.d.ts"/>
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';

@Pipe({ name: 'classificationTypeDisplayValue' })
@Injectable()
export class ClassificationTypePipe implements PipeTransform {
    transform(classification: number): any {
        return classification == 1 ? "Critical" : "Normal";
    }
}