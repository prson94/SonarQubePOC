///<reference path="../es6-shim.d.ts"/>
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';



@Pipe({
    name: 'filterName',
    pure: false
})
@Injectable()
export class FilterPipeName implements PipeTransform {
    transform(items: any[], args: any[]): any {
        return items.filter(item => item.Name.indexOf(args[0].Name) !== -1);
    }
}