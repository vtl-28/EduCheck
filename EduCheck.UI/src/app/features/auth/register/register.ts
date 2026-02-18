import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
  ReactiveFormsModule,
} from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

// Custom validator — checks both password fields match
function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, MatSnackBarModule, MatProgressSpinnerModule],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  form: FormGroup;
  loading = signal(false);
  showPassword = signal(false);
  showConfirm = signal(false);

  readonly provinces = [
    'Eastern Cape',
    'Free State',
    'Gauteng',
    'KwaZulu-Natal',
    'Limpopo',
    'Mpumalanga',
    'North West',
    'Northern Cape',
    'Western Cape',
  ];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.form = this.fb.group(
      {
        firstName:       ['', [Validators.required, Validators.minLength(2)]],
        lastName:        ['', [Validators.required, Validators.minLength(2)]],
        email:           ['', [Validators.required, Validators.email]],
        password:        ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
        province:        [''],
        phoneNumber:     [''],
      },
      { validators: passwordMatchValidator }
    );
  }

  // ── Field getters ──────────────────────────────────────────
  get firstName()       { return this.form.get('firstName')!; }
  get lastName()        { return this.form.get('lastName')!; }
  get email()           { return this.form.get('email')!; }
  get password()        { return this.form.get('password')!; }
  get confirmPassword() { return this.form.get('confirmPassword')!; }

  // ── Error messages ─────────────────────────────────────────
  get firstNameError(): string {
    if (this.firstName.hasError('required')) return 'First name is required';
    if (this.firstName.hasError('minlength')) return 'Must be at least 2 characters';
    return '';
  }

  get lastNameError(): string {
    if (this.lastName.hasError('required')) return 'Last name is required';
    if (this.lastName.hasError('minlength')) return 'Must be at least 2 characters';
    return '';
  }

  get emailError(): string {
    if (this.email.hasError('required')) return 'Email is required';
    if (this.email.hasError('email')) return 'Enter a valid email address';
    return '';
  }

  get passwordError(): string {
    if (this.password.hasError('required')) return 'Password is required';
    if (this.password.hasError('minlength')) return 'Password must be at least 8 characters';
    return '';
  }

  get confirmPasswordError(): string {
    if (this.confirmPassword.hasError('required')) return 'Please confirm your password';
    if (this.form.hasError('passwordMismatch') && this.confirmPassword.touched)
      return 'Passwords do not match';
    return '';
  }

  get confirmHasError(): boolean {
    return (
      this.confirmPassword.touched &&
      (this.confirmPassword.hasError('required') ||
        this.form.hasError('passwordMismatch'))
    );
  }

  // ── Submit ─────────────────────────────────────────────────
  onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    this.form.disable();

    // //const { confirmPassword, ...payload } = this.form.value;
    // console.log('Form value on submit:', this.form.value);
    // Convert empty strings to null so API validation passes
  const raw = this.form.value;
  const payload = {
    ...raw,
    phoneNumber: raw.phoneNumber?.trim() || null,
    province:    raw.province?.trim()    || null,
    city:        raw.city?.trim()        || null,
  };

    this.auth.register(payload).subscribe({
      next: () => {
        this.router.navigate(['/search']);
      },
      error: (err) => {
        this.loading.set(false);
        this.form.enable();
        const message =
          err?.error?.message || 'Registration failed. Please try again.';
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

  goToLogin(): void {
    this.router.navigate(['/auth/login']);
  }

  goToLanding(): void {
    this.router.navigate(['/']);
  }
}