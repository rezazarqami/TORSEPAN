// Read-only coverage of every panel route against isolated synthetic API data.
const assert=require('node:assert/strict'),fs=require('node:fs');
const {chromium}=require(process.env.THEME_PLAYWRIGHT||'playwright');
const routes=['/accounting','/admin/production-edit','/backups','/bowls','/designs','/dimpling','/users/11111111-1111-4111-8111-000000000001/edit','/handpans','/materials','/warehouse/materials','/warehouse/materials/manage','/my-activity','/my-account','/bowls/new','/handpans/new','/materials/new','/warehouse/materials/new','/users/new','/notifications','/reports/payroll','/payroll/exclusions','/reports/payroll/payments','/reports/payroll/table','/production','/production/list','/reports','/sales','/scales','/custom-scales','/admin/shape-edit','/users','/users/list','/warehouse','/warehouse/list','/marketing','/notfound','/error','/production/process'];
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')});
 try{
  const page=await browser.newPage(),errors=[],assets=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  page.on('response',r=>{if(r.status()>=400&&/\.(css|js|woff2|webp|png|svg)(\?|$)/.test(r.url()))assets.push(r.url())});
  const {token}=await(await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
  await page.addInitScript(token=>localStorage.setItem('access_token',token),token);
  for(const width of [390,1280]){
   await page.setViewportSize({width,height:844});
   for(const route of routes){
    const start=errors.length;
    await page.goto('http://127.0.0.1:5170'+route);
    await page.locator('.theme-page-intro').first().waitFor({timeout:12000});
    await page.evaluate(()=>document.fonts.ready);await page.waitForTimeout(180);
    assert.equal(await page.locator('link[href*="theme-pages"]').count(),1,'shared page stylesheet must be loaded');
    assert.equal(await page.locator('.theme-page-intro h1').first().isVisible(),true,route+' heading');
    assert.deepEqual(errors.slice(start),[],route+' runtime error');
    const overflow=await page.locator('.content').evaluate(n=>{
     const bounds=n.getBoundingClientRect();return [...n.children].filter(c=>{
      const r=c.getBoundingClientRect();return r.width&&getComputedStyle(c).position!=='fixed'&&(r.left<bounds.left-1||r.right>bounds.right+1);
     }).map(c=>({class:c.className,rect:c.getBoundingClientRect().toJSON()}));
    });
    assert.deepEqual(overflow,[],route+' page-level overflow at '+width);
    if(width===390){
     assert.equal((await page.locator('.mobile-nav-rail').boundingBox()).width,25);
     const output=process.env.THEME_SCREENSHOTS;if(output){fs.mkdirSync(output,{recursive:true});await page.screenshot({path:output+'/'+route.slice(1).replaceAll('/','-')+'.png'});}
    }
    console.log('PASS '+width+'px '+route);
   }
  }
  // Exercise representative navigation and editors without submitting business data.
  await page.setViewportSize({width:390,height:844});
  await page.goto('http://127.0.0.1:5170/scales');await page.getByRole('button',{name:'ثبت اسکیل جدید',exact:true}).click();await page.locator('#new-scale-name').fill('Preview scale');
  await page.goto('http://127.0.0.1:5170/notifications');await page.getByRole('button',{name:'صدا و اعلان',exact:true}).click();await page.getByRole('button',{name:'اطلاعیه‌های کارگاه',exact:true}).click();
  assert.deepEqual(errors,[],'browser errors');assert.deepEqual(assets,[],'missing assets');
  // Standalone login retains its own layout and uses the shared theme stylesheet.
  await page.evaluate(()=>localStorage.clear());await page.goto('http://127.0.0.1:5170/login');await page.locator('.login-card-modern').waitFor();assert.equal(await page.locator('input').count()>0,true);
  console.log('PASS all '+routes.length+' panel routes at 2 widths, representative tabs and standalone login; isolated data only');
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
