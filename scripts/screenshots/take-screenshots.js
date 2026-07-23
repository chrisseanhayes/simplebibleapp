const { chromium } = require('playwright');

(async () => {
  console.log('Starting browser...');
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  try {
    const appUrl = 'http://localhost:5001';
    console.log(`Navigating to ${appUrl}...`);
    
    // Change this to the actual port where your local app is running if different
    await page.goto(appUrl, { waitUntil: 'networkidle' });
    
    // Optional: Set viewport size for a nicer desktop screenshot
    await page.setViewportSize({ width: 1280, height: 800 });

    const screenshotPath = 'home-page.png';
    await page.screenshot({ path: screenshotPath, fullPage: true });
    
    console.log(`✅ Screenshot successfully saved to ${screenshotPath}`);
    
    // Tip: You can add more navigation and screenshots for other pages here
    // await page.click('text=Login');
    // await page.waitForLoadState('networkidle');
    // await page.screenshot({ path: 'login-page.png', fullPage: true });

  } catch (err) {
    console.error('❌ Error taking screenshot:', err.message);
    console.log('Make sure your local application is running on http://localhost:5001 before running this script.');
  } finally {
    await browser.close();
  }
})();
