import { Component, signal, inject, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { InstituteService } from '../../core/services/institute.service';
import { Institute, AccreditationStatus, getStatus } from '../../core/models/models';
import { ShareModalComponent } from '../../shared/components/share-modal/share-modal';
import { ToastService } from '../../shared/services/toast';

@Component({
  selector: 'app-institute-detail',
  standalone: true,
  imports: [MatSnackBarModule, ShareModalComponent],
  templateUrl: './institute-detail.html',
  styleUrl: './institute-detail.scss',
})
export class InstituteDetail implements OnInit {
  @Input() id!: string;

  private instituteService = inject(InstituteService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private toastService = inject(ToastService);

  institute = signal<Institute | null>(null);
  loading = signal(true);
  isFavorite = signal(false);
  togglingFavorite = signal(false);
  showShareModal = signal(false);

  ngOnInit(): void {
    this.loadInstitute();
  }

  loadInstitute(): void {
    this.loading.set(true);
    this.instituteService.getById(this.id).subscribe({
      next: (data) => {
        this.institute.set(data);
        this.loading.set(false);
        this.checkFavorite();
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load institution details.', 'Dismiss', { duration: 3000 });
        this.router.navigate(['/search']);
      },
    });
  }

  checkFavorite(): void {
    const inst = this.institute();
    if (!inst) return;
    this.instituteService.checkIsFavorite(inst.id).subscribe({
      next: (res) => this.isFavorite.set(res.isFavorited),
      error: (_err: unknown) => {},
    });
  }

  toggleFavorite(): void {
    const inst = this.institute();
    if (!inst || this.togglingFavorite()) return;
    this.togglingFavorite.set(true);

    if (this.isFavorite()) {
      this.instituteService.removeFavorite(inst.id).subscribe({
        next: () => {
          this.isFavorite.set(false);
          this.togglingFavorite.set(false);
          this.snackBar.open('Removed from favorites', '', { duration: 2000 });
        },
        error: () => this.togglingFavorite.set(false),
      });
    } else {
      this.instituteService.addFavorite(inst.id).subscribe({
        next: () => {
          this.isFavorite.set(true);
          this.togglingFavorite.set(false);
          this.snackBar.open('Added to favorites', '', { duration: 2000 });
        },
        error: () => this.togglingFavorite.set(false),
      });
    }
  }

  reportInstitute(): void {
    this.router.navigate(['/report', this.id]);
  }

  goBack(): void {
    this.router.navigate(['/search']);
  }

  openShareModal() {
    this.showShareModal.set(true);
  }
  
  closeShareModal() {
    this.showShareModal.set(false);
    this.toastService.success('Link copied to clipboard!');
  }

  get shareUrl(): string {
    const baseUrl = window.location.origin;
    const currentPath = this.router.url;
    return `${baseUrl}${currentPath}`;
  }

  getShareText(): string {
    const institute = this.institute();
    if (!institute) return '';
    
    const status = this.getStatusLabel(institute.providerType);
    
    return `Check out this institution on EduCheck:

📚 ${institute.institutionName}
✅ Status: ${status}`;
  }

  getStatusLabel(providerType: string): string {
    switch(providerType) {
      case 'Accredited': return 'Accredited ✅';
      case 'Provisional': return 'Provisionally Accredited ⚠️';
      case 'Not Accredited': return 'Not Accredited ❌';
      default: return providerType;
    }
  }

  get statusBannerClass(): string {
    const inst = this.institute();
    if (!inst) return '';
    const map: Record<AccreditationStatus, string> = {
      Accredited: 'banner--green',
      Provisional: 'banner--amber',
      NotAccredited: 'banner--red',
    };
    return map[getStatus(inst)];
  }

  get statusLabel(): string {
    const inst = this.institute();
    if (!inst) return '';
    const map: Record<AccreditationStatus, string> = {
      Accredited: '✓ Accredited',
      Provisional: '⏳ Provisional Accreditation',
      NotAccredited: '✕ Not Accredited',
    };
    return map[getStatus(inst)];
  }

  get showWarning(): boolean {
    const inst = this.institute();
    return inst ? !inst.isAccredited : false;
  }

  get favoriteLabel(): string {
    if (this.togglingFavorite()) return '...';
    return this.isFavorite() ? '❤️ Saved' : '🤍 Add to Favorites';
  }
}