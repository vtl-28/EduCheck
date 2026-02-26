import { Injectable, signal } from '@angular/core';

export interface Toast {
  message: string;
  type: 'success' | 'error' | 'info';
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toast = signal<Toast | null>(null);

  show(message: string, type: 'success' | 'error' | 'info' = 'success', duration = 3000) {
    this.toast.set({ message, type, duration });
    
    setTimeout(() => {
      this.toast.set(null);
    }, duration);
  }

  success(message: string) {
    this.show(message, 'success');
  }

  error(message: string) {
    this.show(message, 'error');
  }

  info(message: string) {
    this.show(message, 'info');
  }
}