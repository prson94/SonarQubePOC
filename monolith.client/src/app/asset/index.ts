import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'asset-index',
  templateUrl: './index.html'
})
export class AssetIndex implements OnInit {

  constructor(private http: HttpClient) {}

  ngOnInit() {
  }

  title = 'Govern: Asset';
}
