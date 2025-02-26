import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
	selector: 'loading',
	templateUrl: "./loading.html",
	standalone: true,
	imports: [],
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class LoadingComponent {
    @Input() isLoading: boolean;
	@Input() showTransparentLoader: boolean = false;
	@Input() inline: boolean = false;
}