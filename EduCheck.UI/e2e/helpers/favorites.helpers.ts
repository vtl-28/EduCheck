import { Page, Locator } from '@playwright/test';

/**
 * Page Object for the Favorites page
 */
export class FavoritesPage {
  readonly page: Page;
  readonly favoriteCards: Locator;
  readonly emptyState: Locator;
  readonly removeButtons: Locator;
  readonly menuButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.favoriteCards = page.locator('.favorite-card, .institute-card');
    this.emptyState = page.locator('.empty-state, text=/No favorites/i');
    this.removeButtons = page.locator('button:has-text("Remove")');
    this.menuButton = page.locator('button.app-bar__menu-btn');
  }

  async goto() {
    await this.page.goto('/favorites');
    await this.page.waitForLoadState('networkidle');
  }

  async getFavoriteCount(): Promise<number> {
    // Wait a moment for favorites to load
    await this.page.waitForTimeout(1000);
    
    // if (await this.emptyState.isVisible()) {
    //   return 0;
    // }
    
    return await this.favoriteCards.count();
  }

  async hasFavoriteWithName(name: string): Promise<boolean> {
    const favorite = this.page.locator(`.favorite-card:has-text("${name}"), .institute-card:has-text("${name}")`);
    return await favorite.isVisible();
  }

  async removeFavoriteByName(name: string) {
        // Get the initial count before removing
    const initialCount = await this.getFavoriteCount();
    
    // Find the favorite card
    const favoriteCard = this.page.locator('.favorite-card, .institute-card', { hasText: name });
    await favoriteCard.waitFor({ state: 'visible', timeout: 5000 });
    
    // Find and click the remove button within that card
    const removeButton = favoriteCard.locator('button:has-text("Remove")');
    await removeButton.click();
    
    // Wait for the card to be removed from the DOM (not just hidden)
    await favoriteCard.waitFor({ state: 'detached', timeout: 10000 });
    
    // Alternative: Wait for the count to decrease
    await this.page.waitForFunction(
      (expectedCount) => {
        const cards = document.querySelectorAll('.favorite-card, .institute-card');
        return cards.length < expectedCount;
      },
      initialCount,
      { timeout: 10000 }
    );
  }

  async clickFavoriteByName(name: string) {
    const favoriteCard = this.page.locator(`.favorite-card:has-text("${name}"), .institute-card:has-text("${name}")`);
    await favoriteCard.click();
    await this.page.waitForURL(/\/institutes\/\d+/);
  }

  async isEmptyStateVisible(): Promise<boolean> {
    return await this.emptyState.isVisible();
  }

  async openDrawer() {
    await this.menuButton.click();
    await this.page.waitForSelector('.drawer-container--open', { state: 'visible' });
  }
}