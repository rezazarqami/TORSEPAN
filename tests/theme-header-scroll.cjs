const assert=require('node:assert/strict'),fs=require('node:fs');
const {chromium}=require(process.env.THEME_PLAYWRIGHT||'playwright');
(async()=>{
 const b=await chromium.launch({headless:true,executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')});
 try{
  const p=await b.newPage();const {token}=await(await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
  await p.addInitScript(t=>localStorage.setItem('access_token',t),token);
  for(const width of [360,390,768,1280])for(const route of ['/','/reports','/warehouse/export','/orders','/scales','/my-account','/warehouse/materials','/notifications']){
   await p.setViewportSize({width,height:670});await p.goto('http://127.0.0.1:5170'+route);await p.locator('.topbar-right').waitFor();await p.locator('.theme-hero').first().waitFor();await p.evaluate(()=>document.fonts.ready);await p.waitForTimeout(120);
   const header=p.locator('.topbar'),main=p.locator('.main'),rail=p.locator('.mobile-nav-rail');
   await main.evaluate(n=>{n.style.scrollBehavior='auto';n.scrollTop=0});
   const top=await header.boundingBox();assert.equal(top.y,0,route+' header starts at top');
   if(route!=='/'){
    const fade=await header.evaluate(n=>{const s=getComputedStyle(n,'::after');return {content:s.content,height:s.height,background:s.backgroundImage}});
    assert.equal(fade.height,'64px',route+' shared fade');assert.match(fade.background,/237, 244, 252/,route+' light canvas behind toolbar');
   }
   if(route==='/reports'){
    const colors=await p.locator('.report-tab.active,.report-hero').evaluateAll(ns=>ns.map(n=>getComputedStyle(n).backgroundImage));
    assert(colors.every(c=>!c.includes('36, 134, 110')&&!c.includes('23, 107, 104')),'legacy report green');
    const text=await p.locator('.report-tab.active b').evaluate(n=>getComputedStyle(n).color);assert.equal(text,'rgb(248, 229, 172)','active report text contrast');
   }
   if(route==='/'){
    const colors=await p.locator('.hall-stock-grid > div').evaluateAll(ns=>ns.map(n=>getComputedStyle(n).backgroundImage));assert.equal(colors.length,3);assert(colors.every(c=>c.includes('0, 21, 47')),'all hall stock cards navy: '+JSON.stringify(colors));
   }
   const before=width<=768?await rail.boundingBox():null;
   const scroll=await main.evaluate(n=>{n.scrollTop=n.scrollHeight;return {top:n.scrollTop,max:n.scrollHeight-n.clientHeight}});
   if(scroll.max>top.height){assert((await header.boundingBox()).y+top.height<=1,route+' header must leave viewport');}
   assert.equal(await p.evaluate(()=>document.body.scrollTop),0);
   if(before){assert.deepEqual(await rail.boundingBox(),before,route+' fixed rail');await rail.click();await p.locator('.sidebar.mobile-open').waitFor();await rail.click();await p.locator('.sidebar.mobile-open').waitFor({state:'hidden'});await p.waitForTimeout(250);}
   await main.evaluate(n=>n.scrollTop=0);await p.waitForTimeout(180);
   if(width===390&&['/','/reports','/warehouse/export'].includes(route)&&process.env.THEME_SCREENSHOTS){fs.mkdirSync(process.env.THEME_SCREENSHOTS,{recursive:true});await p.screenshot({path:process.env.THEME_SCREENSHOTS+'/'+(route==='/'?'dashboard':route.slice(1).replaceAll('/','-'))+'.png'});}
   console.log('PASS '+width+'px '+route+' shared fade, palette, scrolling header and fixed rail');
  }
 }finally{await b.close()}
})().catch(e=>{console.error(e);process.exit(1)});
