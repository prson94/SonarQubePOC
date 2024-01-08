import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'login-index',
  templateUrl: './index.html'
})
export class LoginIndex implements OnInit {

  constructor(private http: HttpClient) {}

  ngOnInit() {
  }

  title = 'Govern: Login';
}
