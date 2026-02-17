import { Component, signal, inject, OnInit, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [ReactiveFormsModule, MatSnackBarModule],
  templateUrl: './report.html',
  styleUrl: './report.scss',
})
export class Report implements OnInit {
  @Input() instituteId?: string;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  form!: FormGroup;
  loading = signal(false);
  submitted = signal(false);

  ngOnInit(): void {
    this.form = this.fb.group({
      reportedInstituteName:    ['', [Validators.required, Validators.minLength(3)]],
      reportedInstituteAddress: ['', [Validators.required, Validators.minLength(5)]],
      reportedInstitutePhone:   [''],
      description:              ['', [Validators.required, Validators.minLength(20)]],
    });

    // Pre-fill name from institute detail page
    if (this.instituteId) {
      this.http
        .get<{ data: { institutionName: string; physicalAddress: string; telephone: string } }>(
          `${environment.apiUrl}/Institutes/${this.instituteId}`
        )
        .subscribe({
          next: (res) => {
            this.form.patchValue({
              reportedInstituteName:    res.data.institutionName,
              reportedInstituteAddress: res.data.physicalAddress,
              reportedInstitutePhone:   res.data.telephone,
            });
          },
          error: (_err: unknown) => {},
        });
    }

    // Pre-fill name from search query param (unknown institution)
    const nameParam = this.route.snapshot.queryParamMap.get('name');
    if (nameParam) {
      this.form.patchValue({ reportedInstituteName: nameParam });
    }
  }

  get nameField()    { return this.form.get('reportedInstituteName')!; }
  get addressField() { return this.form.get('reportedInstituteAddress')!; }
  get phoneField()   { return this.form.get('reportedInstitutePhone')!; }
  get descField()    { return this.form.get('description')!; }

  get descRemaining(): number {
    return Math.max(0, 20 - (this.descField.value?.length ?? 0));
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.form.disable();

    // Send exactly what the API expects — no extra fields
    const payload = {
      reportedInstituteName:    this.form.value.reportedInstituteName,
      reportedInstituteAddress: this.form.value.reportedInstituteAddress,
      reportedInstitutePhone:   this.form.value.reportedInstitutePhone?.trim() || null,
      description:              this.form.value.description,
    };

    this.http.post(`${environment.apiUrl}/fraud-reports`, payload).subscribe({
      next: () => {
        this.loading.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.form.enable();
        const msg = err?.error?.message ?? 'Could not submit report. Try again.';
        this.snackBar.open(msg, 'Dismiss', { duration: 4000 });
      },
    });
  }

  goBack(): void {
    if (this.instituteId) {
      this.router.navigate(['/institutes', this.instituteId]);
    } else {
      this.router.navigate(['/search']);
    }
  }

  goToSearch(): void {
    this.router.navigate(['/search']);
  }
}