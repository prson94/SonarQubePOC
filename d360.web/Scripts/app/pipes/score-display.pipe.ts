
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';

@Pipe({ name: 'scoreDisplay' })
export class ScoreDisplayPipe implements PipeTransform {
    transform(score: number): any {
        return (score == null) ? 'N/A' : (Math.round(score * 100).toString() + '%');
    }
}