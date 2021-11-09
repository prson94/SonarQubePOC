import { Pipe, PipeTransform, Injectable } from '@angular/core';

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
            case "RULETYPE":
                return "Rule";
            case "REFERENCEITEMTYPE":
                return "Reference List";
        }

        return objectType;
    }
}