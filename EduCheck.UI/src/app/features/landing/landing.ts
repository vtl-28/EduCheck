import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRippleModule } from '@angular/material/core';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatIconModule, MatRippleModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing {
  searchQuery = '';

  constructor(private router: Router) {}

  onSearch(): void {
    if (this.searchQuery.trim()) {
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: '/search', q: this.searchQuery.trim() },
      });
    } else {
      this.router.navigate(['/auth/login']);
    }
  }

  goToLogin(): void {
    this.router.navigate(['/auth/login']);
  }

  goToRegister(): void {
    this.router.navigate(['/auth/register']);
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }

  readonly howItWorks = [
    {
      step: 1,
      title: 'Search an Institution',
      description:
        'Enter the name of any private learning institute to instantly check its accreditation and registration status.',
      icon: '🔍',
    },
    {
      step: 2,
      title: 'View Verified Details',
      description:
        'See clear indicators of whether the institution is registered, accredited, pending review, or operating illegally.',
      icon: '✅',
    },
    {
      step: 3,
      title: 'Report if Suspicious',
      description:
        'Suspect a fake or unregistered institution? Submit a report directly to the Department of Higher Education.',
      icon: '🚩',
    },
  ];

  readonly features = [
    {
      icon: '⚡',
      title: 'Real-Time Verification',
      description: 'Instantly check legitimacy using up-to-date DHET data.',
    },
    {
      icon: '🏛️',
      title: 'Accredited Database',
      description: '4,000+ institutions recognised by DHET.',
    },
    {
      icon: '🚩',
      title: 'Smart Reporting',
      description: 'Flag illegal operators through a streamlined form.',
    },
    {
      icon: '📱',
      title: 'Mobile-First Design',
      description: 'Responsive and user-friendly on any device.',
    },
    {
      icon: '💡',
      title: 'Awareness Tips',
      description: 'Learn how to spot red flags and diploma scams.',
    },
    {
      icon: '🔒',
      title: 'Anonymous Reporting',
      description: 'Report institutions without revealing your identity.',
    },
  ];

  readonly currentYear = new Date().getFullYear();

  readonly personas = [
    {
      icon: '🎓',
      title: 'Students',
      description: 'Make informed decisions about your education before committing time and money.',
    },
    {
      icon: '👨‍👩‍👧',
      title: 'Parents',
      description: "Protect your child's future by verifying institutions before enrollment.",
    },
    {
      icon: '🏛️',
      title: 'Government Officials',
      description: 'Leverage public reports to monitor and act against illegal operations.',
    },
  ];
}