/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable } from '@angular/core';
import posthog from 'posthog-js';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private initialized = false;

  constructor() {
    this.initialize();
  }

  private initialize(): void {
    if (!environment.posthog.enabled || !environment.posthog.apiKey) {
      console.warn('PostHog analytics is disabled');
      return;
    }

    try {
      posthog.init(environment.posthog.apiKey, {
        api_host: environment.posthog.apiHost,
        autocapture: environment.posthog.autocapture,
        capture_pageview: environment.posthog.capturePageViews,
        capture_pageleave: true,
        
        // Privacy settings
        mask_all_text: false,
        mask_all_element_attributes: false,
        
        // Session recording
        session_recording: {
          maskAllInputs: true,  // Mask all input fields for privacy
          maskInputOptions: {
            password: true,
            email: true
          }
        },
        
        // Performance
        loaded: () => {
          this.initialized = true;
          if (!environment.production) {
            console.log('PostHog initialized successfully');
          }
        }
      });
    } catch (error) {
      console.error('Failed to initialize PostHog:', error);
    }
  }

  /**
   * Identify user after login
   */
  identifyUser(userId: string, properties?: Record<string, any>): void {
    if (!this.initialized) return;
    
    try {
      posthog.identify(userId, properties);
    } catch (error) {
      console.error('Failed to identify user:', error);
    }
  }

  /**
   * Track custom event
   */
  trackEvent(eventName: string, properties?: Record<string, any>): void {
    if (!this.initialized) return;
    
    try {
      posthog.capture(eventName, properties);
    } catch (error) {
      console.error('Failed to track event:', error);
    }
  }

  /**
   * Track page view (usually automatic, but can be called manually)
   */
  trackPageView(pageName: string, properties?: Record<string, any>): void {
    if (!this.initialized) return;
    
    try {
      posthog.capture('$pageview', {
        $current_url: window.location.href,
        page_name: pageName,
        ...properties
      });
    } catch (error) {
      console.error('Failed to track page view:', error);
    }
  }

  /**
   * Reset analytics on logout
   */
  reset(): void {
    if (!this.initialized) return;
    
    try {
      posthog.reset();
    } catch (error) {
      console.error('Failed to reset PostHog:', error);
    }
  }

  /**
   * Check if feature flag is enabled
   */
  isFeatureEnabled(featureKey: string): boolean {
    if (!this.initialized) return false;
    
    try {
      return posthog.isFeatureEnabled(featureKey) || false;
    } catch (error) {
      console.error('Failed to check feature flag:', error);
      return false;
    }
  }

  /**
   * Get feature flag value
   */
  getFeatureFlag(featureKey: string): string | boolean | undefined {
    if (!this.initialized) return undefined;
    
    try {
      return posthog.getFeatureFlag(featureKey);
    } catch (error) {
      console.error('Failed to get feature flag:', error);
      return undefined;
    }
  }
}