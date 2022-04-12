import { CommonModule } from "@angular/common";
import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit } from "@angular/core";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ThirdPartyLibraryListModule } from "@precisely/prism-ng/third-party-library-list";
import { LicenseInformationModel } from "@precisely/prism-ng/third-party-library-list/Models/third-party-license-model";

@Component({
    selector: "ig-thirdpartylicenses",
    template: `<div>
    <div *ngIf="loadError" [innerText]="loadErrorMessage"></div>
    <ng-container *ngIf="!loading">
        <png-third-party-library-list [licenseInformation]="licensemodel"></png-third-party-library-list>
    </ng-container>
    </div>`,
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ThirdPartyLicenses implements OnInit {
    @Input() src: string;

    public licensemodel: LicenseInformationModel;
    public loadError: boolean = false;
    public loading: boolean = true;

    loadErrorMessage: string = "Third Party License information could not be loaded.";

    constructor(public ref: ChangeDetectorRef, private http: HttpClient) { }

    ngOnInit() {
        const headers = new HttpHeaders({ "Content-Type": "application/json" })
        this.http.get<LicenseInformationModel>(this.src, { headers }).subscribe(
            (res) => {
                this.licensemodel = res;
                this.loading = false;
                this.ref.markForCheck();
            },
            (err) => {
                this.loadError = true;
                this.ref.markForCheck();
            }
        );
    }
}

@NgModule({
    imports: [
        CommonModule,
        ThirdPartyLibraryListModule
    ],
    declarations: [ThirdPartyLicenses],
    exports: [ThirdPartyLicenses]
})

export class ThirdPartyLicensesModule { }