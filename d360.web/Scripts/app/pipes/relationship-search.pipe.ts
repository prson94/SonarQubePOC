///<reference path="../es6-shim.d.ts"/>
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';
import { Relationship } from '../models/relationship.model';



@Pipe({name: 'relationshipSearch'})
export class RelationshipSearchPipe implements PipeTransform {
    transform(items: Relationship[], searchTerm: string): any {        
        if (!searchTerm || searchTerm.length == 0) return items;

        let search = searchTerm.toLowerCase();

        return items.filter(item => item.Source && item.Source.toLowerCase().includes(search) || item.Target && item.Target.toLowerCase().includes(search) || item.SourceName && item.SourceName.toLowerCase().includes(search) || item.TargetName && item.TargetName.toLowerCase().includes(search));      
    }
}