///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Challenge } from '../../models/challenge.model';
import { ChallengeService } from '../../services/index';

@Component({
    selector: 'd3s-object-challenge',
    providers: [ChallengeService],
    template: `
            <header>Challenge</header>            
            <div *ngIf="!hasChallenge()" class="row governance-value governance-value-pass">
                <div class="col s12">
                    <i class="fa fa-circle-o" aria-hidden="true"></i>                
                </div>                
            </div>
            <div *ngIf="!hasChallenge()" class="row">
                <div class="col s12">
                    No current challenges.
                </div>                
            </div>
            <div class="row">
                <div class="col s12">
                    <div *ngIf="hasChallenge()" class="row governance-value governance-value-fail">
                        <div class="col s12">
                            <i class="fa fa-circle" aria-hidden="true"></i>                
                        </div>                
                    </div>
                    <div *ngIf="hasChallenge()" class="row">
                        <div class="col s12">
                            Outstanding challenge: <span [innerHtml]="challenge?.Reason"></span>
                        </div>                
                    </div>
                </div>
            </div>
            
        `
})

export class ObjectChallengeComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;

    challenge: Challenge;
    
    constructor(protected challengeService: ChallengeService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    hasChallenge(): boolean {
        return this.challenge && this.challenge.ResourceID != undefined;
    }

    load() {
        this.isLoading = true;
        this.challengeService.getChallengeInfo(this.objectID, this.objectType)
            .then(result => {
                this.challenge = result;                
                this.isLoading = false;
            });
    }

}