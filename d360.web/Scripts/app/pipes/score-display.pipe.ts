import { Pipe, PipeTransform, Injectable } from '@angular/core';


@Pipe({ name: 'scoreDisplay' })
export class ScoreDisplayPipe implements PipeTransform {
    transform(score: number, precision:number = 0): any {
        if (isNaN(precision))
            precision = 0;
        return (score == null) ? 'N/A' : (Math.round((score * Math.pow(10, 2 + precision)) / Math.pow(10, precision)).toFixed(precision) + '%');
    }
}