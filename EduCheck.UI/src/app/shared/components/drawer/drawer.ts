import {
  Component,
  Output,
  EventEmitter,
  signal,
  computed,
  OnInit,
  inject,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { InstituteService } from '../../../core/services/institute.service';
import { SearchHistoryEntry, AccreditationStatus, getStatus } from '../../../core/models/models';

interface HistoryGroup {
  label: string;
  entries: SearchHistoryEntry[];
}

@Component({
  selector: 'app-drawer',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './drawer.html',
  styleUrl: './drawer.scss',
})
export class Drawer implements OnInit {
  @Output() closed = new EventEmitter<void>();
  @Output() instituteSelected = new EventEmitter<string>();

  private auth = inject(AuthService);
  private instituteService = inject(InstituteService);
  private router = inject(Router);

  readonly currentUser = this.auth.currentUser;
  readonly userInitials = this.auth.userInitials;
  readonly displayName = this.auth.displayName;
  readonly isAdmin = this.auth.isAdmin;

  userMenuOpen = signal(false);
  activeCtxId = signal<number | null>(null);
  historyFilter = signal('');
  historyEntries = signal<SearchHistoryEntry[]>([]);
  loading = signal(false);

  readonly groupedHistory = computed<HistoryGroup[]>(() => {
    const filter = this.historyFilter().toLowerCase().trim();
    const entries = this.historyEntries();
    const filtered = filter
      ? entries.filter((e) => e.institute.institutionName.toLowerCase().includes(filter))
      : entries;
    return this.groupByDate(filtered);
  });

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading.set(true);
    this.instituteService.getSearchHistory().subscribe({
      next: (entries) => {
        this.historyEntries.set(entries);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onFilterChange(value: string): void {
    this.historyFilter.set(value);
  }

  onNewSearch(): void {
    this.router.navigate(['/search']);
    this.closed.emit();
  }

  onInstituteClick(entry: SearchHistoryEntry): void {
    this.activeCtxId.set(null);
    this.instituteSelected.emit(entry.institute.id.toString());
    this.closed.emit();
  }

  toggleCtxMenu(id: number, event: Event): void {
    event.stopPropagation();
    this.activeCtxId.set(this.activeCtxId() === id ? null : id);
  }

  closeCtxMenu(): void {
    this.activeCtxId.set(null);
  }

  addToFavorites(entry: SearchHistoryEntry, event: Event): void {
    event.stopPropagation();
    this.activeCtxId.set(null);
    this.instituteService.addFavorite(entry.institute.id).subscribe();
  }

  deleteEntry(entry: SearchHistoryEntry, event: Event): void {
    event.stopPropagation();
    this.activeCtxId.set(null);
    this.instituteService.deleteHistoryEntry(entry.id).subscribe(() => {
      this.historyEntries.update((entries) => entries.filter((e) => e.id !== entry.id));
    });
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.userMenuOpen.update((v) => !v);
  }

  goToProfile(): void {
    this.userMenuOpen.set(false);
    this.router.navigate(['/profile']);
    this.closed.emit();
  }

  goToFavorites(): void {
    this.userMenuOpen.set(false);
    this.router.navigate(['/favorites']);
    this.closed.emit();
  }

  goToNearby(): void {
    this.router.navigate(['/nearby']);
    this.closed.emit();
  }

  logout(): void {
    this.userMenuOpen.set(false);
    this.auth.logout();
    this.closed.emit();
  }

  close(): void {
    this.activeCtxId.set(null);
    this.userMenuOpen.set(false);
    this.closed.emit();
  }

  statusDotClass(entry: SearchHistoryEntry): string {
    const status = getStatus(entry.institute);
    const map: Record<AccreditationStatus, string> = {
      Accredited: 'dot--green',
      Provisional: 'dot--amber',
      NotAccredited: 'dot--red',
    };
    return map[status];
  }

  private groupByDate(entries: SearchHistoryEntry[]): HistoryGroup[] {
    const now = new Date();
    const today = this.startOfDay(now);
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    const lastWeekStart = new Date(today);
    lastWeekStart.setDate(lastWeekStart.getDate() - 7);
    const lastMonthStart = new Date(today);
    lastMonthStart.setDate(lastMonthStart.getDate() - 30);

    const groups: Record<string, SearchHistoryEntry[]> = {
      Today: [], Yesterday: [], 'This Week': [], 'This Month': [], Older: [],
    };

    for (const entry of entries) {
      const date = this.startOfDay(new Date(entry.searchedAt));
      if (date >= today) groups['Today'].push(entry);
      else if (date >= yesterday) groups['Yesterday'].push(entry);
      else if (date >= lastWeekStart) groups['This Week'].push(entry);
      else if (date >= lastMonthStart) groups['This Month'].push(entry);
      else groups['Older'].push(entry);
    }

    return Object.entries(groups)
      .filter(([, items]) => items.length > 0)
      .map(([label, entries]) => ({ label, entries }));
  }

  private startOfDay(date: Date): Date {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    return d;
  }
}