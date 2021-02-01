import { Input, Component, ChangeDetectionStrategy} from '@angular/core';

@Component({
    selector: 'd3s-loading',
    template: ` 
                <div *ngIf="isLoading && !showTransparentLoader">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    <span style="padding:10px;text-align:center;display: block;">
                        <ng-content></ng-content>
                    </span>
                </div>
                <div *ngIf="isLoading && showTransparentLoader" style="postion:relative;overflow:hidden;width100%;">
                    <div style="position:absolute;top:0;left:0;background:rgba(128,128,128,0.25);height:100%;width:100%;">&nbsp;</div>
                    <div style="padding:10px;text-align:center;position:absolute;top:20%;left:0;height:100%;width:100%;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class LoadingComponent {
    @Input() isLoading: boolean;
    @Input() showTransparentLoader: boolean = false;
}