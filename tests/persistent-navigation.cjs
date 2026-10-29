const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PROFIT_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),read=p=>fs.readFileSync(path.join(root,p),'utf8');
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
  const layout=read('TORSEPAN.Panel/Components/Layout/MainLayout.razor');
  assert(layout.indexOf('<nav class="bottom-navigation"')>layout.indexOf('@Body</main>'),'dock belongs to shared layout outside page content');
  assert(!/mobile-input-active[^}]*display\s*:\s*none/.test(read('TORSEPAN.Panel/wwwroot/navigation-shell.css')),'typing cannot hide the dock');
  const css=['vendor/bootstrap/bootstrap.rtl.min.css','app.css','theme.css','theme-pages.css','navigation-shell.css'].map(p=>read('TORSEPAN.Panel/wwwroot/'+p)).join('\n');
  const page=await browser.newPage();
  for(const width of [320,390,768,1280]){
   await page.setViewportSize({width,height:800});
   await page.setContent(`<html dir="rtl"><head><style>${css}</style></head><body><div class="layout theme-shell"><div class="main"><main class="content"><div id="page" class="navy-management-page"><h1>ناحیه کاربری</h1><div style="height:1100px"></div><label>کد ساز<input id="code"></label><div style="height:1000px"></div></div></main></div><nav class="bottom-navigation"><button id="menu">منو</button><a href="/" id="home">داشبورد</a><a href="/bowls/new">ثبت کاسه</a><a href="/orders">ثبت سفارش</a></nav></div></body></html>`);
   // An overlay keyboard changes visualViewport without resizing the layout viewport.
   await page.evaluate(()=>{window.testViewport=new EventTarget();Object.assign(testViewport,{height:800,offsetTop:0,scale:1});Object.defineProperty(window,'visualViewport',{value:testViewport,configurable:true});});
   await page.addScriptTag({content:read('TORSEPAN.Panel/wwwroot/mobile-input.js')});
   async function CheckDock(bottom=800){
    const dock=page.locator('.bottom-navigation');await dock.waitFor({state:'visible'});
    await page.waitForFunction(bottom=>Math.abs(document.querySelector('.bottom-navigation').getBoundingClientRect().bottom-bottom)<2,bottom);
    assert.equal(await dock.locator('a,button').count(),4);
    assert(await page.locator('#home').evaluate(e=>{const r=e.getBoundingClientRect();return document.elementFromPoint(r.x+r.width/2,r.y+r.height/2)===e;}),'dashboard target receives taps');
   }
   await CheckDock();
   await page.locator('#code').focus();await CheckDock();
   if(width<=900){
    await page.evaluate(()=>{Object.assign(testViewport,{height:440,offsetTop:30});testViewport.dispatchEvent(new Event('resize'));});
    await CheckDock(470);
    await page.evaluate(()=>{Object.assign(testViewport,{height:800,offsetTop:0});testViewport.dispatchEvent(new Event('resize'));});
    await CheckDock();
   }
   await page.locator('#code').blur();await CheckDock();
   // Switching page content must not affect the shared fixed navigation.
   for(const name of ['داشبورد','تولید','انبار','برنامه کاری','دانلود اپلیکیشن']){
    await page.evaluate(name=>{document.querySelector('#page').innerHTML=`<h1>${name}</h1><div style="height:2400px"></div>`;window.scrollTo(0,1500);const main=document.querySelector('.main');main.scrollTop=1500;},name);
    await CheckDock();
   }
   console.log('PASS persistent four-item dock: typing, overlay keyboard, blur, scrolling, page changes',width);
  }
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
