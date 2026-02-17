import { Component, signal, inject, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { InstituteService } from '../../core/services/institute.service';
import { Institute, AccreditationStatus, getStatus } from '../../core/models/models';

@Component({
  selector: 'app-institute-detail',
  standalone: true,
  imports: [MatSnackBarModule],
  templateUrl: './institute-detail.html',
  styleUrl: './institute-detail.scss',
})
export class InstituteDetail implements OnInit {
  @Input() id!: string;

  private instituteService = inject(InstituteService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  institute = signal<Institute | null>(null);
  loading = signal(true);
  isFavorite = signal(false);
  togglingFavorite = signal(false);

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