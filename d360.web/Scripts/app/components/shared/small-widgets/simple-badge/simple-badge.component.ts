
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-simple-badge',
    templateUrl: './simple-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SimpleBadgeComponent implements OnInit, OnChanges {

    @Input() badgeValue: string;
    @Input() badgeType: string;
    private formattedBadge: string;
    useDefinedColor: boolean;
    undefinedColor: boolean;
    private badgeAttributes: any = null;
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
            this.badgeAttributes = JSON.parse(this.badgeValue);
            if (this.badgeAttributes.some(x=> x.hasOwnProperty('color'))) {
                this.useDefinedColor = true;
                this.undefinedColor = this.badgeAttributes.filter(c => c.color == 'transparent').length > 0;
            } else {
                this.useDefinedColor = false;
                this.undefinedColor = false;
            }
        } catch{
            this.useDefinedColor = false;
            this.undefinedColor = false;
        }
    }

    getBadgeText() {
        if (this.badgeAttributes && this.badgeAttributes.length > 0)
            return this.badgeAttributes.map(c => c.name).join('/');
        else
            return this.badgeValue;
    }

    getBadgeDescription() {
        var title = "Status: ";

        if (this.badgeAttributes && this.badgeAttributes.length > 0 && this.badgeAttributes[0].description) {
            if (this.badgeType.toLowerCase().trim() === 'dataclassification') {
                title="Data Classification: "
            }
            title += this.getBadgeText();
            return title + "\r\n" + this.badgeAttributes[0].description;
        }
        else {
            return null;
        }            
    }

    getBackgroundColor() {
        if (this.useDefinedColor && !this.undefinedColor)
            return this.getBackgroundGradient();
        var badgeValue = this.getBadgeText().toLowerCase().trim();
        if (!this.useDefinedColor) {
            switch (this.badgeType.toLowerCase().trim()) {
                case 'dataclassification':
                    switch (badgeValue) {
                        case 'unclassified':
                            return '#90A4AE';
                        case 'public':
                            return '#43A047';
                        case 'sensitive':
                            return '#FFA900';
                        case 'confidential':
                            return '#E55C57';
                        case 'proprietary':
                            return '#FFE50B';
                        case 'secret':
                            return '#990132';
                        case 'top secret':
                            return '#202020';
                        default:
                            //custom badgeValue, we need to generate a color
                            let hash = 0;
                            for (let i = 0; i < badgeValue.length; i++) {
                                hash = badgeValue.charCodeAt(i) + ((hash << 5) - hash);
                                hash = hash & hash;
                            }
                            this.ref.markForCheck();
                            return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
                    }
                case 'status':
                    switch (badgeValue) {
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
            this.formattedBadge = "";
            let firstToken = true;
            if (this.badgeAttributes.length > 0) {
                let color = "";
                for (var i = 0; i < this.badgeAttributes.length; i++) {
                    let currentToken = this.badgeAttributes[i];
                    color = currentToken.color;
                    let name = currentToken.name;
                    if (!firstToken) this.formattedBadge += "/";
                    this.formattedBadge += name;
                    firstToken = false;
                }
                this.ref.markForCheck();
                return this.hexToHSL(color);
            }
        }
    }

    getBackgroundGradient() {
        if (this.badgeAttributes.length > 0) {
            if (this.badgeAttributes.length == 1) {
                return this.badgeAttributes[0].color;
            }
            let split = Math.round( 100 / this.badgeAttributes.length);
            let gradients = this.badgeAttributes.map(x => {
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
                    case r: h = ((g - b) / d) % 6; break;
                    case g: h = (b - r) / d + 2; break;
                    case b: h = (r - g) / d + 4; break;
                }
                h = Math.round(h * 60);
            }
            h = (360 + h) % 360;
            s = +(s * 100).toFixed(0);
            l = +(l * 100).toFixed(0);
            return "hsl(" + h + "," + s + "%,"+ l + "%)";
        }
        return hex;
    }
    private hslStringIsLight(color: string, lumaLimit: number = 128): boolean {
        if (color == null) {
            return true;
        }
        if (color.substr(0, 1) == '#')
            color = this.hexToHSL(color);
        let hsl = /^hsl\(\s*(\d{1,3})\s*,\s*(0|[1-9]\d?|100)%\s*,\s*(0|[1-9]\d?|100)%\s*\)$/i.exec(color);
        if (hsl) {
            let h = (360 + parseInt(hsl[1], 10)) % 360;
            let s = parseInt(hsl[2], 10) * 0.01;
            let l = parseInt(hsl[3], 10) * 0.01;
            var rgb = this.hsl2rgb(h, s, l).map(x => Math.round(x * 255));
            var luma = ((rgb[0] * 299) + (rgb[1] * 587) + (rgb[2] * 114)) / 1000;
            return (luma >= lumaLimit);
        }
        return true;
    }

    getVariant() {
        var dark = 'custom-dark';
        var light = 'custom-light';
        if (this.undefinedColor)
            return light;
        if (this.useDefinedColor)
            return this.hslStringIsLight(this.getBackgroundColor(), 170) ? light : dark;
        else {
            var badgeValue = this.getBadgeText().toLowerCase().trim();
            switch (this.badgeType.toLowerCase().trim()) {
                case 'dataClassification':
                    switch (badgeValue) {
                        case 'unclassified':
                        case 'confidential':
                            return light;
                        case 'public':
                        case 'sensitive':                        
                        case 'proprietary':
                        case 'secret':
                        case 'top secret':
                            return dark;
                        default:
                            return this.hslStringIsLight(this.getBackgroundColor(), 170) ? light : dark;
                    }
                case 'status':
                    switch (badgeValue) {
                        case 'draft':
                            return light;
                        case 'certified':
                        case 'under review':
                            return dark;
                        default:
                            return this.hslStringIsLight(this.getBackgroundColor(), 170) ? light : dark;
                    }
                default:
                    return this.hslStringIsLight(this.getBackgroundColor(), 170) ? light : dark;
            }
            
        }
    }
}
