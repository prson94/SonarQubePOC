import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
	selector: 'grid-paging-info',
	standalone: true,
	templateUrl: './grid-paging-info.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class GridPagingInfoComponent {
	@Input() first: number;
	@Input() rows: number;
	@Input() totalRecords: number;

	get startValue() {
		if (this.first != null) {
			return (this.first + 1).toLocaleString();
		}
		return '';
	}

	get endValue() {
		if (this.totalRecords === null) { return ""; }

		if ((this.first + Number(this.rows)) > this.totalRecords) {
			return this.totalRecords.toLocaleString();
		}
		return (this.first + Number(this.rows)).toLocaleString();
	}
}
