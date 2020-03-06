import { Pipe, PipeTransform, Injectable } from '@angular/core';


@Pipe({ name: 'scoreDisplay' })
export class ScoreDisplayPipe implements PipeTransform {
    transform(score: number, precision: number = 0, trailingZeros: boolean = false): any {
        if (isNaN(precision))
            precision = 0;
        var retval = (score == null) ? 'N/A' : ((score * Math.pow(10, 2 + precision)) / Math.pow(10, precision)).toFixed(precision) + '%';
        if (precision > 0 && !trailingZeros)
            retval = retval.replace(/\.0+%/,'%');
        return retval
    }
}