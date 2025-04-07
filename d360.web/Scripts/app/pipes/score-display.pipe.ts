import { Pipe, PipeTransform } from '@angular/core';


@Pipe({ name: 'scoreDisplay' })
export class ScoreDisplayPipe implements PipeTransform {
    transform(score: number, precision: number = 0, trailingZeros: boolean = false): any {
		if (isNaN(precision)) {
			precision = 0;
		}
		if (score == null) return 'N/A';
		let roundedTxt = ((score * Math.pow(10, 2 + precision)) / Math.pow(10, precision)).toFixed(precision);
		const roundedVal = parseFloat(roundedTxt);

		if (roundedVal === 100 && score < 1) {
			roundedTxt = (roundedVal - Math.pow(10, 0 - precision)).toFixed(precision);
		} else if (roundedVal === 0 && score > 0) {
			roundedTxt = (roundedVal + Math.pow(10, 0 - precision)).toFixed(precision);
		}

		roundedTxt += '%';
		if (precision > 0 && !trailingZeros) {
			roundedTxt = roundedTxt.replace(/(?:0+|\.0+)%/, '%');
		}
		return roundedTxt;
	}
}