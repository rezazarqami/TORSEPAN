// Exercises the actual interactive Blazor app against theme-preview-fixture.cjs.
const assert=require('node:assert/strict');
const fs=require('node:fs');
const {chromium}=require(process.env.THEME_PLAYWRIGHT || 'playwright');
const origin=process.env.THEME_ORIGIN || 'http://127.0.0.1:5170';
const output=process.env.THEME_SCREENSHOTS;
(async()=>{
 const browser=await chromium.launch({headless:true,...(process.env.THEME_CHROMIUM?{executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')}:{})});
 try {
  const page=await browser.newPage();const errors=[],failedAssets=[];page.on('pageerror',e=>errors.push(e.message));
  page.on('response',r=>{if(r.url().startsWith(origin)&&r.status()>=400&&/\.(css|js|woff2|svg|webp|png)(\?|$)/.test(r.url()))failedAssets.push(`${r.status()} ${r.url()}`)});
  const {token}=await (await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
  await page.addInitScript(token=>{localStorage.setItem('access_token',token);localStorage.setItem('refresh_token','local-fixture')},token);
  for(const width of [360,390,768,1280]) {
   await page.setViewportSize({width,height:844});
   for(const [route,name] of [['/','dashboard'],['/orders','orders'],['/warehouse/export','export']]) {
    await page.goto(origin+route);await page.locator('.theme-hero').waitFor();await page.locator('.spinner-border').waitFor({state:'hidden'});await page.evaluate(()=>document.fonts.ready);
    await page.waitForTimeout(200);
    assert.equal(await page.evaluate(()=>document.body.scrollTop),0,'document scrolls outside content');
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`${name}: document overflow at ${width}`);
    const overflow=await page.locator('.topbar-left,.topbar-right,.theme-hero,.order-tabs,.warehouse-dashboard,.production-entry-card').evaluateAll(nodes=>nodes.filter(n=>{const r=n.getBoundingClientRect();const rail=document.querySelector('.mobile-nav-rail').getBoundingClientRect();return r.left< -1||r.right>(rail.width?rail.left:innerWidth)+1||n.scrollWidth>n.clientWidth+2}).map(n=>({class:n.className,width:n.clientWidth,scroll:n.scrollWidth,rect:n.getBoundingClientRect().toJSON()})));
    assert.deepEqual(overflow,[],`${name}: controls overflow at ${width}`);
    assert.equal(await page.locator('.topbar .header-notification a[href="/notifications"]').count(),1);
    assert.equal(await page.locator('.topbar').getByText('آنلاین',{exact:true}).count(),0);
    if(width<=768) {
     const rail=page.locator('.mobile-nav-rail');const before=await rail.boundingBox();assert.equal(before.width,25);assert.equal(before.y,0);assert.equal(before.height,844);
     await page.locator('.content').evaluate(n=>{n.style.scrollBehavior='auto';n.scrollTop=n.scrollHeight});assert.deepEqual(await rail.boundingBox(),before,'rail moves during scroll');
     await rail.click();await page.locator('.sidebar.mobile-open').waitFor();assert.equal(await rail.getAttribute('aria-expanded'),'true');
     assert.equal(await page.locator('.sidebar-install').isVisible(),true);
     await rail.click();await page.locator('.sidebar.mobile-open').waitFor({state:'hidden'});
     await page.locator('.content').evaluate(n=>n.scrollTop=0);
    }
    if(name==='dashboard') {
     const tabs=page.locator('.dashboard-tabs button');await tabs.nth(1).click();await page.locator('.calendar-day').first().waitFor();await page.locator('.calendar-day').nth(4).click();
     await tabs.nth(2).click();await page.getByText('نمای ماهانه',{exact:true}).waitFor();await tabs.first().click();await page.locator('.queue-table').waitFor();
    }
    if(name==='orders') {
     await page.locator('.order-list-toggle').first().click();await page.locator('.order-details').waitFor();await page.locator('.order-list-toggle').first().click();
     await page.locator('.order-tabs button').nth(1).click();await page.locator('#order-customer').fill('آزمایش فرم');
     await page.locator('.add-line').click();await page.locator('.composition-line').nth(1).waitFor();assert.equal(await page.locator('.composition-line').count(),2);
     await page.locator('.remove-line').last().click();await page.locator('.composition-line').nth(1).waitFor({state:'hidden'});assert.equal(await page.locator('.composition-line').count(),1);
     await page.locator('.order-tabs button').nth(2).click();await page.locator('.empty-orders').waitFor();
     await page.locator('.order-tabs button').first().click();await page.locator('.order-list-toggle').first().waitFor();
    }
    if(name==='export') {
     const warehouses=page.locator('.warehouse-card');await warehouses.nth(1).click();await page.getByText('این انبار خالی است',{exact:true}).waitFor();
     await warehouses.first().click();await page.locator('.inventory-item').waitFor();
     await page.locator('.export-tools input.form-control').fill('no-match');await page.getByText('این انبار خالی است',{exact:true}).waitFor();
     await page.locator('.export-tools input.form-control').fill('');await page.locator('.inventory-item').waitFor();
     await page.locator('.item-select input').check();await page.locator('.ship-selected').click();await page.locator('.shipment-dialog').waitFor();
     await page.getByRole('button',{name:'انصراف',exact:true}).click();await page.locator('.shipment-dialog').waitFor({state:'hidden'});
    }
    await page.locator('.content').evaluate(n=>n.scrollTop=0);
    if(output&&width===390){await page.locator('.content').evaluate(n=>{n.style.scrollBehavior='auto';n.scrollTop=0});fs.mkdirSync(output,{recursive:true});await page.screenshot({path:`${output}/${name}.png`});}
    console.log(`PASS ${width}px ${name}: layout, menu and interactions`);
   }
  }
  assert.deepEqual(errors,[],'browser runtime errors');
  assert.deepEqual(failedAssets,[],'missing application assets');
  console.log('PASS no browser runtime errors; no production data or accounts used');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
