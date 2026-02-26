import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-share-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './share-modal.html',
  styleUrl: './share-modal.scss'
})
export class ShareModalComponent {
  
  title = input<string>('Share');
  url = input.required<string>();
  shareText = input<string>('');
  
  // eslint-disable-next-line @angular-eslint/no-output-native
  close = output<void>();
  
  async copyLink() {
    try {
      await navigator.clipboard.writeText(this.url());
    
      this.close.emit();
      
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  }
  
  shareOnWhatsApp() {
    const message = this.shareText() 
      ? `${this.shareText()}\n\n🔗 ${this.url()}`
      : this.url();
    
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
    window.open(whatsappUrl, '_blank');
    
    this.close.emit();
  }
  
  onOverlayClick() {
    this.close.emit();
  }
  
  onModalClick(event: Event) {
    event.stopPropagation();
  }
}