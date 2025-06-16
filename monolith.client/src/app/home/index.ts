import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

interface AssetTypeClassResponseModel {
  name: string;
  description: string;
}
interface AssetTypeResponseModel {
  uid: string;
  parentUid?: string;
  name: string;
  class: AssetTypeClassResponseModel;
  description: string;
  path: string;
}

@Component({
    selector: 'home-index',
    templateUrl: './index.html',
    styleUrls: ['./index.css'],
    imports: [TranslocoPipe]
})
export class HomeIndex implements OnInit {
  public assetTypes: AssetTypeResponseModel[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getAssetTypes();
  }

  getAssetTypes() {
    //this.http.get<AssetTypeResponseModel[]>('/api/v2/assets/types').subscribe(
    //  (result) => {
    //    this.assetTypes = result;
    //  },
    //  (error) => {
    //    console.error(error);
    //  }
    //);
    console.log("API Call");
  }

  title = 'monolith.client';
}
