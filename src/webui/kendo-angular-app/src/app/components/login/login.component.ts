import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs/internal/firstValueFrom';
import { LS_KEYNAME_USERINFO } from '../../settings/app.settings';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  loginObj: any = {
    username: '',
    password: '',
  };

  router = inject(Router);
  http = inject(HttpClient);

  async onClick() {
    console.log(`Login form submitted: ${this.loginObj.username},${this.loginObj.password}`);
    // if (this.loginObj.username === 'admin' && this.loginObj.password === 'test') {
    //   this.router.navigateByUrl('files');
    // } else {
    //   alert('Wrong credentials');
    // }
    try {
      var result = await firstValueFrom(
        this.http.post('https://dummyjson.com/auth/login', {
          username: 'emilys',
          password: 'emilyspass',
          expiresInMins: 1,
        })
      );
      console.log(result);
      await localStorage.setItem(LS_KEYNAME_USERINFO, JSON.stringify(result));
      this.router.navigateByUrl('files');
    } catch (error: any) {
      console.log('ERROR:onClick:', error);
      localStorage.removeItem(LS_KEYNAME_USERINFO);
    }
  }
}
