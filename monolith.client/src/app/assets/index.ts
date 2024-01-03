import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'assets-index',
  templateUrl: './index.html'
})
export class AssetsIndex implements OnInit {

  constructor(private http: HttpClient) {}

  ngOnInit() {
  }

  title = 'Govern: Assets';
}
