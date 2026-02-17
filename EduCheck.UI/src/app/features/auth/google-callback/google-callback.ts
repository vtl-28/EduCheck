import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [],
  template: `
    <div class="oauth-loading">
      <div class="oauth-loading__spinner"></div>
      <p>Signing you in...</p>
    </div>
  `,
  styles: [`
    .oauth-loading {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: linear-gradient(160deg, #0B2545 0%, #1A3A6B 100%);
      color: white;
      gap: 16px;
      font-family: 'DM Sans', sans-serif;
    }
    .oauth-loading__spinner {
      width: 40px; height: 40px;
      border: 3px solid rgba(255,255,255,0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class GoogleCallback implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const accessToken  = params.get('accessToken');
    const refreshToken = params.get('refreshToken');
    const error        = params.get('error');

    if (error || !accessToken || !refreshToken) {
      this.router.navigate(['/auth/login'], {
        queryParams: { error: 'google_failed' }
      });
      return;
    }

    // Build user object from query params
    const user = {
      id:        params.get('userId') ?? '',
      email:     params.get('email') ?? '',
      firstName: params.get('firstName') ?? '',
      lastName:  params.get('lastName') ?? '',
      fullName:  `${params.get('firstName')} ${params.get('lastName')}`.trim(),
      role:      (params.get('role') ?? 'Student') as 'Student' | 'Admin',
    };

    // Store tokens and user via auth service
    this.auth.handleOAuthSuccess(accessToken, refreshToken, user);
    if (user.role === 'Admin') {
      this.router.navigate(['/admin/reports']);
    } else {
      this.router.navigate(['/search']);
    }
  }
}