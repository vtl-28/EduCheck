import { Component, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  form: FormGroup;
  loading = signal(false);
  showPassword = signal(false);

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  ngOnInit(): void {
    const error = this.route.snapshot.queryParamMap.get('error');
    if (error === 'google_failed') {
      this.snackBar.open('Google sign-in failed. Please try again.', 'Dismiss', {
        duration: 4000,
        panelClass: ['snack-error'],
      });
    }
  }

  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  get emailError(): string {
    if (this.email.hasError('required')) return 'Email is required';
    if (this.email.hasError('email')) return 'Enter a valid email address';
    return '';
  }

  get passwordError(): string {
    if (this.password.hasError('required')) return 'Password is required';
    if (this.password.hasError('minlength')) return 'Password must be at least 6 characters';
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.form.disable();
    this.auth.login(this.form.value).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
  if (returnUrl) {
    this.router.navigateByUrl(returnUrl);
    return;
  }
  if (this.auth.isAdmin()) {
    this.router.navigate(['/admin/reports']);
  } else {
    this.router.navigate(['/search']);
  }
      },
      error: (err) => {
        this.loading.set(false);
        this.form.enable();
        const message = err?.error?.message || 'Invalid email or password. Please try again.';
        this.snackBar.open(message, 'Dismiss', {
          duration: 4000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  loginWithGoogle(): void {
    this.auth.loginWithGoogle();
  }

  goToRegister(): void {
    this.router.navigate(['/auth/register']);
  }

  goToLanding(): void {
    this.router.navigate(['/']);
  }
}