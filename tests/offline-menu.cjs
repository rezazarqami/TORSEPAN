const assert = require('node:assert/strict');
const fs = require('node:fs');
const { chromium } = require(process.env.THEME_PLAYWRIGHT || 'playwright');
(async () => {
  const browser = await chromium.launch({executablePath:process.env.THEME_CHROMIUM,headless:true,args:['--no-sandbox','--no-zygote','--single-process']});
  const page = await browser.newPage();
  let allowed = false, status = 200;
  await page.route('https://menu.test/**', async route => {
    if (route.request().url().includes('/api/internal/')) return route.fulfill({status,contentType:'application/json',body:JSON.stringify({allowed})});
    return route.fulfill({contentType:'text/html',body:'<nav class="sidebar-menu"><button class="sidebar-logout">خروج</button></nav>'});
  });
  const script=fs.readFileSync('android/app/src/main/assets/online-mode-controls.js','utf8');
  async function load(native,token) {
    await page.goto('https://menu.test/');
    await page.evaluate(({native,token})=>{localStorage.clear();if(token)localStorage.setItem('access_token',token);if(native)window.torsepanOfflineDevice='phone-one';},{native,token});
    await page.addScriptTag({content:script});
    await page.waitForTimeout(70);
  }
  allowed=true;
  await load(false,'admin');assert.equal(await page.locator('#offline-mode-menu').count(),0,'ordinary browser has no native offline entry');
  status=403;await load(true,'worker');assert.equal(await page.locator('#offline-mode-menu').count(),0,'non-admin denied');
  status=200;allowed=false;await load(true,'admin-second-phone');assert.equal(await page.locator('#offline-mode-menu').count(),0,'unregistered second phone denied');
  allowed=true;await load(true,'registered-admin');assert.equal(await page.locator('#offline-mode-menu').count(),1);
  assert.equal(await page.locator('#offline-mode-menu').getAttribute('href'),'torsepan-app://offline');
  assert.equal(await page.locator('.sidebar-menu > :last-child').getAttribute('class'),'sidebar-logout');
  await page.addScriptTag({content:script});assert.equal(await page.locator('#offline-mode-menu').count(),1,'no duplicate entry');
  await page.evaluate(()=>{localStorage.removeItem('access_token');document.body.append(document.createElement('div'));});
  await page.waitForTimeout(50);assert.equal(await page.locator('#offline-mode-menu').count(),0,'logout removes entry');
  console.log('PASS native-only menu, non-admin, second phone, authorized administrator, placement, duplicates and logout');
  await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
