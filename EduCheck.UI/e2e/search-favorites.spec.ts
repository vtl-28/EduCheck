import { test, expect } from '@playwright/test';
import { loginAsStudent } from './helpers/auth.helpers';
import { SearchPage } from './helpers/search-page.helper';
import { InstituteDetailPage } from './helpers/institute-details.helpers';
import { FavoritesPage } from './helpers/favorites.helpers';

test.describe('Search and Favorites Flows', () => {
  
  test('should remove institute from favorites', async ({ page }) => {
    await loginAsStudent(page);
    
    const searchPage = new SearchPage(page);
    const detailPage = new InstituteDetailPage(page);
    const favoritesPage = new FavoritesPage(page);
    
    // First, add an institute to favorites
    await searchPage.goto();
    await searchPage.searchForInstitute('Natal');
    await searchPage.clickFirstResult();
    
    await detailPage.waitForLoad();
    const instituteName = await detailPage.getInstituteName();
    await detailPage.addToFavorites();
    
    // Navigate to favorites page
    await favoritesPage.goto();
    
    const countBefore = await favoritesPage.getFavoriteCount();
    expect(countBefore).toBeGreaterThan(0);
    
    // Remove the favorite
    await favoritesPage.removeFavoriteByName(instituteName);
    
    // Verify it's removed
    const hasFavorite = await favoritesPage.hasFavoriteWithName(instituteName);
    expect(hasFavorite).toBe(false);
  });
  
  test('should search for an institute and view details', async ({ page }) => {
    await loginAsStudent(page);
    
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    
    // Search for an institute (use a real one from your DB)
    await searchPage.searchForInstitute('Curro Mahikeng Primary School');
    
    // Verify results are shown
    const resultCount = await searchPage.getResultCount();
    expect(resultCount).toBeGreaterThan(0);
    
    // Click first result
    await searchPage.clickFirstResult();
    
    // Verify we're on institute detail page
    const detailPage = new InstituteDetailPage(page);
    await detailPage.waitForLoad();
    
    const instituteName = await detailPage.getInstituteName();
    expect(instituteName).toBeTruthy();
    expect(instituteName.length).toBeGreaterThan(0);
  });

  test('should show empty state when no results found', async ({ page }) => {
    await loginAsStudent(page);
    
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    
    // Search for something that doesn't exist
    await searchPage.searchForInstitute('XYZ_NONEXISTENT_INSTITUTE_12345');
    
    // Verify empty state is shown
    const isEmpty = await searchPage.isEmptyStateVisible();
    expect(isEmpty).toBe(true);
  });

  test('should add institute to favorites and verify in favorites page', async ({ page }) => {
    await loginAsStudent(page);
    
    const searchPage = new SearchPage(page);
    const detailPage = new InstituteDetailPage(page);
    const favoritesPage = new FavoritesPage(page);
    
    // Search and open an institute
    await searchPage.goto();
    await searchPage.searchForInstitute('Mahikeng');
    await searchPage.clickFirstResult();
    
    // Get the institute name
    await detailPage.waitForLoad();
    const instituteName = await detailPage.getInstituteName();
    
    // Add to favorites
    await detailPage.addToFavorites();
    
    // Verify button changed to "Remove from Favorites"
    const isFavorited = await detailPage.isFavorited();
    expect(isFavorited).toBe(true);
    
    // Navigate to favorites page
    await favoritesPage.goto();
    
    // Verify the institute appears in favorites
    const hasFavorite = await favoritesPage.hasFavoriteWithName(instituteName);
    expect(hasFavorite).toBe(true);
  });

  test('should navigate from favorites to institute detail', async ({ page }) => {
    await loginAsStudent(page);
    
    const searchPage = new SearchPage(page);
    const detailPage = new InstituteDetailPage(page);
    const favoritesPage = new FavoritesPage(page);
    
    // Add a favorite first
    await searchPage.goto();
    await searchPage.searchForInstitute('Benedicts');
    await searchPage.clickFirstResult();
    
    await detailPage.waitForLoad();
    const instituteName = await detailPage.getInstituteName();
    await detailPage.addToFavorites();
    
    // Go to favorites and click the favorite
    await favoritesPage.goto();
    await favoritesPage.clickFavoriteByName(instituteName);
    
    // Verify we're back on the institute detail page
    await detailPage.waitForLoad();
    const detailName = await detailPage.getInstituteName();
    expect(detailName).toBe(instituteName);
  });
});