import { Input, Component} from '@angular/core';

@Component({
    selector: 'd3s-loading',
    template: ` 
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                `    
})

export class LoadingComponent {
    @Input() isLoading: boolean;    
};