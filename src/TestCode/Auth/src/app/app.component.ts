import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  HttpClient,
  HttpHeaders,
  HttpErrorResponse,
} from '@angular/common/http';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  username: string = '';
  password: string = '';
  response: string = '';

  constructor(private httpClient: HttpClient) {}

  onSubmitClicked() {
    this.response = 'Initiating API call...';
    const url = '';
    const headers = new HttpHeaders({
      Accept: 'application/json',
      'Content-Type': 'application/json',
    });
    const body = JSON.stringify({
      username: this.username,
      password: this.password,
      options: {
        multiOptionalFactorEnroll: false,
        warnBeforePasswordExpired: false,
      },
    });

    this.httpClient
      .post(url, body, {
        headers: headers,
      })
      .subscribe(
        (data) => {
          console.log(data);
          this.response = JSON.stringify(data, null, 2);
        },
        (err: HttpErrorResponse) => {
          this.response = JSON.stringify(err, null, 2);
        }
      );
  }
}
