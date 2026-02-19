import { Page, Locator } from '@playwright/test';

/**
 * Page Object for the Institute Detail page
 */
export class InstituteDetailPage {
  readonly page: Page;
  readonly instituteName: Locator;
  readonly accreditationBadge: Locator;
  readonly addressSection: Locator;
  readonly contactSection: Locator;
  readonly favoriteButton: Locator;
  readonly backButton: Locator;
  readonly reportButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.instituteName = page.locator('.detail-hero__name');
    this.accreditationBadge = page.locator('.badge, .status-badge');
    this.addressSection = page.locator('.address-section, .detail-section:has-text("Address")');
    this.contactSection = page.locator('.contact-section, .detail-section:has-text("Contact")');
    this.favoriteButton = page.locator('button:has-text("Add to Favorites"), button:has-text("Saved")');
    this.backButton = page.locator('button:has-text("Back"), button.back-button');
    this.reportButton = page.locator('button:has-text("Report")');
  }

  async waitForLoad() {
    await this.instituteName.waitFor({ state: 'visible', timeout: 10000 });
  }

  async getInstituteName(): Promise<string> {
    return await this.instituteName.textContent() || '';
  }

  async getAccreditationStatus(): Promise<string> {
    return await this.accreditationBadge.textContent() || '';
  }

  async addToFavorites() {
    const buttonText = await this.favoriteButton.textContent();
    
    if (buttonText?.includes('Add')) {
      await this.favoriteButton.click();
      // Wait for button text to change
      await this.page.waitForTimeout(500);
    }
  }

  async removeFromFavorites() {
    const buttonText = await this.favoriteButton.textContent();
    
    if (buttonText?.includes('Saved')) {
      await this.favoriteButton.click();
      // Wait for button text to change
      await this.page.waitForTimeout(500);
    }
  }

  async isFavorited(): Promise<boolean> {
    const buttonText = await this.favoriteButton.textContent();
    return buttonText?.includes('Saved') || false;
  }

  async goBack() {
    await this.backButton.click();
    await this.page.waitForURL('/search');
  }

  async clickReportButton() {
    await this.reportButton.click();
    await this.page.waitForURL(/\/report/);
  }
}