import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for the Search page
 */
export class SearchPage {
  readonly page: Page;
  readonly searchInput: Locator;
  readonly resultCards: Locator;
  readonly emptyState: Locator;
  readonly skeletonCards: Locator;

  constructor(page: Page) {
    this.page = page;
    this.searchInput = page.locator('input.search-box__input');
    this.resultCards = page.locator('.institute-card');
    this.emptyState = page.locator('.empty-state');
    this.skeletonCards = page.locator('.skeleton-card');
  }

  async goto() {
    await this.page.goto('/search');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Search for an institute by typing in the search box
   * Since search uses debouncing, we wait for results to appear
   */
  async searchForInstitute(name: string) {
    await this.searchInput.fill(name);
    
    // Wait for debounce to trigger (search typically debounces ~300ms)
    await this.page.waitForTimeout(500);
    
    // Wait for skeleton to disappear and results to appear
    await this.skeletonCards.first().waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {});
    
    // Wait for either results or empty state
    await Promise.race([
      this.resultCards.first().waitFor({ state: 'visible', timeout: 5000 }),
      this.emptyState.waitFor({ state: 'visible', timeout: 15000 }),
    ]).catch(() => {
      // If neither appears, that's ok - empty state might not show
    });
  }

  /**
   * Click on the first search result
   */
  async clickFirstResult() {
    const firstCard = this.resultCards.first();
    await firstCard.waitFor({ state: 'visible' });
    await firstCard.click();
    
    // Wait for navigation to institute detail page
    await this.page.waitForURL(/\/institutes\/\d+/, { timeout: 5000 });
  }

  /**
   * Click on a specific result by institute name
   */
  async clickResultByName(name: string) {
    const result = this.page.locator('.institute-card', { hasText: name });
    await result.waitFor({ state: 'visible' });
    await result.click();
    
    // Wait for navigation to institute detail page
    await this.page.waitForURL(/\/institutes\/\d+/, { timeout: 5000 });
  }

  /**
   * Get the number of visible results
   */
  async getResultCount(): Promise<number> {
    // Wait a moment for results to settle
    await this.page.waitForTimeout(300);
    return await this.resultCards.count();
  }

  /**
   * Check if a specific institute appears in results
   */
  async hasResultWithName(name: string): Promise<boolean> {
    const result = this.page.locator('.institute-card', { hasText: name });
    return await result.isVisible();
  }

  /**
   * Add the first result to favorites
   */
  async addFirstResultToFavorites() {
    const firstCard = this.resultCards.first();
    
    // Find the favorite button within the first card
    // The button text changes from "🤍 Favorite" to "❤️ Saved"
    const favoriteBtn = firstCard.locator('button.card-action-btn').filter({ hasText: /Favorite|Saved/ });
    
    await favoriteBtn.waitFor({ state: 'visible' });
    
    // Click and stop event propagation (button has click.stop)
    await favoriteBtn.click();
    
    // Wait for the action to complete (button text should change)
    await this.page.waitForTimeout(500);
  }

  /**
   * Check if an institute is already favorited
   */
  async isFirstResultFavorited(): Promise<boolean> {
    const firstCard = this.resultCards.first();
    const savedBtn = firstCard.locator('button.card-action-btn--faved');
    return await savedBtn.isVisible();
  }

  /**
   * Get the name of the first result
   */
  async getFirstResultName(): Promise<string> {
    const firstCard = this.resultCards.first();
    const nameElement = firstCard.locator('.institute-card__name');
    return await nameElement.textContent() || '';
  }

  async isEmptyStateVisible(): Promise<boolean> {
    return await this.emptyState.isVisible();
  }
}


