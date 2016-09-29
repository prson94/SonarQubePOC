import { Input, Component, OnInit } from '@angular/core';
import { ArtifactTypeService } from '../../services/index';
import { BaseComponent} from '../shared/base.component';
import { ArtifactType } from '../../models/artifact-type.model';


@Component({
    selector: 'd3s-artifact-type-workflow-status',
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                    
                    <div class="col s12 m12 l4">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                            
                                    <header>Propose New Artifact</header>
                
                                </div>  
                            </div>
                            <div class="col s12">
                                <div class="tile tile-detail">                            
                                    <header>Certify Artifact</header>
                
                                </div>  
                            </div>
                            <div class="col s12">
                                <div class="tile tile-detail">                            
                                    <header>Propose New Artifact (Multi-approval)</header>
                
                                </div>  
                            </div>
                        </div>
                    </div>                
                    <div class="col s12 m12 l8">
                        <div class="tile tile-detail">                            
                                    <header>Selected Workflows</header>
                
                        </div>  
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTypeWorkflowStatusComponent extends BaseComponent implements OnInit {
    @Input() artifactType: ArtifactType;
    
    
    constructor() {
        super();
    }

    ngOnInit() {        
    }
    
};