///<reference path="../es6-shim.d.ts"/>
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';

@Pipe({ name: 'technicalNameToDisplayValue' })
export class TechnicalNameToDisplayValuePipe implements PipeTransform {
    transform(objectType: string): any {
        if (!objectType) return;

        switch (objectType.toUpperCase()) {
            case "ARTIFACTTYPE":
                return "Business Term";
            case "POLICYTYPE":
                return "Policy";
            case "TAXONOMYTYPE":
                return "Model";
            case "FUSIONATTRIBUTETYPE":
                return "Fusion";
            case "RULETYPE":
                return "Rule";                
        }

        return objectType;
    }
}