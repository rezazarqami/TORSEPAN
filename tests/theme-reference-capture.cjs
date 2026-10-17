// Screenshot of the actual app, with synthetic reference display data. Never deployed.
const {chromium}=require(process.env.THEME_PLAYWRIGHT || 'playwright');
const fs=require('node:fs');
const path=require('node:path');
(async()=>{
 const output=process.env.THEME_REFERENCE_OUTPUT || path.resolve('docs/previews');fs.mkdirSync(output,{recursive:true});
 const b=await chromium.launch({headless:true,...(process.env.THEME_CHROMIUM?{executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')}:{})});
 try {
 const context=await b.newContext({viewport:{width:390,height:670},deviceScaleFactor:3});
 const page=await context.newPage();
 const {token}=await(await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
 await page.addInitScript(token=>{localStorage.setItem('access_token',token);localStorage.setItem('refresh_token','local-fixture')},token);
 await page.goto('http://127.0.0.1:5170');await page.locator('.dashboard-tabs').waitFor();await page.evaluate(()=>document.fonts.ready);
 await page.locator('.content').evaluate(n=>{n.style.scrollBehavior='auto';n.scrollTop=0});
 await page.screenshot({path:path.join(output,'navy-dashboard-hd.png')});
 const metrics=await page.evaluate(()=>Object.fromEntries(['.topbar','.topbar .page-title-main','.topbar .user-name','.profile-icon','.dashboard-hero','.dashboard-hero h1','.dashboard-hero p','.stat-copy strong','.stat-copy small','.production-entry-card h4','.production-entry-card .form-control','.dashboard-tab strong'].map(q=>{const n=document.querySelector(q),s=getComputedStyle(n),r=n.getBoundingClientRect();return[q,{x:r.x,y:r.y,width:r.width,height:r.height,fontSize:s.fontSize,fontFamily:s.fontFamily,fontWeight:s.fontWeight}]})));
 fs.writeFileSync(path.join(output,'reference-metrics.json'),JSON.stringify(metrics,null,2));
 console.log('PASS high-resolution screenshot of actual application at 390px / 3x, using synthetic reference data');
 }finally{await b.close()}
})().catch(e=>{console.error(e);process.exit(1)});
