import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { InstituteService } from '../../core/services/institute.service';
import { FavoriteEntry, AccreditationStatus, getStatus } from '../../core/models/models';
import { Drawer } from '../../shared/components/drawer/drawer';

@Component({
  selector: 'app-favorites',
  standalone: true,
  imports: [MatSnackBarModule, Drawer],
  templateUrl: './favorites.html',
  styleUrl: './favorites.scss',
})
export class Favorites implements OnInit {
  private instituteService = inject(InstituteService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  favorites = signal<FavoriteEntry[]>([]);
  loading = signal(true);
  drawerOpen = signal(false);
  removingId = signal<number | null>(null);

  ngOnInit(): void {
    this.loadFavorites();
  }

  loadFavorites(): void {
    this.loading.set(true);
    this.instituteService.getFavorites().subscribe({
      next: (data) => {
        this.favorites.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load favorites.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  openInstitute(entry: FavoriteEntry): void {
    this.router.navigate(['/institutes', entry.institute.id]);
  }

  removeFavorite(entry: FavoriteEntry, event: Event): void {
    event.stopPropagation();
    this.removingId.set(entry.id);
    this.instituteService.removeFavorite(entry.institute.id).subscribe({
      next: () => {
        this.favorites.update((favs) => favs.filter((f) => f.id !== entry.id));
        this.removingId.set(null);
        this.snackBar.open('Removed from favorites', '', { duration: 2000 });
      },
      error: () => {
        this.removingId.set(null);
        this.snackBar.open('Could not remove. Try again.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  openFromDrawer(instituteId: string): void {
    this.drawerOpen.set(false);
    this.router.navigate(['/institutes', instituteId]);
  }

  goToSearch(): void {
    this.router.navigate(['/search']);
  }

  badgeClass(entry: FavoriteEntry): string {
    const map: Record<AccreditationStatus, string> = {
      Accredited: 'badge--green',
      Provisional: 'badge--amber',
      NotAccredited: 'badge--red',
    };
    return map[getStatus(entry.institute)];
  }

  badgeLabel(entry: FavoriteEntry): string {
    const map: Record<AccreditationStatus, string> = {
      Accredited: '✓ Accredited',
      Provisional: '⏳ Provisional',
      NotAccredited: '✕ Not Accredited',
    };
    return map[getStatus(entry.institute)];
  }
}