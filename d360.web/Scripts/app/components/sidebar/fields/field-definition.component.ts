import {Component, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {ObjectDetailService} from '../../../services/object-detail.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-field-definition',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="row"
             *ngIf="!isLoading">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-field-definition-tile [objectID]="objectID"
                                               [objectType]="objectType"
                                               [title]="objectName"></d3s-field-definition-tile>
                </div>
            </div>
        </div>
    `,
    providers: [ObjectDetailService]
})

export class FieldDefinitionComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    objectID: number;
    objectType: string;
    objectName: string;

    constructor(
        private objectDetailService: ObjectDetailService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(
            params => {
                this.objectID = +params['objectId']; // (+) converts string 'id' to a number
                this.objectType = params['objectType'];

                this.objectDetailService.getObject(this.objectID, this.objectType).subscribe(
                    res => {
                        if (res) {
                            this.objectName = 'Field Definitions for ' + (res.Name ? res.Name : res.DisplayValue);
                        }
                    }
                );
            }
        );
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    load() {

    }
}
