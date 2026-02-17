import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import {
  ApiResponse,
  FraudReport,
  FraudReportsResponse,
  FraudReportStatistics,
  ReportStatus,
} from '../../../core/models/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [MatSnackBarModule],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
})
export class Reports implements OnInit {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  reports = signal<FraudReport[]>([]);
  stats = signal<FraudReportStatistics | null>(null);
  loading = signal(true);
  loadingStats = signal(true);
  activeFilter = signal<ReportStatus | 'All'>('All');
  expandedId = signal<string | null>(null);

  readonly filters: (ReportStatus | 'All')[] = [
    'All', 'Submitted', 'UnderReview', 'Verified', 'Dismissed', 'ActionTaken',
  ];

  readonly filteredReports = computed(() => {
    const filter = this.activeFilter();
    return filter === 'All'
      ? this.reports()
      : this.reports().filter((r) => r.status === filter);
  });

  ngOnInit(): void {
    this.loadReports();
    this.loadStats();
  }

  loadReports(): void {
    this.loading.set(true);
    this.http
      .get<ApiResponse<FraudReportsResponse>>(
        `${environment.apiUrl}/admin/fraud-reports`
      )
      .subscribe({
        next: (res) => {
          this.reports.set(res.data.reports);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.snackBar.open('Could not load reports.', 'Dismiss', { duration: 3000 });
        },
      });
  }

  loadStats(): void {
    this.loadingStats.set(true);
    this.http
      .get<ApiResponse<FraudReportStatistics>>(
        `${environment.apiUrl}/admin/fraud-reports/statistics`
      )
      .subscribe({
        next: (res) => {
          this.stats.set(res.data);
          this.loadingStats.set(false);
        },
        error: () => this.loadingStats.set(false),
      });
  }

  toggleExpanded(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  logout(): void {
    this.auth.logout();
  }

  statusBadgeClass(status: ReportStatus): string {
    const map: Record<ReportStatus, string> = {
      Submitted:    'badge--amber',
      UnderReview:  'badge--blue',
      Verified:     'badge--green',
      Dismissed:    'badge--grey',
      ActionTaken:  'badge--purple',
    };
    return map[status] ?? 'badge--grey';
  }

  statusLabel(status: ReportStatus): string {
    const map: Record<ReportStatus, string> = {
      Submitted:   '🔴 Submitted',
      UnderReview: '🔵 Under Review',
      Verified:    '🟢 Verified',
      Dismissed:   '⚫ Dismissed',
      ActionTaken: '🟣 Action Taken',
    };
    return map[status] ?? status;
  }

  filterLabel(filter: ReportStatus | 'All'): string {
    if (filter === 'All') return 'All';
    return this.statusLabel(filter);
  }

  severityClass(severity: string): string {
    const map: Record<string, string> = {
      Low:      'severity--low',
      Medium:   'severity--medium',
      High:     'severity--high',
      Critical: 'severity--critical',
    };
    return map[severity] ?? 'severity--medium';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-ZA', {
      day: '2-digit', month: 'short', year: 'numeric',
    });
  }
}