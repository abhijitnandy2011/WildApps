import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs/internal/firstValueFrom';

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
        })
      );
      console.log(result);
      localStorage.setItem('user', JSON.stringify(result));
      this.router.navigateByUrl('files');
    } catch (error: any) {
      console.log('ERROR:', error);
      localStorage.removeItem('user');
    }
  }
}
