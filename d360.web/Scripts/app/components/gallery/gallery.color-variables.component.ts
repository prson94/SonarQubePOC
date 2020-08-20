import { Component, OnInit, AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-color-variables',
    templateUrl: './gallery.color-variables.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .colorsample {
            padding: 3px;
        }
        div.colorsample {
            width: 100px;
            margin-bottom: 6px;
            margin-right: 6px;
            display: inline-block;
        }
        table.samples {
            border-spacing: 6px;
            border-collapse: separate;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryColorVariablesComponent implements OnInit, AfterViewInit {
    protected tintedbasecolors: Array<string> = ['grey', 'midblue', 'lightblue', 'red', 'coral', 'orange', 'green'];
    protected tintsshaded: Array<string> = [ 't5', 't4', 't3', 't2', 't1', 'Base', 's1', 's2', 's3'];
    protected nontintedbasecolors: Array<string> = ['black', 'white', 'slate'];
    protected basecolors: Array<string>;
    protected hexcolors: Map<string, string> = new Map<string, string>();

    constructor(private cdr: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.basecolors = this.nontintedbasecolors.concat(this.tintedbasecolors);
    }

    ngAfterViewInit(): void {
        for (let base of this.nontintedbasecolors) {
            let cls = this.getClassName(base, 'Base');
            this.hexcolors.set(cls, this.findColor(cls));
        }
        for (let base of this.tintedbasecolors) {
            for (let tint of this.tintsshaded) {
                let cls = this.getClassName(base, tint);
                this.hexcolors.set(cls, this.findColor(cls));
            }
        }
        this.cdr.detectChanges();
    }

    getClassName(base: string, tint: string): string {
        if (tint == 'Base')
            return 'ig-'+base;
        return 'ig-'+base + '-' + tint;
    }

    getHex(base: string, tint: string): string {
        if (this.hexcolors.has(this.getClassName(base, tint)))
            return this.hexcolors.get(this.getClassName(base, tint));
        return '';
    }

    private findColor(cls: string): string {
        let el = document.querySelector('.' + cls);
        let col = window.getComputedStyle(el).getPropertyValue('background-color');
        return this.rgbToHex(col);
    }

    private rgbToHex(rgb: string): string {
        return '#' + rgb.match(/\d+/g).map(c => ((+c < 16) ? '0' : '') + (+c).toString(16)).join('');
    }
}
