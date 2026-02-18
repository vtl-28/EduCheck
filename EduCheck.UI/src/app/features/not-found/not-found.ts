import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFound {
  constructor(private router: Router) {}

  goHome(): void {
    this.router.navigate(['/']);
  }

  goToSearch(): void {
    this.router.navigate(['/search']);
  }
}