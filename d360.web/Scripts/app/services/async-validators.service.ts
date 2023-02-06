import { debounceTime, first, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class AsyncValidatorService {

    constructor(private httpClient: HttpClient) {
    }

    public labelUniqueValidator(): AsyncValidatorFn {
        return (control: AbstractControl): Promise<ValidationErrors | null> | Observable<ValidationErrors | null> => {
            const url = `/api/v2/connectorLabels/search?q=${encodeURIComponent(control.value)}&isExact=true`;
			return this
				.httpClient
				.get(url)
				.pipe(
					debounceTime(1000),
					map((response) => <any[]>response))

				.pipe(map((res) => {
					return (res && res.length > 0) ? { "alreadyExists": true } : null;
				})).pipe(first());
        };
    }

    public tagUniqueValidator(): AsyncValidatorFn {

        return (control: AbstractControl): Promise<ValidationErrors | null> | Observable<ValidationErrors | null> => {
            const url = `api/v2/tags/search?value=${control.value}&ignoreCounts=true`;
            return this.httpClient.get(url)
                .pipe(map((response) => <any[]>response))
                .pipe(map((res) => {
                    var doesExist = false;
                    res.forEach((s) => {
                        if (s.name.toLowerCase() === control.value.toLowerCase()) {
                            doesExist = true;
                        }
                    });
                    return doesExist ? { "alreadyExists": true } : null;
                }));
        };
    }
}