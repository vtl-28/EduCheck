import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { Drawer } from '../../shared/components/drawer/drawer';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, MatSnackBarModule, Drawer],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private auth   = inject(AuthService);
  private router = inject(Router);
  private fb     = inject(FormBuilder);
  private http   = inject(HttpClient);
  private snack  = inject(MatSnackBar);

  readonly currentUser  = this.auth.currentUser;
  readonly userInitials = this.auth.userInitials;
  readonly displayName  = this.auth.displayName;
  readonly isStudent    = signal(false);

  drawerOpen    = signal(false);
  saving        = signal(false);
  showCurrentPw = signal(false);
  showNewPw     = signal(false);

  profileForm!: FormGroup;

  readonly provinces = [
    '', 'Eastern Cape', 'Free State', 'Gauteng', 'KwaZulu-Natal',
    'Limpopo', 'Mpumalanga', 'North West', 'Northern Cape', 'Western Cape',
  ];

  ngOnInit(): void {
    const user = this.currentUser();
    this.isStudent.set(user?.role === 'Student');

    this.profileForm = this.fb.group({
      firstName:   [user?.firstName ?? '', [Validators.required, Validators.maxLength(100)]],
      lastName:    [user?.lastName  ?? '', [Validators.required, Validators.maxLength(100)]],
      phoneNumber: [user?.phoneNumber ?? ''],
      province:    [user?.province  ?? ''],
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.saving()) return;
    this.saving.set(true);

    const raw = this.profileForm.value;
    const payload = {
      firstName:   raw.firstName.trim(),
      lastName:    raw.lastName.trim(),
      phoneNumber: raw.phoneNumber?.trim() || null,
      province:    raw.province?.trim()    || null,
      city:        raw.city?.trim()        || null,
    };

    this.http
      .put<{ success: boolean; message: string; user: unknown }>(
        `${environment.apiUrl}/Auth/profile`,
        payload
      )
      .subscribe({
        next: (_res) => {
          this.saving.set(false);
          // Update stored user with new name/phone
          const user = this.currentUser();
          if (user) {
            const updated = {
              ...user,
              firstName:   payload.firstName,
              lastName:    payload.lastName,
              fullName:    `${payload.firstName} ${payload.lastName}`,
              phoneNumber: payload.phoneNumber,
              province:    payload.province,
              city:        payload.city,
            };
            this.auth.updateCurrentUser(updated);
          }
          this.snack.open('Profile updated successfully', '', { duration: 3000 });
        },
        error: (err) => {
          this.saving.set(false);
          const msg = err?.error?.message ?? 'Could not update profile. Try again.';
          this.snack.open(msg, 'Dismiss', { duration: 4000 });
        },
      });
  }

  openFromDrawer(instituteId: string): void {
    this.drawerOpen.set(false);
    this.router.navigate(['/institutes', instituteId]);
  }

  logout(): void {
    this.auth.logout();
  }
}