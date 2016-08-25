///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Input, Output, EventEmitter, ElementRef } from '@angular/core';
import { NavBarMode } from '../../models/nav-bar.model';

@Component({
    selector: 'd3s-navbar-menu',
    styles: [`
        .navbar-menu {
            background-color: #1E1A15;
        }
        .navbar-menu-list {
            display:inline;
        }
        .navbar-menu-item {
            display:inline-block;
            padding: 10px;
            background-color: #1E1A15;
            color:white;
            transition: all 200ms ease-in-out;
            cursor: pointer;
            width: 44px;
            margin-left: -4px;
        }

        .navbar-menu-item.selected {
            cursor: default;
            background-color: #383127;
        }

        .navbar-menu-item.selected:hover {
            background-color: #383127;
        }

        .navbar-menu-item:hover {
            background-color: #82705C;
        }
    
  `],
    template: `
                <div class="navbar-menu">
                    <ul class="navbar-menu-list">
                        <li (click)="handleClick(NavBarMode.Default)" style="margin: 0;" class="navbar-menu-item" [class.selected]="mode == NavBarMode.Default"><i class="fa fa-2x fa-home"></i></li>
                        <li (click)="handleClick(NavBarMode.Favorites)" class="navbar-menu-item" [class.selected]="mode == NavBarMode.Favorites"><i class="fa fa-2x fa-star"></i></li>
                        <!--<li (click)="handleClick(NavBarMode.Edit)" class="navbar-menu-item" [class.selected]="mode == NavBarMode.Edit"><i class="fa fa-2x fa-cog"></i></li>-->
                    </ul>
                </div>
`
})

export class NavBarMenuComponent implements OnInit {
    @Input() mode: NavBarMode = NavBarMode.Default;
    @Output() modeChange = new EventEmitter<NavBarMode>();

    NavBarMode = NavBarMode;

    constructor(private elementRef: ElementRef) {
    }

    ngOnInit() {
    }

    handleClick(mode: NavBarMode) {
        this.modeChange.emit(mode);
    }
}
