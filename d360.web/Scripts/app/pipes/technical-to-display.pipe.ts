import { Pipe, PipeTransform, Injectable } from '@angular/core';

@Pipe({ name: 'technicalNameToDisplayValue' })
export class TechnicalNameToDisplayValuePipe implements PipeTransform {
    transform(objectType: string): any {
        if (!objectType) return;

        switch (objectType.toUpperCase()) {
            case "ARTIFACTTYPE":
                return $localize`Business Term`;
            case "POLICYTYPE":
                return $localize`Policy`;
            case "TAXONOMYTYPE":
                return $localize`Model`;
            case "RULETYPE":
                return $localize`Rule`;
            case "REFERENCEITEMTYPE":
                return $localize`Reference List`;
        }

        return objectType;
    }
}