import { Component, Input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export type RadiusOption = 5 | 10 | 25 | 50 | 100;

@Component({
  selector: 'app-radius-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="radius-selector">
      <label class="radius-selector__label">Search Radius:</label>
      <div class="radius-selector__options">
        @for (option of radiusOptions; track option) {
          <button
            class="radius-btn"
            [class.radius-btn--active]="selectedRadius() === option"
            (click)="selectRadius(option)"
            [disabled]="disabled()"
          >
            {{ option }} km
          </button>
        }
      </div>
      
      @if (resultsCount() !== null) {
        <p class="radius-selector__results">
          {{ resultsCount() }} {{ resultsCount() === 1 ? 'institute' : 'institutes' }} 
          within {{ selectedRadius() }}km
        </p>
      }
    </div>
  `,
  styles: [`
    .radius-selector {
      background: white;
      padding: 16px;
      border-radius: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .radius-selector__label {
      display: block;
      font-size: 14px;
      font-weight: 600;
      color: #4B5563;
      margin-bottom: 12px;
    }

    .radius-selector__options {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .radius-btn {
      flex: 1;
      min-width: 70px;
      padding: 10px 16px;
      border: 2px solid #E5E7EB;
      background: white;
      color: #6B7280;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        border-color: #3B82F6;
        color: #3B82F6;
        background: #EFF6FF;
      }

      &--active {
        border-color: #3B82F6;
        background: #3B82F6;
        color: white;

        &:hover {
          background: #2563EB;
          border-color: #2563EB;
        }
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .radius-selector__results {
      margin: 12px 0 0 0;
      font-size: 13px;
      color: #059669;
      font-weight: 500;
    }

    @media (max-width: 640px) {
      .radius-btn {
        flex: 1 1 calc(50% - 4px);
      }
    }
  `]
})
export class RadiusSelectorComponent {
  @Input() disabled = signal(false);
  @Input() resultsCount = signal<number | null>(null);
  
  selectedRadius = signal<RadiusOption>(10); // Default 10km
  radiusChange = output<RadiusOption>();
  
  radiusOptions: RadiusOption[] = [5, 10, 25, 50, 100];

  selectRadius(radius: RadiusOption) {
    this.selectedRadius.set(radius);
    this.radiusChange.emit(radius);
  }
}