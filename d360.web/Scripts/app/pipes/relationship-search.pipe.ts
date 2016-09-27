
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';
import { Relationship } from '../models/relationship.model';



@Pipe({name: 'relationshipSearch'})
export class RelationshipSearchPipe implements PipeTransform {
    transform(items: Relationship[], searchTerm: string): any {        
        if (!searchTerm || searchTerm.length == 0) return items;

        let search = searchTerm.toLowerCase();

        return items.filter(item => item.Object && item.Object.toLowerCase().includes(search) || item.Subject && item.Subject.toLowerCase().includes(search) || item.ObjectName && item.ObjectName.toLowerCase().includes(search) || item.SubjectName && item.SubjectName.toLowerCase().includes(search) || item.PredicateName && item.PredicateName.toLowerCase().includes(search));      
    }
}