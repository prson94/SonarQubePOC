
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-status-badge',
    templateUrl: './status-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class StatusBadgeComponent implements OnInit, OnChanges {

    @Input() status: string;
    @Input() igBadgeStyle: boolean = false;
    private formattedStatus: string;
    useDefinedColor: boolean;
    singleUndefinedColor: boolean;
    private colorObjects: any = null;
    private changeWait: any;
    constructor(
        private ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    ngOnInit(): void {
        this.load();
        this.ref.markForCheck();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
        this.ref.markForCheck();
    }

    load() {
        try {
            this.colorObjects = JSON.parse(this.status);
            this.useDefinedColor = true;
            this.singleUndefinedColor = this.colorObjects.length == 1 && this.colorObjects[0].color == 'transparent';
            if (this.singleUndefinedColor)
                this.formattedStatus = this.colorObjects[0].name;
        } catch{
            this.useDefinedColor = false;
            this.singleUndefinedColor = false;
        }
    }

    getStatusText() {
        if (this.useDefinedColor && this.colorObjects.length > 0)
            return this.colorObjects.map(c => c.name).join('/');
        else
            return this.status;
    }

    getBackgroundColor(name: string = "") {
        status = this.status.toLowerCase().trim();
        if (name)
            status = name;
        if (!this.useDefinedColor) {
            switch (status) {
                case 'draft':
                    return '#d1dce4';
                case 'certified':
                    return '#4ecc89';
                case 'under review':
                    return '#e2792a';
                default:
                    //custom status, we need to generate a color
                    let hash = 0;
                    for (let i = 0; i < status.length; i++) {
                        hash = status.charCodeAt(i) + ((hash << 5) - hash);
                        hash = hash & hash;
                    }
                    this.ref.markForCheck();
                    return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
            }
        } else {
            this.formattedStatus = "";
            let firstToken = true;
            if (this.colorObjects.length > 0) {
                let color = "";
                for (var i = 0; i < this.colorObjects.length; i++) {
                    let currentToken = this.colorObjects[i];
                    color = currentToken.color;
                    let name = currentToken.name;
                    if (!firstToken) this.formattedStatus += "/";
                    this.formattedStatus += name;
                    firstToken = false;
                }
                this.ref.markForCheck();
                return this.hexToHSL(color);
            }
        }
    }

    getBackgroundGradient() {
        if (this.colorObjects.length > 0) {
            if (this.colorObjects.length == 1) {
                return this.colorObjects[0].color;
            }
            let split = Math.round( 100 / this.colorObjects.length);
            let gradients = this.colorObjects.map(x => {
                if (x) 
                    return x.color + " " + split + "%"
            });
            return "linear-gradient(100deg, " + gradients.join(",") + ")";
        }

    }

    private hsl2rgb(h:number, s:number, l:number): number[]{
        let a = s * Math.min(l, 1 - l);
        let f = (n, k = (n + h / 30) % 12) => l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
        return [f(0), f(8), f(4)];
    }
    private hexToHSL(hex) {
        var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        if (result) {
            let r = parseInt(result[1], 16);
            let g = parseInt(result[2], 16);
            let b = parseInt(result[3], 16);
            r /= 255, g /= 255, b /= 255;
            var max = Math.max(r, g, b), min = Math.min(r, g, b);
            var h, s, l = (max + min) / 2;
            if (max == min) {
                h = s = 0;
            } else {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                switch (max) {
                    case r: h = (g - b) / d + (g < b ? 6 : 0); break;
                    case g: h = (b - r) / d + 2; break;
                    case b: h = (r - g) / d + 4; break;
                }
                h /= 6;
            }
            return "hsl(" +h + "%" + ","+s + "%" + ","+l + "%" + ")";
        }
        return hex;
    }
    private hslStringIsLight(color: string, lumaLimit: number = 128): boolean {
        if (color == null) {
            return true;
        }
        var hue = parseInt(color.substr(4, color.indexOf(",")), 10);
        hue = (hue + 360) % 360;
        //assuming sat/light is 70%
        var rgb = this.hsl2rgb(hue, 0.7, 0.7).map(x => Math.round(x * 255));
        var luma = ((rgb[0] * 299) + (rgb[1] * 587) + (rgb[2] * 114)) / 1000;
        return (luma >= lumaLimit);
    }

    getForegroundColor() {
        var dark = '#515667';
        var light = '#ffffff';
        if (this.singleUndefinedColor)
            return dark;
        if (this.useDefinedColor) {
            let name = this.colorObjects[0].name;
            let color = this.getBackgroundColor();
            switch (name.toLowerCase().trim()) {
                case 'draft':
                    return dark;
                case 'certified':
                case 'under review':
                    return light;
                default:
                    return this.hslStringIsLight(color, 170) ? dark : light;
            }
        } else {
            switch (this.status.toLowerCase().trim()) {
                case 'draft':
                    return dark;
                case 'certified':
                case 'under review':
                    return light;
                default:
                    return this.hslStringIsLight(this.getBackgroundColor(), 170) ? dark : light;
            }
        }
    }

    getStatusIcon() {
        switch (this.status.toLowerCase().trim()) {
            case 'draft':
                return 'fa-adjust fa-flip-horizontal';
            case 'certified':
                return 'fa-check-circle-o';
            case 'under review':
                return 'fa-circle';
            default:
                return 'fa-circle-o';
        }
    }
};
