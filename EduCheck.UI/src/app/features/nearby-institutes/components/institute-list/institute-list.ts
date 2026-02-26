import { Component, Input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NearbyInstituteDto } from '../../../../core/models/pagination';

@Component({
  selector: 'app-institute-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './institute-list.html',
  styleUrl: './institute-list.scss'
})
export class InstituteListComponent {
  @Input() institutes: NearbyInstituteDto[] = [];
  @Input() loading = signal(false);
  
 
  selectInstitute = output<NearbyInstituteDto>();
  viewDetails = output<number>();
  
 
  getStatusClass(institute: NearbyInstituteDto): string {
    if (institute.isAccredited) return 'status-accredited';
    if (institute.providerType?.includes('Provisional')) return 'status-provisional';
    return 'status-not-accredited';
  }
  
  
  onInstituteClick(institute: NearbyInstituteDto) {
    this.selectInstitute.emit(institute);
  }

  onViewDetails(event: Event, instituteId: number) {
    event.stopPropagation();
    this.viewDetails.emit(instituteId);
  }
}